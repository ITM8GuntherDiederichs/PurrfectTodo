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
