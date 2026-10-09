// File: Member.cs
// Purpose: Defines the Member domain entity (a library user who borrows books).
// Email is unique. Borrow rules (active membership + loan cap) live in CanBorrow.
using LibraryManagementSystem.Domain.Exceptions;

namespace LibraryManagementSystem.Domain.Entities;

/// <summary>
/// Represents a library user who can borrow books.
/// Inherits identity and audit fields from <see cref="BaseEntity"/>.
/// </summary>
// A library user who borrows books. Email is unique (enforced in EF config).
public class Member : BaseEntity
{
    /// <summary>
    /// Gets the member's first name. Required, never blank.
    /// </summary>
    public string FirstName { get; private set; }
    /// <summary>
    /// Gets the member's last name. Required, never blank.
    /// </summary>
    public string LastName { get; private set; }
    /// <summary>
    /// Gets the member's full name ("FirstName LastName"). Computed, has no setter.
    /// </summary>
    // Computed, no setter: EF is told to Ignore it (see MemberConfiguration).
    public string FullName => $"{FirstName} {LastName}";
    /// <summary>
    /// Gets the member's email address. Required and unique in the database.
    /// </summary>
    public string Email { get; private set; }
    /// <summary>
    /// Gets the optional phone number. May be null.
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the membership started.
    /// </summary>
    public DateTime MembershipDate { get; private set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets a value indicating whether the membership is currently active.
    /// Inactive members cannot borrow. Changed via <see cref="Deactivate"/> / <see cref="Reactivate"/>.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets the loan history for this member.
    /// Navigation property used by EF Core for the one-to-many side.
    /// </summary>
    public ICollection<Loan> Loans { get; private set; } = new List<Loan>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Member"/> class for EF Core.
    /// Required so EF Core can re-create the object when reading from the database.
    /// Application code should use the public constructor instead.
    /// </summary>
    // EF Core re-hydration only.
    protected Member()
    {
        // Safe defaults so non-nullable strings are never null after EF creates the object.
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Member"/> class.
    /// </summary>
    /// <param name="firstName">First name. Cannot be empty.</param>
    /// <param name="lastName">Last name. Cannot be empty.</param>
    /// <param name="email">Email address. Cannot be empty; must be unique.</param>
    /// <param name="phone">Optional phone number.</param>
    /// <exception cref="DomainException">Thrown when a required value is missing.</exception>
    public Member(string firstName, string lastName, string email, string? phone = null)
    {
        // Guards: a member record is useless without a name and a contact email.
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        // Trim to avoid hidden spaces that would break display or duplicate checks.
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim();
        Phone = phone;
    }

    /// <summary>
    /// Updates the member's name and phone number. Email is intentionally not changed here.
    /// </summary>
    /// <param name="firstName">New first name. Cannot be empty.</param>
    /// <param name="lastName">New last name. Cannot be empty.</param>
    /// <param name="phone">New phone number. May be null.</param>
    /// <exception cref="DomainException">Thrown when a name is empty.</exception>
    public void Update(string firstName, string lastName, string? phone)
    {
        // Same guards as the constructor: names must stay valid on every update.
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone;
        // Refresh UpdatedAt so we know when this row last changed.
        Touch();
    }

    /// <summary>
    /// Deactivates the membership so the member can no longer borrow.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        // Refresh UpdatedAt so we know when this row last changed.
        Touch();
    }

    /// <summary>
    /// Reactivates a previously deactivated membership.
    /// </summary>
    public void Reactivate()
    {
        IsActive = true;
        // Refresh UpdatedAt so we know when this row last changed.
        Touch();
    }

    /// <summary>
    /// Checks whether this member is allowed to borrow another book.
    /// </summary>
    /// <param name="activeLoanCount">How many loans the member currently has open.</param>
    /// <param name="maxLoans">Maximum allowed open loans. Defaults to 5.</param>
    /// <returns>True when the membership is active and the member is under the loan cap.</returns>
    // Borrow policy used by LoanService: active membership + under the loan cap (default 5).
    public bool CanBorrow(int activeLoanCount, int maxLoans = 5)
    {
        // Both conditions must hold: active account AND room left under the cap.
        return IsActive && activeLoanCount < maxLoans;
    }
}
