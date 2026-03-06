# Purrfect Todo — Agent Brief

Cat-themed multi-user todo web app. Cat owners register, add cat profiles, and manage care tasks (feeding, vet, grooming, etc.) per cat.

## Stack
.NET 10 · Blazor Server · MudBlazor · EF Core · SQL Server · ASP.NET Identity · Azure App Service · GitHub Actions

## Key Facts
- **Multi-user:** every query must be scoped to the logged-in user's `UserId`
- **Auth:** email + password via ASP.NET Identity (includes password reset email)
- **Core entities:** User → Cat (one-to-many) → Todo (one-to-many)
- **Todo fields:** Title, Category (enum), Priority (enum), DueDate, IsCompleted
- **v2 only:** recurring todos, email reminders

## MVP Scope
1. Register / login / password reset
2. Cat profile CRUD (name; photo is v2)
3. Todo CRUD per cat (title, category, due date, priority, complete/uncomplete)
4. Filter/sort todos by cat, category, priority, due date
5. Admin user management page

## Full requirements: `requirements.md`
