using System.ComponentModel.DataAnnotations;

namespace PurrfectTodo.Data;

/// <summary>
/// Represents a cat owned by a user.
/// </summary>
public class Cat
{
    /// <summary>Gets or sets the unique identifier of the cat.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the ID of the owning user.</summary>
    public string UserId { get; set; } = "";

    /// <summary>Gets or sets the owning user.</summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>Gets or sets the cat's name.</summary>
    [MaxLength(100)]
    public string Name { get; set; } = "";

    /// <summary>Gets or sets the UTC date/time when the cat record was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the collection of todos associated with this cat.</summary>
    public ICollection<Todo> Todos { get; set; } = [];
}
