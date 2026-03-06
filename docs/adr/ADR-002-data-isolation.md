# ADR-002: Data Isolation — UserId Scoping on Every Query

## Status
Accepted

## Context

Purrfect Todo is a **multi-user application** where each registered user owns their own cats and todos. The core security requirement (stated in the requirements document, section 7) is:

> "Data isolated by UserId. Users cannot access another user's cats or todos."

Without a deliberate, consistent isolation strategy, a developer could accidentally write a query that retrieves another user's data. This would be a **data privacy violation** and a potential GDPR breach.

The application does not use row-level security at the database level (Azure SQL RLS) — isolation is enforced entirely in the application service layer.

### Domain relationships relevant to isolation

```
User ──< Cat ──< Todo
         │         │
      UserId    UserId (denormalised)
```

- `Cat.UserId` — foreign key linking a cat to its owner.
- `Todo.UserId` — **denormalised** foreign key on Todo (in addition to `Todo.CatId`). This allows efficient, direct filtering of todos by user without a join through Cat. It also means that when a user deletes a cat, we can still query all of the user's todos without needing the cat to exist.

The denormalisation of `UserId` on `Todo` is a deliberate performance and isolation choice. It must be populated correctly on every insert (equal to the owning user's ID).

---

## Decision

**Every service method that reads or mutates data must scope its query to the calling user's `UserId`.**

### Rule 1 — Services never query without a UserId filter

No service method may return cats or todos without filtering by `UserId`. The `UserId` is always passed in as a parameter or resolved from `AuthenticationStateProvider` in the Blazor component before calling the service.

**Correct pattern — UserId passed explicitly:**
```csharp
// CatService.cs
public async Task<List<Cat>> GetCatsAsync(string userId)
{
    using var db = await _factory.CreateDbContextAsync();
    return await db.Cats
        .Where(c => c.UserId == userId)   // ← isolation enforced here
        .OrderBy(c => c.Name)
        .ToListAsync();
}
```

**Correct pattern — mutating with ownership check:**
```csharp
// TodoService.cs
public async Task<bool> DeleteAsync(Guid todoId, string userId)
{
    using var db = await _factory.CreateDbContextAsync();
    var todo = await db.Todos
        .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId); // ← both conditions
    if (todo == null) return false;   // not found OR belongs to another user
    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return true;
}
```

### Rule 2 — Cat ownership is always verified before accessing todos

When fetching todos for a specific cat, the query must confirm the cat belongs to the user **in the same query**:

```csharp
public async Task<List<Todo>> GetTodosForCatAsync(Guid catId, string userId)
{
    using var db = await _factory.CreateDbContextAsync();
    return await db.Todos
        .Where(t => t.CatId == catId && t.UserId == userId)  // ← both conditions
        .OrderByDescending(t => t.DueDate)
        .ToListAsync();
}
```

This prevents a user from supplying a valid `catId` that belongs to another user and receiving that user's todos.

### Rule 3 — Inserts always stamp UserId from the authenticated session

On creation, `UserId` is **never** accepted from user-supplied form input. It is always resolved from the authenticated session and stamped by the service:

```csharp
public async Task<Cat> CreateCatAsync(Cat cat, string userId)
{
    cat.Id = Guid.NewGuid();
    cat.UserId = userId;          // ← stamped here, never trusted from UI
    cat.CreatedAt = DateTime.UtcNow;
    using var db = await _factory.CreateDbContextAsync();
    db.Cats.Add(cat);
    await db.SaveChangesAsync();
    return cat;
}
```

### Rule 4 — Admin service is the only exception

The `AdminService` (accessible only to users with the `Admin` role) may query across users for user-management purposes. This is gated behind role-based authorisation:

- Admin pages carry `[Authorize(Roles = "Admin")]`
- `AdminService` methods do **not** filter by UserId (intentionally)
- The Admin role is assigned only via the DataSeeder or manual DB intervention — never via self-registration

### Rule 5 — UserId is resolved in Blazor components, not in services

Services receive `userId` as a `string` parameter. They do not resolve it themselves from `IHttpContextAccessor` or `AuthenticationStateProvider`. This keeps services:
- Testable (no auth dependency needed in unit tests)
- Stateless (safe with `IDbContextFactory` and scoped lifetime)

**Standard component pattern:**
```razor
@inject AuthenticationStateProvider AuthStateProvider
@inject CatService CatService

@code {
    private string? _userId;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        _userId = authState.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (_userId != null)
            _cats = await CatService.GetCatsAsync(_userId);
    }
}
```

---

## Consequences

### What becomes easier
- **Security by default** — every service call requires a `userId`; it is impossible to accidentally omit the filter.
- **Testability** — services are pure functions over a DbContext factory + userId parameter; no auth mocking needed.
- **Auditability** — data ownership is explicit in every query; easy to trace in code review.
- **GDPR compliance** — `DELETE FROM Cats WHERE UserId = ?` and `DELETE FROM Todos WHERE UserId = ?` cleanly remove all user data.

### What becomes harder
- **Admin queries** — AdminService must intentionally bypass the pattern; reviewers must scrutinise any new AdminService method that returns user-owned data.
- **Cross-user features** — if a future version adds household sharing (v2 open question), this pattern must be extended, not bypassed.

---

## Alternatives Considered

### Database-level Row-Level Security (Azure SQL RLS)
- **Why not chosen:** RLS adds operational complexity (SQL predicates, session context), makes migrations harder to reason about, and is unnecessary when the service layer can enforce isolation cleanly. It may be revisited if the application is exposed directly to a client that can issue raw queries (e.g., a reporting tool), but that is not the case here.

### Global Query Filters on DbContext
- **Why not chosen:** EF Core global query filters (`HasQueryFilter`) can automatically inject `WHERE UserId = @currentUser` on every query. However, this requires injecting the current user into the DbContext constructor — which is incompatible with `IDbContextFactory` (the DbContext is created on demand, not per-request). Explicit filters per query are therefore more consistent with the chosen service pattern (ADR-003).

### Passing the full `ClaimsPrincipal` to services
- **Why not chosen:** Passing `ClaimsPrincipal` couples services to ASP.NET Core auth abstractions, making them harder to unit test. Extracting just the `userId` string in the component and passing it is simpler and sufficient.

---

## Review Trigger

Revisit this decision if:
- Household/sharing features are introduced in v2 (shared cats between users).
- An API layer is added that exposes endpoints callable outside of the Blazor auth context.
- Performance profiling shows that the `UserId` filter is not hitting an index (see DBA agent for index strategy).
