// ---------------------------------------------------------------------------
// File: DataSeeder.cs
// Purpose: Seeds demo categories, authors, books, copies, members, and loans.
// Layer: Infrastructure / Persistence - runs once on startup (see Program.cs).
// For juniors: All lookups upsert by natural key (name/ISBN/barcode/email) so
//   re-runs never duplicate rows or wipe real user data. Loans seed only when
//   the demo copies have no loans yet (they have no natural key).
// ---------------------------------------------------------------------------
using LibraryManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Infrastructure.Persistence;

/// <summary>
/// Seeds demo data (catalog, members, loan history) on startup.
/// </summary>
/// <remarks>
/// For juniors: Everything upserts by natural key so re-runs are safe.
/// Loans seed only when demo copies have no loans yet.
/// </remarks>
// Seeds the demo catalog, members, and loan history. Runs on startup
// (see Program.cs). Everything upserts by natural key (ISBN / barcode /
// email), so restarts and re-runs never duplicate rows or wipe user data.
// Loans seed only when the table is empty (they have no natural key).
public static class DataSeeder
{
    /// <summary>
    /// Seeds all demo data in order: categories, authors, books, copies, members, loans.
    /// </summary>
    /// <param name="db">Database context to read/write seed rows.</param>
    public static async Task SeedAsync(LibraryDbContext db)
    {
        // Seed lookup tables first (others depend on their Ids).
        var programming = await GetOrCreateCategory(db, "Programming", "Software development books");
        var selfHelp = await GetOrCreateCategory(db, "Self-Help", "Productivity and personal growth");
        var fiction = await GetOrCreateCategory(db, "Fiction", "Novels and stories");
        var science = await GetOrCreateCategory(db, "Science", "Popular science books");
        var history = await GetOrCreateCategory(db, "History", "History and civilization");
        var mystery = await GetOrCreateCategory(db, "Mystery", "Crime and thrillers");

        var martin = await GetOrCreateAuthor(db, "Robert C. Martin");
        var thomas = await GetOrCreateAuthor(db, "David Thomas");
        var gamma = await GetOrCreateAuthor(db, "Erich Gamma");
        var fowler = await GetOrCreateAuthor(db, "Martin Fowler");
        var clear = await GetOrCreateAuthor(db, "James Clear");
        var herbert = await GetOrCreateAuthor(db, "Frank Herbert");
        var orwell = await GetOrCreateAuthor(db, "George Orwell");
        var harari = await GetOrCreateAuthor(db, "Yuval Noah Harari");
        var hawking = await GetOrCreateAuthor(db, "Stephen Hawking");
        var tolkien = await GetOrCreateAuthor(db, "J.R.R. Tolkien");
        var larsson = await GetOrCreateAuthor(db, "Stieg Larsson");
        var kahneman = await GetOrCreateAuthor(db, "Daniel Kahneman");

        // Original catalog (kept first so existing barcodes stay stable).
        await GetOrCreateBook(db,
            "Clean Code: A Handbook of Agile Software Craftsmanship",
            "9780132350884", martin.Id, programming.Id,
            2008, 464,
            "Even bad code can function. But if code isn't clean, it can bring a development organization to its knees.",
            "Prentice Hall");
        await GetOrCreateBook(db,
            "The Pragmatic Programmer",
            "9780135957059", thomas.Id, programming.Id,
            2019, 352,
            "From journeyman to master. Tips, tricks and pragmatic approaches to modern software development.",
            "Addison-Wesley");
        await GetOrCreateBook(db,
            "Design Patterns: Elements of Reusable Object-Oriented Software",
            "9780201633610", gamma.Id, programming.Id,
            1994, 395,
            "The classic Gang of Four catalog of 23 software design patterns.",
            "Addison-Wesley");
        await GetOrCreateBook(db,
            "Refactoring: Improving the Design of Existing Code",
            "9780134757599", fowler.Id, programming.Id,
            2018, 448,
            "How to safely improve the design of existing code with proven refactorings.",
            "Addison-Wesley");
        await GetOrCreateBook(db,
            "Atomic Habits",
            "9780735211292", clear.Id, selfHelp.Id,
            2018, 320,
            "Tiny changes, remarkable results. A proven framework for improving every day.",
            "Avery");

        // Extended catalog. Barcode prefixes are hand-picked to stay unique
        // (e.g. "Clean Architecture" would otherwise collide with "Clean Code").
        await GetOrCreateBook(db,
            "Clean Architecture: A Craftsman's Guide to Software Structure and Design",
            "9780134494166", martin.Id, programming.Id,
            2017, 432,
            "Rules and patterns for designing sustainable software architectures.",
            "Prentice Hall",
            [("ARCH-001", "A1"), ("ARCH-002", "A1"), ("ARCH-003", "A2")]);
        await GetOrCreateBook(db,
            "Dune",
            "9780441172719", herbert.Id, fiction.Id,
            1965, 688,
            "Paul Atreides journeys to the desert planet Arrakis in this landmark of science fiction.",
            "Ace Books",
            [("DUNE-001", "B1"), ("DUNE-002", "B1")]);
        await GetOrCreateBook(db,
            "1984",
            "9780451524935", orwell.Id, fiction.Id,
            1949, 328,
            "Winston Smith dares to think for himself under the watchful eye of Big Brother.",
            "Signet Classic",
            [("NINE-001", "B2"), ("NINE-002", "B2")]);
        await GetOrCreateBook(db,
            "Sapiens: A Brief History of Humankind",
            "9780062316097", harari.Id, history.Id,
            2015, 464,
            "How Homo sapiens came to rule the world, from the Stone Age to the present.",
            "Harper",
            [("SAPI-001", "C1"), ("SAPI-002", "C1")]);
        await GetOrCreateBook(db,
            "A Brief History of Time",
            "9780553380163", hawking.Id, science.Id,
            1998, 212,
            "From the Big Bang to black holes: cosmology for the curious.",
            "Bantam",
            [("BRIE-001", "C2"), ("BRIE-002", "C2")]);
        await GetOrCreateBook(db,
            "The Hobbit",
            "9780547928227", tolkien.Id, fiction.Id,
            1937, 310,
            "Bilbo Baggins is swept from his comfortable hole into a quest for dragon-guarded gold.",
            "Mariner Books",
            [("HOBB-001", "D1"), ("HOBB-002", "D1")]);
        await GetOrCreateBook(db,
            "The Girl with the Dragon Tattoo",
            "9780307454546", larsson.Id, mystery.Id,
            2008, 656,
            "A disgraced journalist and a hacker dig into a decades-old disappearance.",
            "Vintage Crime",
            [("DRAG-001", "E1"), ("DRAG-002", "E1"), ("DRAG-003", "E1")]);
        await GetOrCreateBook(db,
            "Thinking, Fast and Slow",
            "9780374533557", kahneman.Id, selfHelp.Id,
            2013, 418,
            "The two systems that drive how we think, judge, and decide.",
            "Farrar, Straus and Giroux",
            [("THIN-001", "A2"), ("THIN-002", "A2")]);

        // Original titles get their 2 borrowable copies (barcodes derived
        // from the title, matching the first version of this seeder).
        await EnsureCopiesForTitle(db,
            "Clean Code: A Handbook of Agile Software Craftsmanship",
            ["CLEA-001", "CLEA-002"], "A1");
        await EnsureCopiesForTitle(db,
            "The Pragmatic Programmer",
            ["THEP-001", "THEP-002"], "A1");
        await EnsureCopiesForTitle(db,
            "Design Patterns: Elements of Reusable Object-Oriented Software",
            ["DESI-001", "DESI-002"], "A1");
        await EnsureCopiesForTitle(db,
            "Refactoring: Improving the Design of Existing Code",
            ["REFA-001", "REFA-002"], "A1");
        await EnsureCopiesForTitle(db,
            "Atomic Habits",
            ["ATOM-001", "ATOM-002"], "A1");

        // Demo members. One inactive (Omar) to show borrow blocking + history.
        var alice = await GetOrCreateMember(db, "Alice", "Johnson", "alice.johnson@example.com", "+1 555-0101");
        var maria = await GetOrCreateMember(db, "Maria", "Garcia", "maria.garcia@example.com", "+1 555-0102");
        var james = await GetOrCreateMember(db, "James", "Wilson", "james.wilson@example.com", null);
        var priya = await GetOrCreateMember(db, "Priya", "Patel", "priya.patel@example.com", "+1 555-0104");
        var omar = await GetOrCreateMember(db, "Omar", "Haddad", "omar.haddad@example.com", null, isActive: false);

        // Demo loan history: active, overdue, and returned loans so the
        // dashboard, badges, and member history render realistically.
        // Loans have no natural key, so the demo copies act as the marker:
        // seed only when none of them has any loan yet (reruns stay clean,
        // and real user activity on other copies is never touched).
        var demoBarcodes = new[]
        {
            "DUNE-001", "SAPI-001", "ARCH-001", "HOBB-001",
            "NINE-001", "BRIE-001", "DRAG-001", "CLEA-001",
        };
        var demoCopyIds = await db.BookCopies
            .Where(c => demoBarcodes.Contains(c.Barcode))
            .Select(c => c.Id)
            .ToListAsync();
        if (!await db.Loans.AnyAsync(l => demoCopyIds.Contains(l.BookCopyId)))
        {
            var now = DateTime.UtcNow;
            var seeded = 0;
            seeded += await AddLoan(db, "DUNE-001", alice.Id, now.AddDays(-30), 14, overdue: true) ? 1 : 0;
            seeded += await AddLoan(db, "SAPI-001", maria.Id, now.AddDays(-20), 14, overdue: true) ? 1 : 0;
            seeded += await AddLoan(db, "ARCH-001", james.Id, now.AddDays(-5), 14) ? 1 : 0;
            seeded += await AddLoan(db, "HOBB-001", alice.Id, now.AddDays(-2), 14) ? 1 : 0;
            seeded += await AddLoan(db, "NINE-001", priya.Id, now.AddDays(-3), 14) ? 1 : 0;
            seeded += await AddLoan(db, "BRIE-001", james.Id, now.AddDays(-40), 14, returnedAt: now.AddDays(-10)) ? 1 : 0;
            seeded += await AddLoan(db, "DRAG-001", maria.Id, now.AddDays(-50), 14, returnedAt: now.AddDays(-30)) ? 1 : 0;
            seeded += await AddLoan(db, "CLEA-001", omar.Id, now.AddDays(-60), 14, returnedAt: now.AddDays(-45)) ? 1 : 0;
            if (seeded > 0)
                await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Finds a category by name or creates it (upsert, safe for re-runs).
    /// </summary>
    /// <param name="db">Database context.</param>
    /// <param name="name">Natural key; lookup field (unique).</param>
    /// <param name="description">Description for new rows only.</param>
    /// <returns>Existing or newly created category.</returns>
    private static async Task<Category> GetOrCreateCategory(
        LibraryDbContext db, string name, string? description)
    {
        // Upsert by name: return existing to avoid duplicates on re-run.
        var existing = await db.Categories.FirstOrDefaultAsync(c => c.Name == name);
        if (existing is not null) return existing;
        // Not found: create via domain constructor, then persist immediately
        // so Id is available for books below.
        var category = new Category(name, description);
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    /// <summary>
    /// Finds an author by name or creates them.
    /// </summary>
    /// <param name="db">Database context.</param>
    /// <param name="name">Author full name (natural key).</param>
    /// <returns>Existing or newly created author.</returns>
    private static async Task<Author> GetOrCreateAuthor(
        LibraryDbContext db, string name)
    {
        // Same upsert pattern as categories, keyed by author name.
        var existing = await db.Authors.FirstOrDefaultAsync(a => a.Name == name);
        if (existing is not null) return existing;
        var author = new Author(name);
        db.Authors.Add(author);
        await db.SaveChangesAsync();
        return author;
    }

    /// <summary>
    /// Finds a book by ISBN or creates it, then ensures its copies exist.
    /// </summary>
    /// <param name="db">Database context.</param>
    /// <param name="title">Book title.</param>
    /// <param name="isbn">ISBN-13 natural key (unique).</param>
    /// <param name="authorId">FK to author.</param>
    /// <param name="categoryId">FK to category.</param>
    /// <param name="year">Publication year.</param>
    /// <param name="pages">Page count.</param>
    /// <param name="description">Optional blurb.</param>
    /// <param name="publisher">Optional publisher.</param>
    /// <param name="copies">Optional (barcode, shelf) pairs to ensure.</param>
    /// <returns>Existing or newly created book.</returns>
    private static async Task<Book> GetOrCreateBook(
        LibraryDbContext db, string title, string isbn, Guid authorId, Guid categoryId,
        int year, int pages, string? description, string? publisher,
        (string Barcode, string Shelf)[]? copies = null)
    {
        // ISBN is the natural key: if book exists, only ensure missing copies.
        var existing = await db.Books.FirstOrDefaultAsync(b => b.Isbn == isbn);
        if (existing is not null)
        {
            // New copies for an existing title still get added (by barcode).
            if (copies is not null)
                await EnsureCopies(db, existing.Id, copies);
            return existing;
        }

        // New title: create via domain constructor (validates year/pages).
        var book = new Book(title, isbn, authorId, categoryId, year, pages, description, publisher);
        db.Books.Add(book);
        await db.SaveChangesAsync();

        if (copies is not null)
            await EnsureCopies(db, book.Id, copies);
        return book;
    }

    /// <summary>
    /// Ensures fixed-barcode copies for the original titles (stable barcodes).
    /// </summary>
    /// <param name="db">Database context.</param>
    /// <param name="title">Book title lookup (must already exist).</param>
    /// <param name="barcodes">Barcodes to ensure (skips existing).</param>
    /// <param name="shelf">Shelf location for new copies.</param>
    // Copies for the original titles (fixed barcodes, kept stable).
    private static async Task EnsureCopiesForTitle(
        LibraryDbContext db, string title, string[] barcodes, string shelf)
    {
        // Lookup parent book by title; skip silently if missing (safe re-run).
        var book = await db.Books.FirstOrDefaultAsync(b => b.Title == title);
        if (book is null) return;
        // Convert barcodes to (barcode, shelf) tuples for shared helper.
        await EnsureCopies(db, book.Id, barcodes.Select(b => (b, shelf)).ToArray());
    }

    /// <summary>
    /// Ensures physical copies by barcode (skips duplicates, saves once).
    /// </summary>
    /// <param name="db">Database context.</param>
    /// <param name="bookId">FK to parent book.</param>
    /// <param name="copies">Barcode + shelf pairs to add if missing.</param>
    private static async Task EnsureCopies(
        LibraryDbContext db, Guid bookId, (string Barcode, string Shelf)[] copies)
    {
        // Track if anything added to avoid empty SaveChanges call.
        var changed = false;
        foreach (var (barcode, shelf) in copies)
        {
            // Barcode is unique: skip existing so re-runs stay idempotent.
            if (await db.BookCopies.AnyAsync(c => c.Barcode == barcode))
                continue;
            // New physical copy via domain constructor.
            db.BookCopies.Add(new BookCopy(bookId, barcode, shelf));
            changed = true;
        }
        if (changed)
            await db.SaveChangesAsync();
    }

    /// <summary>
    /// Finds a member by email or creates them (email is the natural key).
    /// </summary>
    /// <param name="db">Database context.</param>
    /// <param name="firstName">Given name.</param>
    /// <param name="lastName">Family name.</param>
    /// <param name="email">Unique email (one account per email).</param>
    /// <param name="phone">Optional phone.</param>
    /// <param name="isActive">False creates an inactive member (borrow blocked).</param>
    /// <returns>Existing or newly created member.</returns>
    private static async Task<Member> GetOrCreateMember(
        LibraryDbContext db, string firstName, string lastName, string email,
        string? phone, bool isActive = true)
    {
        // Email is the natural key (one account per email).
        var existing = await db.Members.FirstOrDefaultAsync(m => m.Email == email);
        if (existing is not null) return existing;
        var member = new Member(firstName, lastName, email, phone);
        // Inactive demo member shows borrow-blocking behavior.
        if (!isActive)
            member.Deactivate();
        db.Members.Add(member);
        await db.SaveChangesAsync();
        return member;
    }

    /// <summary>
    /// Creates a demo loan via domain methods (keeps status rules valid).
    /// </summary>
    /// <param name="db">Database context.</param>
    /// <param name="barcode">Copy barcode to loan (must be available).</param>
    /// <param name="memberId">Borrowing member Id.</param>
    /// <param name="borrowedAt">Borrow date (UTC).</param>
    /// <param name="loanDays">Loan period in days (due = borrowed + days).</param>
    /// <param name="overdue">True to mark the loan overdue for demo.</param>
    /// <param name="returnedAt">If set, loan is seeded as returned.</param>
    /// <returns>True if seeded; false if copy missing/out (never corrupts state).</returns>
    // Creates a loan through the domain methods so status rules hold, and marks
    // the copy loaned (open loans) or leaves it available (returned loans).
    // Returns false (skips) when the copy is missing or currently out, so the
    // seeder can never corrupt real lending state. Returns true when seeded.
    private static async Task<bool> AddLoan(
        LibraryDbContext db, string barcode, Guid memberId,
        DateTime borrowedAt, int loanDays, bool overdue = false, DateTime? returnedAt = null)
    {
        // Guard: skip if copy missing or already loaned (protects real data).
        var copy = await db.BookCopies.FirstOrDefaultAsync(c => c.Barcode == barcode);
        if (copy is null || !copy.IsAvailable) return false;

        // Use domain constructor so due-date rule (borrowed + days) holds.
        var loan = new Loan(copy.Id, memberId, borrowedAt, loanDays);
        if (returnedAt.HasValue)
        {
            // Returned path: close loan via domain, free the copy.
            loan.Return(returnedAt);
            copy.MarkAvailable();
        }
        else
        {
            // Open loan: mark copy loaned; optionally force overdue for demo.
            copy.MarkLoaned();
            if (overdue)
                loan.MarkOverdue();
        }

        // Stage both changes; caller batches SaveChanges once.
        db.Loans.Add(loan);
        db.BookCopies.Update(copy);
        return true;
    }
}
