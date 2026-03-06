using System.ComponentModel.DataAnnotations;

namespace PurrfectTodo.Data;

/// <summary>
/// Represents a care task (todo) for a specific cat.
/// </summary>
public class Todo
{
    /// <summary>Gets or sets the unique identifier of the todo.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the ID of the cat this todo belongs to.</summary>
    public Guid CatId { get; set; }

    /// <summary>Gets or sets the cat this todo belongs to.</summary>
    public Cat Cat { get; set; } = null!;

    /// <summary>Gets or sets the ID of the owning user (denormalised for efficient filtering).</summary>
    public string UserId { get; set; } = "";

    /// <summary>Gets or sets the owning user.</summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>Gets or sets the title of the todo.</summary>
    [MaxLength(500)]
    public string Title { get; set; } = "";

    /// <summary>Gets or sets the category of the care task.</summary>
    public Category Category { get; set; } = Category.Other;

    /// <summary>Gets or sets the priority level of the todo.</summary>
    public Priority Priority { get; set; } = Priority.Medium;

    /// <summary>Gets or sets the optional due date.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Gets or sets whether the todo has been completed.</summary>
    public bool IsCompleted { get; set; }

    /// <summary>Gets or sets the UTC date/time when the todo was completed.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Gets or sets the UTC date/time when the todo was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC date/time when the todo was last updated.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
