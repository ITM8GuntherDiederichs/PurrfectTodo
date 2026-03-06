# 🐱 PurrfectTodo

A cat-themed todo application built with .NET 10, Blazor Server, MudBlazor, Entity Framework Core, and ASP.NET Identity.

## Stack

- **Frontend**: Blazor Server (.NET 10) with MudBlazor UI components
- **Backend**: ASP.NET Core (.NET 10)
- **ORM**: Entity Framework Core 10 with SQL Server
- **Auth**: ASP.NET Core Identity
- **Testing**: xUnit, Moq, coverlet

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (local or Azure)

### Configuration & Secrets

The connection string placeholder in `appsettings.json` points to LocalDB for development.

For the admin account password, set it via [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets):

```bash
cd PurrfectTodo
dotnet user-secrets set "SystemAccount:Password" "YourPassword123!"
```

The admin account (`admin@purrfecttodo.local`) is seeded on startup using this password.  
If no password is configured, a random one is generated and logged as a warning.

### Run Locally

```bash
run.cmd
```

Or manually:

```bash
cd PurrfectTodo
dotnet run
```

### Run Tests

```bash
dotnet test
```

## Project Structure

```
PurrfectTodo.sln
├── PurrfectTodo/           # Blazor Server web application
└── PurrfectTodo.Tests/     # xUnit test project
```

## CI

GitHub Actions CI runs on every push and pull request to `main`:
- Restore dependencies
- Build (Release)
- Run tests
