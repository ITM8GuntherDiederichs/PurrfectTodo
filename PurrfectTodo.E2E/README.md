# PurrfectTodo.E2E

End-to-end Playwright tests for the PurrfectTodo Blazor Server application.

## Prerequisites

- .NET 10 SDK
- Chromium browser installed via Playwright (one-time setup below)
- PurrfectTodo app accessible (or `run-dev.cmd` available to auto-start it)

## One-time browser install

```powershell
cd PurrfectTodo.E2E
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

## Running the tests

The test suite auto-detects whether the app is already running on
`http://localhost:5059`. If it is not, it starts it via `run-dev.cmd` and
waits up to 30 seconds for the server to become ready.

```powershell
cd PurrfectTodo.E2E
dotnet test --settings playwright.runsettings
```

To run with a visible browser window (useful for debugging):

```powershell
$env:HEADED = "1"
dotnet test --settings playwright.runsettings
```

## Tests

| Test | Description |
|------|-------------|
| `Login_WithValidCredentials_RedirectsToDashboard` | Valid login lands on dashboard |
| `Login_WithWrongPassword_ShowsError` | Wrong password shows error banner |
| `Login_WithEmptyFields_ShowsValidation` | Empty submit shows validation |
| `Login_ThenLogout_RedirectsToLogin` | Logout redirects back to login page |

## CI

**These tests are intentionally excluded from CI.** Browsers are not available
in the CI environment. Run them locally or against a staging environment.
