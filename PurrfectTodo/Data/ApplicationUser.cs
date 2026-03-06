using Microsoft.AspNetCore.Identity;

namespace PurrfectTodo.Data;

/// <summary>
/// Extended Identity user with profile properties for Purrfect Todo.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Gets or sets the user's first name.</summary>
    public string? FirstName { get; set; }

    /// <summary>Gets or sets the user's last name.</summary>
    public string? LastName { get; set; }

    /// <summary>Gets or sets the UTC date/time when the account was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
