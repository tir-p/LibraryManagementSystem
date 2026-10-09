// ---------------------------------------------------------------------------
// File: CategoryConfiguration.cs
// Purpose: EF Core mapping for the Category entity (unique name + books).
// Layer: Infrastructure / Persistence / Configurations.
// For juniors: Example of a simple lookup-table mapping with a unique index
//   to prevent duplicate category names.
// ---------------------------------------------------------------------------
using LibraryManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagementSystem.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for Category (lookup table with unique names).
/// </summary>
/// <remarks>For juniors: Unique index prevents duplicate "Fiction", etc.</remarks>
// Maps Category: enforces unique names so "Fiction" can't be duplicated.
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <summary>
    /// Configures Category columns and the one-to-many Books relationship.
    /// </summary>
    /// <param name="builder">EF builder for Category (fluent API).</param>
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // Fluent API: maps lookup-table columns.
        // Primary key (BaseEntity.Id).
        builder.HasKey(x => x.Id);

        // Required short name -> NVARCHAR(50) NOT NULL.
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        // Unique index: same category name can't appear twice.
        builder.HasIndex(x => x.Name).IsUnique();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);

        // One category has many books. Restrict protects books if category is deleted.
        builder.HasMany(x => x.Books)
            .WithOne(b => b.Category)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
