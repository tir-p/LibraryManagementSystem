// File: DomainException.cs
// Purpose: Defines the exception thrown when a domain business rule is broken
// (blank title, double loan, third renewal, and so on).
// The API's GlobalExceptionHandler turns these into HTTP 400 responses.
namespace LibraryManagementSystem.Domain.Exceptions;

/// <summary>
/// Exception thrown when a domain business rule is violated.
/// Examples: blank title, loaning an unavailable copy, renewing too many times.
/// </summary>
// Thrown when a business rule breaks (blank title, double loan, 3rd renewal...).
// The API's GlobalExceptionHandler turns these into HTTP 400 responses.
public sealed class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">Human-readable reason why the business rule failed.</param>
    public DomainException(string message) : base(message)
    {
        // No extra work: just pass the message to the base Exception class.
    }
}
