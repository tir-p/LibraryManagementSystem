// ---------------------------------------------------------------------------
// File: LibraryDbContext.cs
// Purpose: EF Core DbContext - entry point for database access.
// Layer: Infrastructure / Persistence.
// For juniors: Exposes one DbSet per aggregate root and auto-discovers all
//   IEntityTypeConfiguration classes via ApplyConfigurationsFromAssembly().
//   Domain entities stay persistence-ignorant; mapping lives in Configurations/.
// ---------------------------------------------------------------------------
using LibraryManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Infrastructure.Persistence;

/// <summary>
/// EF Core database context - entry point for all database access.
/// </summary>
/// <remarks>
/// For juniors: Think of this as the session with the database. Domain entities
/// stay persistence-ignorant; mapping lives in Persistence/Configurations.
/// </remarks>
// EF Core entry point. Domain entities stay persistence-ignorant;
// all table mapping lives in Persistence/Configurations.
public class LibraryDbContext : DbContext
{
    /// <summary>
    /// Creates a new context with the configured options (connection, provider).
    /// </summary>
    /// <param name="options">DbContext options built in DI (UseSqlServer + conn string).</param>
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
        // Empty: options (provider + conn string from DI) flow to base DbContext.
    }

    // One DbSet per aggregate root. Property name becomes the table name by default.
    /// <summary>Authors table.</summary>
    public DbSet<Author> Authors => Set<Author>();
    /// <summary>Categories lookup table.</summary>
    public DbSet<Category> Categories => Set<Category>();
    /// <summary>Book catalog entries.</summary>
    public DbSet<Book> Books => Set<Book>();
    /// <summary>Physical copies (borrowable items).</summary>
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    /// <summary>Library members.</summary>
    public DbSet<Member> Members => Set<Member>();
    /// <summary>Borrow records.</summary>
    public DbSet<Loan> Loans => Set<Loan>();

    /// <summary>
    /// Applies all entity mappings at model build time.
    /// </summary>
    /// <param name="modelBuilder">EF builder collecting all configurations.</param>
    /// <remarks>
    /// For juniors: Called once by EF to build the model. No manual registration
    /// needed - just add a new *Configuration class.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Must call base first so default conventions apply.
        base.OnModelCreating(modelBuilder);
        // Auto-discovers every IEntityTypeConfiguration in this assembly,
        // so adding a new *Configuration class is enough (no manual registration).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }
}
