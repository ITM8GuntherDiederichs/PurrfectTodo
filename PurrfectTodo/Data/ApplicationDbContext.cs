using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PurrfectTodo.Data;

/// <summary>
/// EF Core database context for Purrfect Todo.
/// Inherits from <see cref="IdentityDbContext{TUser}"/> to include ASP.NET Identity tables.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    /// <summary>Gets or sets the cats table.</summary>
    public DbSet<Cat> Cats { get; set; }

    /// <summary>Gets or sets the todos table.</summary>
    public DbSet<Todo> Todos { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── ApplicationUser ───────────────────────────────────────────────────
        builder.Entity<ApplicationUser>()
            .Property(u => u.FirstName)
            .HasMaxLength(100);

        builder.Entity<ApplicationUser>()
            .Property(u => u.LastName)
            .HasMaxLength(100);

        // ── Cat ──────────────────────────────────────────────────────────────
        builder.Entity<Cat>()
            .Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Entity<Cat>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Cat>()
            .HasIndex(c => c.UserId);

        // ── Todo ─────────────────────────────────────────────────────────────
        builder.Entity<Todo>()
            .Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        // Cat → Todo: cascade delete (deleting a cat deletes its todos)
        builder.Entity<Todo>()
            .HasOne(t => t.Cat)
            .WithMany(c => c.Todos)
            .HasForeignKey(t => t.CatId)
            .OnDelete(DeleteBehavior.Cascade);

        // Todo → User: restrict to prevent multiple cascade paths
        builder.Entity<Todo>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Todo>()
            .HasIndex(t => t.UserId);

        builder.Entity<Todo>()
            .HasIndex(t => t.CatId);

        builder.Entity<Todo>()
            .HasIndex(t => new { t.UserId, t.IsCompleted });

        builder.Entity<Todo>()
            .HasIndex(t => new { t.UserId, t.DueDate });
    }
}
