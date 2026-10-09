// ---------------------------------------------------------------------------
// File: BookConfiguration.cs
// Purpose: EF Core mapping for the Book catalog entry (ISBN, links, copies).
// Layer: Infrastructure / Persistence / Configurations.
// For juniors: Defines constraints (required, max length, unique ISBN) and
//   relationships to Author, Category, and BookCopy.
// ---------------------------------------------------------------------------
using LibraryManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagementSystem.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for Book (catalog entry with ISBN, Author, Category, Copies).
/// </summary>
/// <remarks>For juniors: Enforces unique ISBN and link rules to keep catalog clean.</remarks>
// Maps Book (catalog entry): unique ISBN, links to Author/Category, owns its physical Copies.
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    /// <summary>
    /// Configures Book columns, indexes, and relationships.
    /// </summary>
    /// <param name="builder">EF builder for Book (fluent API).</param>
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // Fluent API: each builder call maps one column/relation to the DB.
        // Primary key (BaseEntity.Id).
        builder.HasKey(x => x.Id);

        // Required title, capped at 200 chars -> NVARCHAR(200) NOT NULL.
        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        // ISBN-13. Unique index prevents cataloging the same edition twice.
        builder.Property(x => x.Isbn)
            .IsRequired()
            .HasMaxLength(13);

        builder.HasIndex(x => x.Isbn).IsUnique();

        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Publisher).HasMaxLength(100);

        builder.Property(x => x.Language)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.CoverImageUrl).HasMaxLength(500);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);

        // Book -> Author (many-to-one). Restrict so deleting an author with books fails.
        builder.HasOne(x => x.Author)
            .WithMany(a => a.Books)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Book -> Category (many-to-one). Same Restrict reasoning as Author.
        builder.HasOne(x => x.Category)
            .WithMany(c => c.Books)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Book -> Copies (one-to-many). Cascade: deleting a book removes its physical copies.
        builder.HasMany(x => x.Copies)
            .WithOne(c => c.Book)
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
