namespace LibraryManagementSystem.Domain.Exceptions;

// Thrown when a business rule breaks (blank title, double loan, 3rd renewal...).
// The API's GlobalExceptionHandler turns these into HTTP 400 responses.
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
