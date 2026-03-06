using PurrfectTodo.Data;
using PurrfectTodo.Services;
using PurrfectTodo.Tests.Helpers;

namespace PurrfectTodo.Tests;

/// <summary>
/// Unit tests for <see cref="TodoService"/>.
/// Each test uses an isolated InMemory database to guarantee independence.
/// </summary>
public class TodoServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TodoService CreateService(out TestDbContextFactory factory)
    {
        factory = TestDbContextFactory.CreateIsolated();
        return new TodoService(factory);
    }

    /// <summary>
    /// Seeds a <see cref="Cat"/> directly into the test database and returns it.
    /// The service methods filter by <c>UserId</c>, so we need cats in the store
    /// for the Include() in <see cref="TodoService.GetTodosAsync"/> to resolve.
    /// </summary>
    private static async Task<Cat> SeedCatAsync(
        TestDbContextFactory factory, string userId, string name = "TestCat")
    {
        var cat = new Cat { Id = Guid.NewGuid(), UserId = userId, Name = name, CreatedAt = DateTime.UtcNow };
        await using var db = factory.CreateDbContext();
        db.Cats.Add(cat);
        await db.SaveChangesAsync();
        return cat;
    }

    /// <summary>
    /// Seeds a <see cref="Todo"/> directly into the test database.
    /// </summary>
    private static async Task<Todo> SeedTodoAsync(
        TestDbContextFactory factory,
        string userId,
        Guid catId,
        string title = "Test Todo",
        Category category = Category.Other,
        bool isCompleted = false)
    {
        var todo = new Todo
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CatId = catId,
            Title = title,
            Category = category,
            IsCompleted = isCompleted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await using var db = factory.CreateDbContext();
        db.Todos.Add(todo);
        await db.SaveChangesAsync();
        return todo;
    }

    // ── GetTodosAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTodosAsync_ReturnsOnlyTodosForUser()
    {
        // Arrange
        var service = CreateService(out var factory);
        const string userA = "user-a";
        const string userB = "user-b";

        var catA = await SeedCatAsync(factory, userA);
        var catB = await SeedCatAsync(factory, userB);

        await SeedTodoAsync(factory, userA, catA.Id, "A Task 1");
        await SeedTodoAsync(factory, userA, catA.Id, "A Task 2");
        await SeedTodoAsync(factory, userB, catB.Id, "B Task 1");  // must not appear

        // Act
        var todos = await service.GetTodosAsync(userA);

        // Assert
        Assert.Equal(2, todos.Count);
        Assert.All(todos, t => Assert.Equal(userA, t.UserId));
        Assert.DoesNotContain(todos, t => t.Title == "B Task 1");
    }

    [Fact]
    public async Task GetTodosAsync_FiltersByCatId()
    {
        // Arrange
        var service = CreateService(out var factory);
        const string userId = "user-filter-cat";

        var cat1 = await SeedCatAsync(factory, userId, "Cat One");
        var cat2 = await SeedCatAsync(factory, userId, "Cat Two");

        await SeedTodoAsync(factory, userId, cat1.Id, "Feed Cat One");
        await SeedTodoAsync(factory, userId, cat1.Id, "Groom Cat One");
        await SeedTodoAsync(factory, userId, cat2.Id, "Feed Cat Two");  // different cat

        // Act
        var todos = await service.GetTodosAsync(userId, catId: cat1.Id);

        // Assert
        Assert.Equal(2, todos.Count);
        Assert.All(todos, t => Assert.Equal(cat1.Id, t.CatId));
        Assert.DoesNotContain(todos, t => t.Title == "Feed Cat Two");
    }

    [Fact]
    public async Task GetTodosAsync_FiltersByCategory()
    {
        // Arrange
        var service = CreateService(out var factory);
        const string userId = "user-filter-category";

        var cat = await SeedCatAsync(factory, userId);

        await SeedTodoAsync(factory, userId, cat.Id, "Vet Appointment", Category.Vet);
        await SeedTodoAsync(factory, userId, cat.Id, "Evening Feeding", Category.Feeding);
        await SeedTodoAsync(factory, userId, cat.Id, "Morning Feeding", Category.Feeding);

        // Act
        var todos = await service.GetTodosAsync(userId, category: Category.Feeding);

        // Assert
        Assert.Equal(2, todos.Count);
        Assert.All(todos, t => Assert.Equal(Category.Feeding, t.Category));
        Assert.DoesNotContain(todos, t => t.Title == "Vet Appointment");
    }

    [Fact]
    public async Task GetTodosAsync_FiltersByIsCompleted()
    {
        // Arrange
        var service = CreateService(out var factory);
        const string userId = "user-filter-completed";

        var cat = await SeedCatAsync(factory, userId);

        await SeedTodoAsync(factory, userId, cat.Id, "Done Task", isCompleted: true);
        await SeedTodoAsync(factory, userId, cat.Id, "Pending Task", isCompleted: false);

        // Act
        var completed = await service.GetTodosAsync(userId, isCompleted: true);
        var pending = await service.GetTodosAsync(userId, isCompleted: false);

        // Assert
        Assert.Single(completed);
        Assert.Equal("Done Task", completed[0].Title);

        Assert.Single(pending);
        Assert.Equal("Pending Task", pending[0].Title);
    }

    // ── CreateTodoAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTodoAsync_StampsUserIdAndTimestamps()
    {
        // Arrange
        var service = CreateService(out _);
        const string userId = "user-create";
        var before = DateTime.UtcNow.AddSeconds(-1);

        var todo = new Todo { Title = "New Task", CatId = Guid.NewGuid() };

        // Act
        var created = await service.CreateTodoAsync(todo, userId);

        // Assert
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(userId, created.UserId);
        Assert.Equal("New Task", created.Title);
        Assert.True(created.CreatedAt >= before, "CreatedAt should be set to approximately now");
        Assert.True(created.UpdatedAt >= before, "UpdatedAt should be set to approximately now");
    }

    // ── CompleteTodoAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteTodoAsync_SetsIsCompletedAndCompletedAt()
    {
        // Arrange
        var service = CreateService(out var factory);
        const string userId = "user-complete";

        var cat = await SeedCatAsync(factory, userId);
        var todo = await SeedTodoAsync(factory, userId, cat.Id, "Feed Whiskers");

        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        await service.CompleteTodoAsync(todo.Id, userId, complete: true);

        // Assert – reload from a fresh context to confirm persistence
        await using var db = factory.CreateDbContext();
        var persisted = await db.Todos.FindAsync(todo.Id);
        Assert.NotNull(persisted);
        Assert.True(persisted.IsCompleted);
        Assert.NotNull(persisted.CompletedAt);
        Assert.True(persisted.CompletedAt >= before);
    }

    [Fact]
    public async Task CompleteTodoAsync_DoesNotComplete_WhenNotOwner()
    {
        // Arrange
        var service = CreateService(out var factory);
        const string ownerUser = "user-complete-owner";
        const string otherUser = "user-complete-other";

        var cat = await SeedCatAsync(factory, ownerUser);
        var todo = await SeedTodoAsync(factory, ownerUser, cat.Id, "Groom Kitty");

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CompleteTodoAsync(todo.Id, otherUser, complete: true));

        // Verify: todo remains incomplete for the real owner
        await using var db = factory.CreateDbContext();
        var persisted = await db.Todos.FindAsync(todo.Id);
        Assert.NotNull(persisted);
        Assert.False(persisted.IsCompleted);
    }

    // ── DeleteTodoAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteTodoAsync_DoesNotDelete_WhenNotOwner()
    {
        // Arrange
        var service = CreateService(out var factory);
        const string ownerUser = "user-delete-owner";
        const string otherUser = "user-delete-other";

        var cat = await SeedCatAsync(factory, ownerUser);
        var todo = await SeedTodoAsync(factory, ownerUser, cat.Id, "Vet Visit");

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.DeleteTodoAsync(todo.Id, otherUser));

        // Verify: todo still exists for the real owner
        await using var db = factory.CreateDbContext();
        var persisted = await db.Todos.FindAsync(todo.Id);
        Assert.NotNull(persisted);
    }
}
