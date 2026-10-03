using LibraryManagementSystem.Application.DTOs;

namespace LibraryManagementSystem.Application.Interfaces;

// Validates demo staff credentials and mints JWTs.
// Users are in-memory (see AuthService): no DB migration needed.
// To move to persistent users later, back this interface with a Users table.
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
