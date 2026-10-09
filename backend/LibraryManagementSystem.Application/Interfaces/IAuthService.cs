// -----------------------------------------------------------------------------
// File header:
// IAuthService validates staff credentials and mints JWTs. Users are currently
// in-memory (see AuthService) so no DB migration is needed; to persist users
// later, back this interface with a Users table.
// For junior devs: controllers call LoginAsync, then return the JWT to the client.
// -----------------------------------------------------------------------------
using LibraryManagementSystem.Application.DTOs;

namespace LibraryManagementSystem.Application.Interfaces;

// Validates demo staff credentials and mints JWTs.
// Users are in-memory (see AuthService): no DB migration needed.
// To move to persistent users later, back this interface with a Users table.
/// <summary>
/// Authentication use cases (login only for now).
/// </summary>
public interface IAuthService
{
    /// <summary>Validates credentials and issues a JWT.</summary>
    /// <param name="request">Email + password.</param>
    /// <returns>Token, email, role, and expiry.</returns>
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
