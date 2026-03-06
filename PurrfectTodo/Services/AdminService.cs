using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PurrfectTodo.Data;

namespace PurrfectTodo.Services;

/// <summary>
/// Provides site-wide administration operations (user management).
/// </summary>
/// <remarks>
/// All methods on this service are intended for use by callers in the <c>Admin</c> role only.
/// Authorisation is enforced at the page/component level; this service does not re-check roles.
/// </remarks>
public class AdminService(
    IDbContextFactory<ApplicationDbContext> factory,
    UserManager<ApplicationUser> userManager)
{
    /// <summary>
    /// Returns all registered users.
    /// </summary>
    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        using var db = await factory.CreateDbContextAsync();
        return await db.Users
            .OrderBy(u => u.Email)
            .ToListAsync();
    }

    /// <summary>
    /// Deletes a user and all of their associated data (cats and todos are cascade-deleted via FK).
    /// </summary>
    /// <param name="userId">The ID of the user to delete.</param>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    public async Task DeleteUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to delete user {userId}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }
}
