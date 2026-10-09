// ---------------------------------------------------------------------------
// File: LoanConfiguration.cs
// Purpose: EF Core mapping for the Loan borrow record (copy + member + dates).
// Layer: Infrastructure / Persistence / Configurations.
// For juniors: Demonstrates two foreign keys, enum-to-string, and a DB check
//   constraint (DueDate > BorrowedAt) backing a domain rule.
// ---------------------------------------------------------------------------
using LibraryManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagementSystem.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for Loan (borrow record linking copy + member).
/// </summary>
/// <remarks>For juniors: Two FKs + enum-as-string + DB date guard.</remarks>
// Maps Loan (borrow record): two FKs (copy + member), enum as string, DB-level date guard.
public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    /// <summary>
    /// Configures Loan dates, status, relationships, and check constraint.
    /// </summary>
    /// <param name="builder">EF builder for Loan (fluent API).</param>
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        // Fluent API: maps borrow-record dates, status, and two FKs.
        // Primary key (BaseEntity.Id).
        builder.HasKey(x => x.Id);

        // Borrow/due are required; ReturnedAt stays null until return.
        builder.Property(x => x.BorrowedAt).IsRequired();
        builder.Property(x => x.DueDate).IsRequired();
        // Null until the book is returned.
        builder.Property(x => x.ReturnedAt);

        // LoanStatus enum -> string ("Active", "Returned"...) for readability.
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.RenewalCount).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);

        // Loan -> BookCopy (many-to-one). Restrict so a loaned copy can't be deleted.
        builder.HasOne(x => x.BookCopy)
            .WithMany(c => c.Loans)
            .HasForeignKey(x => x.BookCopyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Loan -> Member (many-to-one). Restrict keeps history if member is removed.
        builder.HasOne(x => x.Member)
            .WithMany(m => m.Loans)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // DB guard backing the domain rule: due date must be after borrow date.
        builder.ToTable(t => t.HasCheckConstraint("CK_Loan_DueDate", "[DueDate] > [BorrowedAt]"));
    }
}
