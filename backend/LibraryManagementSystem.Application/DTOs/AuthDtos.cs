// -----------------------------------------------------------------------------
// File header:
// AuthDtos defines the data shapes for login: what the client sends,
// what the API returns (JWT + user info), and the role names used by
// [Authorize(Roles = ...)] throughout the controllers.
// For junior devs: DTO = Data Transfer Object, a simple container with no logic.
// -----------------------------------------------------------------------------
namespace LibraryManagementSystem.Application.DTOs;

// Role names used in JWT claims + [Authorize(Roles = ...)].
// Librarian = full write access. Assistant = read-only (GETs only).
// ReadOnly is the combined value for read endpoints (const so it stays
// usable inside attribute arguments).
/// <summary>
/// Well-known role names for staff users.
/// </summary>
public static class UserRoles
{
    /// <summary>Full write access (can create/update/delete).</summary>
    public const string Librarian = "Librarian";
    /// <summary>Read-only access (GET endpoints only).</summary>
    public const string Assistant = "Assistant";
    /// <summary>Combined value for read endpoints that both roles can call.</summary>
    public const string ReadOnly = Librarian + "," + Assistant;
}

/// <summary>
/// Login credentials sent by the client.
/// </summary>
/// <param name="Email">Staff email address.</param>
/// <param name="Password">Plain-text password (checked against the stored hash).</param>
public record LoginRequest(string Email, string Password);

/// <summary>
/// Successful login result, including the JWT the client must send back.
/// </summary>
/// <param name="Token">Signed JWT bearer token.</param>
/// <param name="Email">Email of the logged-in user.</param>
/// <param name="Role">Role claim embedded in the token (Librarian/Assistant).</param>
/// <param name="ExpiresAt">UTC time when the token stops being valid.</param>
public record LoginResponse(
    string Token,
    string Email,
    string Role,
    DateTime ExpiresAt);

/// <summary>
/// Minimal info about the currently logged-in user (from the JWT claims).
/// </summary>
/// <param name="Email">Email taken from the token's email claim.</param>
/// <param name="Role">Role taken from the token's role claim.</param>
public record CurrentUserDto(string Email, string Role);
