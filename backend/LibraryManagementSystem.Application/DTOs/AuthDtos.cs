namespace LibraryManagementSystem.Application.DTOs;

// Role names used in JWT claims + [Authorize(Roles = ...)].
// Librarian = full write access. Assistant = read-only (GETs only).
// ReadOnly is the combined value for read endpoints (const so it stays
// usable inside attribute arguments).
public static class UserRoles
{
    public const string Librarian = "Librarian";
    public const string Assistant = "Assistant";
    public const string ReadOnly = Librarian + "," + Assistant;
}

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string Token,
    string Email,
    string Role,
    DateTime ExpiresAt);

public record CurrentUserDto(string Email, string Role);
