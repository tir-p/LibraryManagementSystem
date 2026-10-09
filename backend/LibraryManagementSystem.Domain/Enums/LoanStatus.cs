// File: LoanStatus.cs
// Purpose: Defines the lifecycle states for a Loan.
// Stored as strings in the database (see LoanConfiguration).
namespace LibraryManagementSystem.Domain.Enums;

/// <summary>
/// Lifecycle state of a <see cref="Entities.Loan"/>.
/// </summary>
// Lifecycle of a Loan. Stored as strings in the DB (see LoanConfiguration).
public enum LoanStatus
{
    /// <summary>
    /// The item is currently checked out and not yet returned.
    /// </summary>
    Active = 0,
    /// <summary>
    /// The item was returned and the loan is closed.
    /// </summary>
    Returned = 1,
    /// <summary>
    /// The loan passed its due date without being returned or renewed.
    /// </summary>
    Overdue = 2,
    /// <summary>
    /// The borrowed item was reported lost.
    /// </summary>
    Lost = 3
}
