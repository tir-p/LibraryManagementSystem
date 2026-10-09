// ---------------------------------------------------------------------------
// File: AuthorConfiguration.cs
// Purpose: EF Core mapping for the Author entity (table, columns, relations).
// Layer: Infrastructure / Persistence / Configurations.
// For juniors: IEntityTypeConfiguration keeps mapping out of the DbContext and
//   is auto-discovered via ApplyConfigurationsFromAssembly().
// ---------------------------------------------------------------------------
using LibraryManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagementSystem.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the Author entity (table, columns, Books relation).
/// </summary>
/// <remarks>For juniors: Keeps Author table rules out of DbContext.</remarks>
// Maps Author entity to its table: column types, lengths, and the Author -> Books relationship.
public class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    /// <summary>
    /// Configures Author columns and the one-to-many Books relationship.
    /// </summary>
    /// <param name="builder">EF builder for Author (fluent API).</param>
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        // Fluent API: each builder call maps one column/relation to the DB.
        // Primary key (comes from BaseEntity.Id).
        builder.HasKey(x => x.Id);

        // Required name, capped at 100 chars -> NVARCHAR(100) NOT NULL.
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Optional bio, capped to avoid NVARCHAR(MAX).
        builder.Property(x => x.Biography)
            .HasMaxLength(2000);

        // Audit fields from BaseEntity.
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);

        // One author has many books. Restrict = deleting an author with books fails
        // instead of cascade-deleting the catalog.
        builder.HasMany(x => x.Books)
            .WithOne(b => b.Author)
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
