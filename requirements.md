# Purrfect Todo — Requirements Document

> version: draft-1  
> updated: 2026-03-05  
> status: Awaiting sign-off

---

## 1. Project Overview

**Purrfect Todo** is a cat-themed, multi-user web application that helps cat owners manage tasks for their cats — feeding schedules, vet appointments, grooming, playtime, and anything else a cat demands.

- **Problem:** Cat owners juggle recurring and one-off care tasks across multiple cats with no dedicated tool.
- **Target users:** Individual cat owners who own one or more cats.
- **Success metric:** Users can register, add their cats, and manage todos for each cat without friction.
- **Type:** Greenfield project.

---

## 2. Users & Roles

| Role | Description |
|------|-------------|
| **User** | Registered cat owner. Manages their own cats and todos. Cannot see other users' data. |
| **Admin** | Site administrator. Can manage all users. |

**Authentication:** Email + password via ASP.NET Identity. Standard registration flow (register → confirm email → login).

**Assumption:** No social login for MVP. No multi-tenant household sharing in v1 — each account is personal.

---

## 3. User Stories (MVP)

### Authentication
- **US-01:** As a visitor, I want to register with email and password so that I can create a personal account.
- **US-02:** As a registered user, I want to log in and log out so that my data is private and secure.
- **US-03:** As a user, I want to reset my password via email so that I can recover access if I forget it.

### Cat Management
- **US-04:** As a user, I want to add a cat profile (name, optional photo) so that I can organise todos by cat.
- **US-05:** As a user, I want to edit or delete a cat profile so that I can keep my cats up to date.

### Todo Management
- **US-06:** As a user, I want to create a todo for a specific cat so that I know which task belongs to whom.
- **US-07:** As a user, I want to set a title, category, due date, and priority on a todo so that I can organise and prioritise care tasks.
- **US-08:** As a user, I want to mark a todo as complete so that I can track what has been done.
- **US-09:** As a user, I want to edit a todo so that I can correct mistakes or update details.
- **US-10:** As a user, I want to delete a todo so that I can remove tasks that are no longer needed.
- **US-11:** As a user, I want to filter/view todos by cat, category, priority, or due date so that I can focus on what matters now.

### Admin
- **US-12:** As an admin, I want to view and manage all registered users so that I can maintain the platform.

---

## 4. User Stories (Nice-to-Have — v2)

- **US-13:** As a user, I want to set a todo as recurring (daily, weekly, monthly) so that repeated tasks are automatically re-created.
- **US-14:** As a user, I want to receive an email reminder before a todo is due so that I don't forget important cat care tasks.
- **US-15:** As a user, I want a dashboard showing overdue and upcoming todos at a glance.
- **US-16:** As a user, I want to upload a photo of my cat so that the profile feels personal.

---

## 5. Domain Model

### Entities

```
User
  - Id (string, Identity)
  - Email
  - FirstName
  - LastName
  - CreatedAt

Cat
  - Id (Guid)
  - UserId (FK → User)
  - Name (string, required)
  - PhotoUrl (string, nullable)
  - CreatedAt

Todo
  - Id (Guid)
  - CatId (FK → Cat)
  - UserId (FK → User)   ← denormalised for fast queries
  - Title (string, required)
  - Category (enum: Feeding | Vet | Grooming | Playtime | Medication | Other)
  - Priority (enum: Low | Medium | High)
  - DueDate (DateTime?, nullable)
  - IsCompleted (bool)
  - CompletedAt (DateTime?, nullable)
  - CreatedAt
  - UpdatedAt
```

### Relationships
- User → Cats: one-to-many
- Cat → Todos: one-to-many
- User → Todos: one-to-many (via UserId on Todo for isolation)

### Data isolation
Every query is scoped to the logged-in user's `UserId`. Users cannot access another user's cats or todos.

---

## 6. Technical Stack

| Layer | Technology |
|-------|-----------|
| Framework | .NET 10, ASP.NET Core |
| Frontend | Blazor Server |
| UI Components | MudBlazor |
| ORM | Entity Framework Core (code-first) |
| Database | SQL Server (LocalDB dev, Azure SQL prod) |
| Auth | ASP.NET Identity |
| Secrets | Azure Key Vault (prod), User Secrets (dev) |
| Hosting | Azure App Service |
| CI/CD | GitHub Actions |
| Email (v2) | SendGrid or Azure Communication Services |

---

## 7. Non-Functional Requirements

| Requirement | Detail |
|-------------|--------|
| Security | All routes require authentication except register/login. Data isolated by UserId. HTTPS enforced. |
| Performance | < 2 second page load for todo list up to 500 todos. |
| Accessibility | WCAG 2.1 AA target. |
| Browser support | Modern browsers: Chrome, Edge, Firefox, Safari. Mobile-responsive. |
| Localisation | English only for MVP. |
| Data retention | Users can delete their account and all associated data (GDPR). |

---

## 8. Integrations

| Integration | Purpose | MVP? |
|-------------|---------|------|
| ASP.NET Identity | Auth + user management | ✅ Yes |
| SendGrid / Azure Communication Services | Password reset emails | ✅ Yes (reset only) |
| SendGrid / Azure Communication Services | Todo reminder emails | ❌ v2 |

---

## 9. Open Questions / Assumptions

| # | Question / Assumption |
|---|----------------------|
| A1 | **Assumed:** No household/sharing feature in v1 — todos are strictly per user account. |
| A2 | **Assumed:** Completed todos are retained (not auto-deleted) so users can review history. |
| A3 | **Assumed:** Cat photo upload is v2; v1 uses a default cat avatar. |
| A4 | **Assumed:** No mobile app — web only, but mobile-responsive. |
| A5 | **Open:** Should completed todos be hidden by default or shown with a strikethrough? |
| A6 | **Open:** Is there a maximum number of cats per user? (Assumed: no hard limit.) |
| A7 | **Open:** Email provider preference — SendGrid or Azure Communication Services? |

---

## 10. Definition of Done

A feature is done when:
- [ ] Code builds with 0 errors (`dotnet build`)
- [ ] Unit tests pass (`dotnet test`)
- [ ] Feature works end-to-end in the browser (manual smoke test or Playwright)
- [ ] Data is correctly isolated to the logged-in user
- [ ] PR is reviewed and merged to `main` by the PR Review agent
- [ ] Related GitHub issue is closed with a resolution comment
