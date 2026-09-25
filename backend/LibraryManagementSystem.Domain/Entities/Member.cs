using LibraryManagementSystem.Domain.Exceptions;

namespace LibraryManagementSystem.Domain.Entities;

// A library user who borrows books. Email is unique (enforced in EF config).
public class Member : BaseEntity
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    // Computed, no setter: EF is told to Ignore it (see MemberConfiguration).
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; private set; }
    public string? Phone { get; private set; }

    public DateTime MembershipDate { get; private set; } = DateTime.UtcNow;
    public bool IsActive { get; private set; } = true;

    public ICollection<Loan> Loans { get; private set; } = new List<Loan>();

    // EF Core re-hydration only.
    protected Member()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
    }

    public Member(string firstName, string lastName, string email, string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim();
        Phone = phone;
    }

    public void Update(string firstName, string lastName, string? phone)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    public void Reactivate()
    {
        IsActive = true;
        Touch();
    }

    // Borrow policy used by LoanService: active membership + under the loan cap (default 5).
    public bool CanBorrow(int activeLoanCount, int maxLoans = 5)
    {
        return IsActive && activeLoanCount < maxLoans;
    }
}
