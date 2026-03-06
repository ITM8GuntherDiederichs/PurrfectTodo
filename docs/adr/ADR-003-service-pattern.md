# ADR-003: Service Pattern — IDbContextFactory, Primary Constructors, and Method-Scoped DbContext

## Status
Accepted

## Context

Purrfect Todo uses Blazor Server, where components have a **long-lived circuit lifetime** that spans the user's session. If a `DbContext` is injected directly (scoped lifetime) into a service that is also scoped, the DbContext may be held open for the entire circuit — leading to:

- **Stale data** — EF Core's first-level cache returns cached entities instead of querying the database.
- **Concurrency conflicts** — a single context tracking changes from multiple async operations on the same circuit.
- **Connection pool exhaustion** — connections remain open for the full circuit duration rather than being returned to the pool promptly.

The reference project `SecondWork` solves this by using `IDbContextFactory<ApplicationDbContext>` and creating a short-lived `DbContext` per service method. This is the established team pattern and must be followed in Purrfect Todo.

---

## Decision

All application services in Purrfect Todo **must** follow this pattern:

### 1. Primary Constructor Syntax

Services use C# 12 **primary constructor** syntax. Dependencies are declared in the constructor parameter list — not as private fields assigned in a body.

```csharp
// ✅ Correct
public class CatService(IDbContextFactory<ApplicationDbContext> factory)
{
    // ...
}

// ❌ Incorrect — old-style constructor with private field
public class CatService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public CatService(IDbContextFactory<ApplicationDbContext> factory)
    {
        _factory = factory;
    }
}
```

Additional dependencies (e.g., `UserManager<ApplicationUser>`) are added to the primary constructor parameter list:

```csharp
public class AdminService(
    IDbContextFactory<ApplicationDbContext> factory,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager)
{
    // ...
}
```

### 2. IDbContextFactory — Never Inject DbContext Directly

Services **must not** accept `ApplicationDbContext` as a constructor parameter. They must accept `IDbContextFactory<ApplicationDbContext>`.

```csharp
// ✅ Correct — factory injected, context created per method
public class TodoService(IDbContextFactory<ApplicationDbContext> factory) { }

// ❌ Incorrect — DbContext injected directly
public class TodoService(ApplicationDbContext db) { }
```

`IDbContextFactory<ApplicationDbContext>` is registered in `Program.cs` as a singleton:

```csharp
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
```

### 3. Short-Lived DbContext Per Method

Every service method that touches the database creates its own `DbContext` with `using var`:

```csharp
public async Task<List<Cat>> GetCatsAsync(string userId)
{
    using var db = await factory.CreateDbContextAsync();   // ← created here
    return await db.Cats
        .Where(c => c.UserId == userId)
        .OrderBy(c => c.Name)
        .ToListAsync();
}                                                           // ← disposed here
```

The `using var` ensures the context — and its underlying database connection — is **returned to the connection pool** as soon as the method exits, regardless of success or failure.

**Multi-step methods** that require a single transaction use one context for the entire method body:

```csharp
public async Task<Cat> CreateCatAsync(Cat cat, string userId)
{
    using var db = await factory.CreateDbContextAsync();
    cat.Id = Guid.NewGuid();
    cat.UserId = userId;
    cat.CreatedAt = DateTime.UtcNow;
    db.Cats.Add(cat);
    await db.SaveChangesAsync();
    return cat;
}
```

### 4. Service Lifetime — Scoped

All services are registered as **Scoped** in `Program.cs`:

```csharp
builder.Services.AddScoped<CatService>();
builder.Services.AddScoped<TodoService>();
builder.Services.AddScoped<AdminService>();
```

Scoped lifetime matches the Blazor Server circuit. Because services hold **no database state** (the DbContext is created and disposed per method), scoped lifetime is safe and does not cause the issues described in the Context section.

### 5. No Business Logic in Components

Blazor components **must not** directly use `ApplicationDbContext`. All data access goes through services. Components:

- Inject the relevant service
- Call service methods with the current `userId` (see ADR-002)
- Bind results to component state for rendering

### 6. Enums Live in Data/Enums.cs

Domain enumerations are defined in a single file: `Data/Enums.cs`. For Purrfect Todo the following enums are defined:

```csharp
namespace PurrfectTodo.Data;

/// <summary>
/// Categories of cat care tasks.
/// </summary>
public enum Category
{
    Feeding,
    Vet,
    Grooming,
    Playtime,
    Medication,
    Other
}

/// <summary>
/// Priority levels for a todo item.
/// </summary>
public enum Priority
{
    Low,
    Medium,
    High
}
```

