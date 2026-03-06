using Microsoft.EntityFrameworkCore;
using PurrfectTodo.Data;

namespace PurrfectTodo.Services;

/// <summary>
/// Provides CRUD and filtering operations for <see cref="Todo"/> entities, scoped to the owning user.
/// </summary>
/// <remarks>
/// Ownership is verified via both <c>UserId</c> and <c>CatId</c> where applicable, per ADR-002.
/// Follows the service pattern defined in ADR-003.
/// </remarks>
public class TodoService(IDbContextFactory<ApplicationDbContext> factory)
{
    /// <summary>
    /// Returns todos for the specified user, with optional filters.
    /// </summary>
    /// <param name="userId">The owning user's ID.</param>
    /// <param name="catId">Optional — restrict to a specific cat.</param>
    /// <param name="category">Optional — restrict to a specific category.</param>
    /// <param name="priority">Optional — restrict to a specific priority.</param>
    /// <param name="isCompleted">Optional — filter by completion status.</param>
    public async Task<List<Todo>> GetTodosAsync(
        string userId,
        Guid? catId = null,
        Category? category = null,
        Priority? priority = null,
        bool? isCompleted = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.Todos
            .AsNoTracking()
            .Include(t => t.Cat)
            .Where(t => t.UserId == userId);

        if (catId.HasValue)
            query = query.Where(t => t.CatId == catId.Value);

        if (category.HasValue)
            query = query.Where(t => t.Category == category.Value);

        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);

        if (isCompleted.HasValue)
            query = query.Where(t => t.IsCompleted == isCompleted.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Returns a single todo by ID, or <c>null</c> if not found or not owned by the user.
    /// </summary>
    public async Task<Todo?> GetTodoAsync(Guid id, string userId)
    {
        using var db = await factory.CreateDbContextAsync();
        return await db.Todos
            .AsNoTracking()
            .Include(t => t.Cat)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    }

    /// <summary>
    /// Creates a new todo, stamping <paramref name="userId"/> as the owner.
    /// </summary>
    public async Task<Todo> CreateTodoAsync(Todo todo, string userId)
    {
        using var db = await factory.CreateDbContextAsync();
        todo.Id = Guid.NewGuid();
        todo.UserId = userId;
        todo.CreatedAt = DateTime.UtcNow;
        todo.UpdatedAt = DateTime.UtcNow;
        db.Todos.Add(todo);
        await db.SaveChangesAsync();
        return todo;
    }

    /// <summary>
    /// Updates an existing todo after verifying ownership.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the todo is not found or not owned by the user.</exception>
    public async Task UpdateTodoAsync(Todo todo, string userId)
    {
        using var db = await factory.CreateDbContextAsync();
        var existing = await db.Todos
            .FirstOrDefaultAsync(t => t.Id == todo.Id && t.UserId == userId);
        if (existing is null)
            throw new KeyNotFoundException($"Todo {todo.Id} not found for user {userId}.");

        existing.Title = todo.Title;
        existing.Category = todo.Category;
        existing.Priority = todo.Priority;
        existing.DueDate = todo.DueDate;
        existing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a todo after verifying ownership.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the todo is not found or not owned by the user.</exception>
    public async Task DeleteTodoAsync(Guid id, string userId)
    {
        using var db = await factory.CreateDbContextAsync();
        var todo = await db.Todos
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (todo is null)
            throw new KeyNotFoundException($"Todo {id} not found for user {userId}.");

        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Marks a todo as completed or incomplete, updating <c>CompletedAt</c> accordingly.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the todo is not found or not owned by the user.</exception>
    public async Task CompleteTodoAsync(Guid id, string userId, bool complete)
    {
        using var db = await factory.CreateDbContextAsync();
        var todo = await db.Todos
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (todo is null)
            throw new KeyNotFoundException($"Todo {id} not found for user {userId}.");

        todo.IsCompleted = complete;
        todo.CompletedAt = complete ? DateTime.UtcNow : null;
        todo.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
