namespace LibraryManagementSystem.Domain.Enums;

// Lifecycle of a Loan. Stored as strings in the DB (see LoanConfiguration).
public enum LoanStatus
{
    Active = 0,
    Returned = 1,
    Overdue = 2,
    Lost = 3
}
