# ADR-001: Solution Structure — Single Project vs Layered Architecture

## Status
Accepted

## Context

Purrfect Todo is a greenfield, cat-themed multi-user todo web application built for individual cat owners. The stack is .NET 10, Blazor Server, MudBlazor, EF Core, SQL Server, and ASP.NET Identity, hosted on Azure App Service.

Before implementation begins, the team must decide how to structure the solution. The primary options are:

1. **Single project** — all code (UI components, services, data models, EF context) lives in one `.csproj`.
2. **Layered / Clean Architecture** — separate projects for Domain, Application, Infrastructure, and Presentation layers, with strict dependency rules enforced by project references.
3. **Vertical slices** — features are self-contained folders with their own models, services, and components.

The reference implementation for this team (`SecondWork`) uses a single-project Blazor Server layout and serves as the established baseline pattern.

### Forces at play

| Force | Impact |
|-------|--------|
| Small team (11 specialised agents, one product) | Low — simple structure reduces coordination overhead |
| MVP scope (auth, cat CRUD, todo CRUD, basic filtering) | Low — no complex domain logic that demands isolation |
| Blazor Server renders server-side; no separate API surface needed | Low — no need for an Application layer to mediate between a client and server |
| EF Core with a single `ApplicationDbContext` | Low — no benefit in isolating the infrastructure layer until multiple data sources exist |
| Future v2 features (email reminders, recurring todos) | Medium — may eventually justify an Application layer, but YAGNI applies now |
| Maintainability and onboarding cost | Lower with single project at this scope |

---

## Decision

**Use a single-project Blazor Server solution** for the Purrfect Todo MVP.

The project structure follows the established `SecondWork` pattern:

```
PurrfectTodo/
├── PurrfectTodo.csproj
├── Program.cs
├── Components/          # Blazor pages, layouts, shared UI components
│   ├── Account/         # Identity scaffolded pages
│   ├── Layout/          # MainLayout, NavMenu
│   └── Pages/           # Feature pages (Cats/, Todos/, Admin/)
├── Data/                # EF Core entities, enums, ApplicationDbContext
├── Services/            # Application services (CatService, TodoService, AdminService)
├── Migrations/          # EF Core migrations
├── wwwroot/             # Static assets (CSS, JS, images)
├── docs/
│   └── adr/             # Architecture Decision Records
├── appsettings.json
└── appsettings.Development.json
```

### Naming conventions

| Item | Convention | Example |
|------|-----------|---------|
| Namespace root | `PurrfectTodo` | `PurrfectTodo.Services` |
| Blazor pages | `Components/Pages/<Feature>/` | `Components/Pages/Todos/TodoList.razor` |
| Services | `Services/<Name>Service.cs` | `Services/TodoService.cs` |
| Entities | `Data/<EntityName>.cs` | `Data/Cat.cs` |
| Enums | `Data/Enums.cs` | `Data/Enums.cs` |

---

## Consequences

### What becomes easier
- **Onboarding** — one project to clone, build, and understand.
- **Refactoring** — renaming entities, services, or pages without crossing project boundaries.
- **EF migrations** — single context, single project; no cross-project reference issues.
- **Blazor component access** — components can directly reference services and models without DTO mapping layers.
- **Consistency** — aligns 100% with the `SecondWork` reference, so team agents apply the same patterns without translation.

### What becomes harder
- **Unit testing services in isolation** — without an Application layer, services depend on EF `IDbContextFactory`. This is mitigated by using `IDbContextFactory` rather than a concrete `DbContext`, and by keeping services focused and testable with an in-memory or SQLite provider.
- **Swapping data stores** — EF Core is deeply integrated. Acceptable for this project; Azure SQL is the only target.
- **Growing into microservices** — if the product scales dramatically, extracting an Application layer is straightforward from this structure (strangler fig migration). Not a concern for MVP.

---

## Alternatives Considered

### Layered / Clean Architecture (rejected)
- **Why rejected:** The overhead of Domain, Application, Infrastructure, and Presentation projects is disproportionate to the scope. The business logic is simple CRUD with user scoping — no complex aggregates, domain events, or policies. Clean Architecture optimises for large teams, complex domains, and multiple delivery mechanisms. None of those conditions apply here at MVP.

### Vertical Slice Architecture (rejected)
- **Why rejected:** Vertical slices work well for larger APIs with many independent features. Blazor Server with MudBlazor benefits from shared layouts and cascading auth state — slicing by feature would require duplicating or re-abstracting these shared elements. The added complexity is not justified for the MVP feature count.

---

## Review Trigger

Revisit this decision if:
- A second delivery mechanism is added (e.g., a REST API for a mobile app).
- The service layer exceeds ~10 services or services start sharing significant business logic.
- v2 features (recurring todos, email reminders) introduce meaningful domain complexity.
