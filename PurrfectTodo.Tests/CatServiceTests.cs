using PurrfectTodo.Data;
using PurrfectTodo.Services;
using PurrfectTodo.Tests.Helpers;

namespace PurrfectTodo.Tests;

/// <summary>
/// Unit tests for <see cref="CatService"/>.
/// Each test uses an isolated InMemory database to guarantee independence.
/// </summary>
public class CatServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CatService CreateService(out TestDbContextFactory factory)
    {
        factory = TestDbContextFactory.CreateIsolated();
        return new CatService(factory);
    }

    private static async Task<Cat> SeedCatAsync(
        TestDbContextFactory factory, string userId, string name = "Whiskers")
    {
        var cat = new Cat { Id = Guid.NewGuid(), UserId = userId, Name = name, CreatedAt = DateTime.UtcNow };
        await using var db = factory.CreateDbContext();
        db.Cats.Add(cat);
        await db.SaveChangesAsync();
        return cat;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCatsAsync_ReturnsOnlyCatsForUser()
    {
        // Arrange
        var service = CreateService(out var factory);
        const string userA = "user-a";
        const string userB = "user-b";

        await SeedCatAsync(factory, userA, "Mittens");
        await SeedCatAsync(factory, userA, "Luna");
        await SeedCatAsync(factory, userB, "Shadow");   // belongs to user B – must not appear

        // Act
        var results = await service.GetCatsAsync(userA);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, c => Assert.Equal(userA, c.UserId));
        Assert.DoesNotContain(results, c => c.Name == "Shadow");
    }

    [Fact]
    public async Task CreateCatAsync_StampsUserIdOnCat()
    {
        // Arrange
        var service = CreateService(out _);
        const string userId = "user-create";
        var cat = new Cat { Name = "Mochi" };

        // Act
        var created = await service.CreateCatAsync(cat, userId);

        // Assert
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(userId, created.UserId);
        Assert.Equal("Mochi", created.Name);
        Assert.True(created.CreatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task UpdateCatAsync_ThrowsOrIgnores_WhenCatBelongsToDifferentUser()
    {
        // Arrange
        var service = CreateService(out var factory);
        const string ownerUser = "user-owner";
        const string otherUser = "user-other";

        var existing = await SeedCatAsync(factory, ownerUser, "Cleo");

        var intruderUpdate = new Cat { Id = existing.Id, Name = "Hacked" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateCatAsync(intruderUpdate, otherUser));
    }

    [Fact]
    public async Task DeleteCatAsync_DeletesCat_WhenOwner()
    {
        // Arrange
        var service = CreateService(out var factory);
        const string userId = "user-delete-owner";

        var cat = await SeedCatAsync(factory, userId, "Felix");

        // Act
        await service.DeleteCatAsync(cat.Id, userId);

        // Assert – cat should no longer be found
        var found = await service.GetCatAsync(cat.Id, userId);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteCatAsync_DoesNotDelete_WhenNotOwner()
    {
        // Arrange
        var service = CreateService(out var factory);
        const string ownerUser = "user-delete-real";
        const string otherUser = "user-delete-other";

        var cat = await SeedCatAsync(factory, ownerUser, "Garfield");

        // Act & Assert – service must throw rather than silently succeed
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.DeleteCatAsync(cat.Id, otherUser));

        // Verify the cat still exists for the real owner
        var stillExists = await service.GetCatAsync(cat.Id, ownerUser);
        Assert.NotNull(stillExists);
    }
}
