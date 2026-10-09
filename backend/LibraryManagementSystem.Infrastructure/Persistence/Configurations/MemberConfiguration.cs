// ---------------------------------------------------------------------------
// File: MemberConfiguration.cs
// Purpose: EF Core mapping for the Member entity (identity + loan history).
// Layer: Infrastructure / Persistence / Configurations.
// For juniors: Shows unique email index for login identity and ignoring the
//   computed FullName property so EF does not try to map it.
// ---------------------------------------------------------------------------
using LibraryManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagementSystem.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for Member (identity via unique email + loan history).
/// </summary>
/// <remarks>For juniors: Unique email = one account; FullName is ignored.</remarks>
// Maps Member: unique email for login/identity, computed FullName ignored.
public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    /// <summary>
    /// Configures Member columns and the one-to-many Loans relationship.
    /// </summary>
    /// <param name="builder">EF builder for Member (fluent API).</param>
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        // Fluent API: maps member identity fields.
        // Primary key (BaseEntity.Id).
        builder.HasKey(x => x.Id);

        // Required first/last names for FullName + display.
        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(50);

        // Computed as FirstName + LastName, no setter -> ignored by EF.
        builder.Ignore(x => x.FullName);

        // Unique so one email = one account.
        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(x => x.Email).IsUnique();

        builder.Property(x => x.Phone).HasMaxLength(20);

        builder.Property(x => x.MembershipDate).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);

        // Member -> Loans (one-to-many). Restrict preserves loan history on member delete.
        builder.HasMany(x => x.Loans)
            .WithOne(l => l.Member)
            .HasForeignKey(l => l.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
