# Security Review — PurrfectTodo — 2026-03-06

**Reviewer:** Security Agent  
**Scope:** Full pre-release review — Authentication, Authorisation, OWASP Top 10, Secrets, Dependencies  
**Branch reviewed:** `main` (HEAD at time of review)  
**Fix PR:** [#14](https://github.com/ITM8GuntherDiederichs/PurrfectTodo/pull/14) — `fix/security-lockout-enabled`

---

## Critical (fix before release)

_None found._

---

## High (fix before release)

### H1 — Account lockout silently disabled on password sign-in

| | |
|---|---|
| **File** | `PurrfectTodo/Components/Account/Pages/Login.razor` line 114 |
| **OWASP** | A07 — Authentication Failures |
| **Fix** | Included in PR #14 |

`PasswordSignInAsync` was called with `lockoutOnFailure: false`. Program.cs correctly configures a lockout policy (5 failed attempts → 15-minute lockout, `AllowedForNewUsers = true`), but that policy is **never applied** because of this flag. An attacker can attempt unlimited passwords against any user account with no throttling or lockout.

**Fix applied:** `lockoutOnFailure: true` — the lockout policy now enforces correctly.

---

## Medium (schedule for next sprint)

### M1 — No input length limits on entity fields

| | |
|---|---|
| **Files** | `Data/Cat.cs`, `Data/Todo.cs`, `Data/ApplicationUser.cs`, `Account/Pages/Register.razor` |
| **OWASP** | A08 — Software & Data Integrity Failures |
| **Fix** | Included in PR #14 |

`Cat.Name`, `Todo.Title`, `ApplicationUser.FirstName`, and `ApplicationUser.LastName` had no `[MaxLength]` annotations. An authenticated user could POST arbitrarily long strings, causing database errors, log bloat, or storage DoS.

**Fix applied:** `[MaxLength(100)]` on Name/FirstName/LastName; `[MaxLength(500)]` on Title. EF migration `SecurityInputLengthLimits` enforces column-level constraints. Matching `[MaxLength]` added to `Register.razor` `InputModel` for client-side validation.

---

### M2 — No rate limiting on authentication endpoints

| | |
|---|---|
| **File** | `Program.cs` |
| **OWASP** | A04 — Insecure Design |
| **Action** | File backlog issue |

Even with lockout enabled (H1 fix), there is no ASP.NET Core rate-limiting middleware protecting `/Account/Login`, `/Account/Register`, or `/Account/ForgotPassword`. A distributed attack from many IPs bypasses per-account lockout. Consider adding `AddRateLimiter` with a fixed-window or sliding-window policy on auth endpoints.

---

### M3 — Raw exception messages exposed in UI

| | |
|---|---|
| **Files** | `Pages/AdminUsers.razor`, `Pages/Cats.razor`, `Pages/Todos.razor`, `Pages/Home.razor` |
| **OWASP** | A05 — Security Misconfiguration |
| **Action** | File backlog issue |

Exception messages (e.g. `ex.Message`) are surfaced directly to the user via `Snackbar.Add($"Error: {ex.Message}", ...)` and `_initError = ex.Message`. While service-layer exceptions are controlled (KeyNotFoundException messages include entity IDs and user IDs), an unexpected database or framework exception could leak internal implementation details.

**Recommendation:** Catch `Exception` at the component level and display a generic "An unexpected error occurred" message. Log the full exception server-side. Only surface controlled error strings (from known exception types like `KeyNotFoundException`) to the UI.

---

### M4 — IdentityNoOpEmailSender registered in all environments

| | |
|---|---|
| **File** | `Program.cs` line 51 |
| **OWASP** | A07 — Authentication Failures |
| **Action** | File backlog issue |

`IdentityNoOpEmailSender` is registered unconditionally. In production this means:
- Email confirmation links are **never delivered** — new users cannot confirm accounts and therefore cannot log in.
- Password reset tokens are generated but **emails are never sent** — users who forget their password cannot recover their accounts.

`RequireConfirmedAccount = true` is set, which is good, but it only works if emails are actually delivered. Register a real `IEmailSender<ApplicationUser>` implementation (e.g. Azure Communication Services, SendGrid) before going live.

---

## Low / Informational

### L1 — No security response headers (CSP, X-Frame-Options, X-Content-Type-Options)

No Content Security Policy, `X-Frame-Options`, or `X-Content-Type-Options` headers are configured. Blazor Server's SignalR-based rendering significantly limits classical XSS, but defence-in-depth recommends adding security headers. Consider using `NWebsec` middleware or `app.Use()` to set these headers in production.

### L2 — Password minimum length is 6 characters

The `Register.razor` `InputModel` enforces `MinimumLength = 6`. NIST SP 800-63B recommends a minimum of 8 characters. ASP.NET Identity's password complexity defaults (digit, uppercase, lowercase, non-alphanumeric) partially compensate, but raising the minimum to 8 is low-cost and aligns with modern guidance.

### L3 — AdminService does not re-check the Admin role internally

`AdminService` comments state "Authorisation is enforced at the page/component level; this service does not re-check roles." This is acceptable for the current simple architecture, but as a defence-in-depth measure consider adding a role assertion inside `DeleteUserAsync` (or a policy-based authorization approach) so a misconfigured page cannot accidentally expose admin operations.

### L4 — Demo scaffold pages still present (`Counter.razor`, `Weather.razor`)

The default Blazor scaffold pages `Counter.razor` and `Weather.razor` are still in the project. While they carry `[Authorize]` they serve no application purpose and represent unnecessary attack surface. Remove before production.

### L5 — Connection string in `appsettings.json` uses Windows Authentication

The development connection string (`Trusted_Connection=True`) contains no credentials so there is no immediate secret exposure. However, ensure the production connection string is stored in Azure Key Vault and injected at runtime — never committed to source control.

---

## Passed Checks ✅

| Check | Result |
|---|---|
| CatService — all methods scope by UserId | ✅ Pass |
| TodoService — all methods scope by UserId | ✅ Pass |
| AdminUsers.razor — `[Authorize(Roles = "Admin")]` | ✅ Pass |
| Home, Cats, Todos pages — `[Authorize]` attribute | ✅ Pass |
| HTTPS enforced — `app.UseHttpsRedirection()` | ✅ Pass |
| HSTS enabled in production — `app.UseHsts()` | ✅ Pass |
| Anti-forgery protection — `app.UseAntiforgery()` | ✅ Pass |
| Open redirect protection in IdentityRedirectManager | ✅ Pass |
| No hardcoded secrets or API keys in code | ✅ Pass |
| DataSeeder reads admin password from configuration | ✅ Pass |
| UserSecretsId configured in .csproj | ✅ Pass |
| .gitignore excludes `.pfx`, `.publishsettings`, `.env` | ✅ Pass |
| No vulnerable NuGet packages (`dotnet list package --vulnerable`) | ✅ Pass |
| EF Core LINQ queries — no raw SQL injection risk | ✅ Pass |
| Blazor HTML encoding — XSS risk low | ✅ Pass |
| Status cookie: HttpOnly, SameSite=Strict, 5-second MaxAge | ✅ Pass |
| Error page: generic in production, no stack trace exposed | ✅ Pass |
| Lockout configured: 5 attempts / 15 minutes | ✅ Pass (enabled by H1 fix) |
| RequireConfirmedAccount = true | ✅ Pass |
| Email confirmation token URL-encoded via WebEncoders | ✅ Pass |
| Password reset uses time-limited one-time tokens (Identity default) | ✅ Pass |

---

## Recommendations Summary

| ID | Severity | Action |
|---|---|---|
| H1 | High | ✅ Fixed in PR #14 — lockout enabled |
| M1 | Medium | ✅ Fixed in PR #14 — MaxLength constraints |
| M2 | Medium | File issue — add rate limiting to auth endpoints |
| M3 | Medium | File issue — genericise exception messages in UI |
| M4 | Medium | File issue — replace IdentityNoOpEmailSender for production |
| L1 | Low | File issue — add CSP/security response headers |
| L2 | Low | File issue — raise password minimum length to 8 |
| L3 | Low/Info | File issue — defence-in-depth role check in AdminService |
| L4 | Low/Info | File issue — remove demo scaffold pages |
| L5 | Info | Verify production connection string goes to Key Vault |
