using Microsoft.EntityFrameworkCore;
using PurrfectTodo.Data;

namespace PurrfectTodo.Services;

/// <summary>
/// Provides CRUD operations for <see cref="Cat"/> entities, scoped to the owning user.
/// </summary>
/// <remarks>
/// All methods require a <paramref name="userId"/> and enforce ownership — a user can only
/// read or mutate their own cats. The service follows ADR-003: primary constructor syntax
/// and a short-lived <see cref="ApplicationDbContext"/> per method via
/// <see cref="IDbContextFactory{TContext}"/>.
/// </remarks>
public class CatService(IDbContextFactory<ApplicationDbContext> factory)
{
    /// <summary>
    /// Returns all cats owned by the specified user, ordered by name.
    /// </summary>
    public async Task<List<Cat>> GetCatsAsync(string userId)
    {
        using var db = await factory.CreateDbContextAsync();
        return await db.Cats
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Returns a single cat by ID, or <c>null</c> if it does not exist or is not owned by the user.
    /// </summary>
    public async Task<Cat?> GetCatAsync(Guid id, string userId)
    {
        using var db = await factory.CreateDbContextAsync();
        return await db.Cats
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
    }

    /// <summary>
    /// Creates a new cat, stamping <paramref name="userId"/> as the owner.
    /// </summary>
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

    /// <summary>
    /// Updates an existing cat after verifying ownership.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the cat is not found or not owned by the user.</exception>
    public async Task UpdateCatAsync(Cat cat, string userId)
    {
        using var db = await factory.CreateDbContextAsync();
        var existing = await db.Cats
            .FirstOrDefaultAsync(c => c.Id == cat.Id && c.UserId == userId);
        if (existing is null)
            throw new KeyNotFoundException($"Cat {cat.Id} not found for user {userId}.");

        existing.Name = cat.Name;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a cat (and cascades to its todos) after verifying ownership.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the cat is not found or not owned by the user.</exception>
    public async Task DeleteCatAsync(Guid id, string userId)
    {
        using var db = await factory.CreateDbContextAsync();
        var cat = await db.Cats
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (cat is null)
            throw new KeyNotFoundException($"Cat {id} not found for user {userId}.");

        db.Cats.Remove(cat);
        await db.SaveChangesAsync();
    }
}
