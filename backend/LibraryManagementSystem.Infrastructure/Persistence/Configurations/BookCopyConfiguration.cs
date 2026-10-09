// ---------------------------------------------------------------------------
// File: BookCopyConfiguration.cs
// Purpose: EF Core mapping for BookCopy (physical, borrowable item).
// Layer: Infrastructure / Persistence / Configurations.
// For juniors: Shows enum-to-string conversion, unique barcode index, and how
//   to ignore computed properties (IsAvailable) that EF should not map.
// ---------------------------------------------------------------------------
using LibraryManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagementSystem.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for BookCopy (physical item with barcode and status).
/// </summary>
/// <remarks>For juniors: Shows enum-to-string and ignoring computed props.</remarks>
// Maps BookCopy (physical item): unique barcode, enum stored as string, computed prop ignored.
public class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    /// <summary>
    /// Configures BookCopy columns, barcode uniqueness, and Book/Loan links.
    /// </summary>
    /// <param name="builder">EF builder for BookCopy (fluent API).</param>
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        // Fluent API: maps physical copy fields and links.
        // Primary key (BaseEntity.Id).
        builder.HasKey(x => x.Id);

        // Physical label. Unique so two copies never share a barcode.
        builder.Property(x => x.Barcode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Barcode).IsUnique();

        // Human location like "A-3-12". Optional.
        builder.Property(x => x.ShelfLocation).HasMaxLength(50);

        // CopyStatus enum -> readable string ("Available", "Loaned"...) instead of int.
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Computed from Status, has no setter -> must be ignored or EF throws at startup.
        builder.Ignore(x => x.IsAvailable);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);

        // Copy -> Book (many-to-one). Cascade with Book.Copies above.
        builder.HasOne(x => x.Book)
            .WithMany(b => b.Copies)
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        // Copy -> Loans (one-to-many). Restrict keeps loan history even if copy is removed.
        builder.HasMany(x => x.Loans)
            .WithOne(l => l.BookCopy)
            .HasForeignKey(l => l.BookCopyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