These are stored as integers in the database (EF Core default). String conversion is not required for MVP; if readable column values are needed for reporting, EF Core `.HasConversion<string>()` can be added without a breaking migration.

### 7. Complete Service Template

The canonical template for all new services in Purrfect Todo:

```csharp
using Microsoft.EntityFrameworkCore;
using PurrfectTodo.Data;

namespace PurrfectTodo.Services;

public class ExampleService(IDbContextFactory<ApplicationDbContext> factory)
{
    // READ — always scoped to userId
    public async Task<List<ExampleEntity>> GetAllAsync(string userId)
    {
        using var db = await factory.CreateDbContextAsync();
        return await db.ExampleEntities
            .Where(e => e.UserId == userId)
            .ToListAsync();
    }

    // READ SINGLE — ownership verified in query
    public async Task<ExampleEntity?> GetByIdAsync(Guid id, string userId)
    {
        using var db = await factory.CreateDbContextAsync();
        return await db.ExampleEntities
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
    }

    // CREATE — UserId stamped from parameter, never trusted from input
    public async Task<ExampleEntity> CreateAsync(ExampleEntity entity, string userId)
    {
        using var db = await factory.CreateDbContextAsync();
        entity.Id = Guid.NewGuid();
        entity.UserId = userId;
        entity.CreatedAt = DateTime.UtcNow;
        db.ExampleEntities.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    // UPDATE — ownership check before mutation
    public async Task<ExampleEntity?> UpdateAsync(ExampleEntity updated, string userId)
    {
        using var db = await factory.CreateDbContextAsync();
        var existing = await db.ExampleEntities
            .FirstOrDefaultAsync(e => e.Id == updated.Id && e.UserId == userId);
        if (existing == null) return null;

        // ... copy changed fields
        existing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return existing;
    }

    // DELETE — ownership check before deletion
    public async Task<bool> DeleteAsync(Guid id, string userId)
    {
        using var db = await factory.CreateDbContextAsync();
        var entity = await db.ExampleEntities
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return false;
        db.ExampleEntities.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }
}
```

### 8. Services for Purrfect Todo MVP

| Service | Responsibility |
|---------|---------------|
| `CatService` | CRUD for Cat entities, scoped to UserId |
| `TodoService` | CRUD + filtering for Todo entities, scoped to UserId |
| `AdminService` | User management (Admin role only); not UserId-scoped |

---

## Consequences

### What becomes easier
- **No stale data** — each method gets a fresh DbContext with an empty first-level cache.
- **Connection pool efficiency** — connections released immediately after each operation.
- **Thread safety** — no shared DbContext state between concurrent Blazor Server circuit operations.
- **Testability** — `IDbContextFactory<T>` can be easily mocked or pointed at an in-memory/SQLite database in tests.
- **Consistency** — identical pattern to `SecondWork`; team agents produce consistent, reviewable code without pattern drift.

### What becomes harder
- **Cross-service transactions** — if two services need to participate in a single database transaction, they cannot share a DbContext by default. This is not required for the MVP. If needed, a Unit of Work pattern or a dedicated orchestration service method should be introduced.
- **Lazy loading** — EF Core lazy loading proxies are incompatible with `IDbContextFactory` contexts disposed after each method. This is a feature, not a limitation: all navigation properties must be explicitly loaded via `.Include()`, making data access explicit and predictable.

---

## Alternatives Considered

### Injecting DbContext directly (AddDbContext)
- **Why rejected:** As described in Context, this causes stale data and connection-holding issues in Blazor Server circuits. The `IDbContextFactory` pattern is the Microsoft-recommended approach for Blazor Server.

### Repository pattern (IRepository<T> abstractions)
- **Why rejected:** For this project size and with EF Core as the single data store, repository abstractions add interface boilerplate without meaningful benefit. The service layer already provides the abstraction boundary between components and data access. Repository pattern is appropriate when the data access layer must be swappable; that is not a requirement here.

### Unit of Work pattern
- **Why not adopted now:** Not required for MVP. All writes are contained within single service methods with one `SaveChangesAsync()` call. Deferred until cross-service transactions are needed.

---

## Reference

- [Microsoft Docs — DbContext lifetime in Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/blazor-ef-core)
- Reference implementation: `SecondWork/Services/TodoService.cs`, `SecondWork/Services/FamilyService.cs`
