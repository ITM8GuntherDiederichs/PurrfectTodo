using Microsoft.EntityFrameworkCore;
using PurrfectTodo.Data;

namespace PurrfectTodo.Tests.Helpers;

/// <summary>
/// An <see cref="IDbContextFactory{TContext}"/> backed by an EF Core InMemory database.
/// Each call to CreateDbContext returns a fresh context sharing the same InMemory store.
/// </summary>
public sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
    : IDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext() => new(options);

    public Task<ApplicationDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());

    // ── Factory method ────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a factory backed by a uniquely named InMemory database so that
    /// each test run gets a clean, isolated store.
    /// </summary>
    public static TestDbContextFactory CreateIsolated()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TestDbContextFactory(options);
    }
}
