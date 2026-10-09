// ---------------------------------------------------------------------------
// File: AuthService.cs (API layer)
// Purpose: Demo staff login + JWT minting — validates credentials from an
//   in-memory staff store and issues signed JWTs. Lives in API (not Application)
//   because JWT creation is infrastructure, not a business rule.
// ---------------------------------------------------------------------------
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using LibraryManagementSystem.Domain.Exceptions;
using Microsoft.IdentityModel.Tokens;

namespace LibraryManagementSystem.API.Services;

// Demo staff store: two accounts (librarian = full access, assistant = read-only).
// Passwords are PBKDF2-hashed in memory (salt + 100k SHA256 iterations) —
// plaintext only ever lives in config for this demo. Override via appsettings
// Auth:{Librarian,Assistant}{Email,Password} or environment variables.
// Lives in the API layer (not Application) because JWT minting is infrastructure.
/// <summary>
/// Demo staff authentication + JWT issuer (implements <c>IAuthService</c>).
/// Holds two in-memory users (Librarian = full access, Assistant = read-only).
/// DI: <c>IConfiguration</c> injected for Jwt + Auth settings; registered as Singleton in Program.cs.
/// </summary>
public class AuthService : IAuthService
{
    private readonly List<StaffUser> _users = new();
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiresMinutes;

    /// <summary>
    /// Builds the in-memory staff store from config and caches JWT settings.
    /// Reads Jwt:Key/Issuer/Audience/ExpiresMinutes + Auth:{Librarian,Assistant}{Email,Password}.
    /// Throws if Jwt:Key is missing — app cannot sign tokens without it.
    /// </summary>
    public AuthService(IConfiguration config)
    {
        _key = config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        _issuer = config["Jwt:Issuer"] ?? "LibraryManagementSystem";
        _audience = config["Jwt:Audience"] ?? "LibraryFrontend";
        _expiresMinutes = int.TryParse(config["Jwt:ExpiresMinutes"], out var m) ? m : 480;

        AddUser(
            config["Auth:LibrarianEmail"] ?? "librarian@library.com",
            config["Auth:LibrarianPassword"] ?? "Librarian123!",
            UserRoles.Librarian);
        AddUser(
            config["Auth:AssistantEmail"] ?? "assistant@library.com",
            config["Auth:AssistantPassword"] ?? "Assistant123!",
            UserRoles.Assistant);
    }

    /// <summary>
    /// Validates email/password and returns a signed JWT + user info.
    /// Throws <c>DomainException</c> (mapped to 400) when credentials are invalid.
    /// JWT logic: builds claims (sub/email/name/role), signs with HmacSha256, sets expiry.
    /// </summary>
    public Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // Normalize email (trim + case-insensitive lookup) — junior note: emails are case-insensitive.
        var email = request.Email?.Trim() ?? "";
        // Find staff user by email; FirstOrDefault returns null when not found.
        var user = _users.FirstOrDefault(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        // Same error for unknown email or wrong password (prevents user enumeration).
        // VerifyPassword uses constant-time compare to avoid timing attacks.
        if (user is null || !VerifyPassword(request.Password ?? "", user))
            throw new DomainException("Invalid email or password.");

        // Credentials OK: compute expiry then mint JWT (see CreateToken below).
        var expiresAt = DateTime.UtcNow.AddMinutes(_expiresMinutes);
        var token = CreateToken(user, expiresAt);
        return Task.FromResult(new LoginResponse(token, user.Email, user.Role, expiresAt));
    }

    /// <summary>
    /// Hashes a password with a fresh 16-byte salt (PBKDF2, 100k SHA256, 32-byte output).
    /// Called once at startup per demo user; salt+hash are kept in memory.
    /// </summary>
    private void AddUser(string email, string password, string role)
    {
        // Junior note: random salt means same password hashes differently per user.
        var salt = RandomNumberGenerator.GetBytes(16);
        _users.Add(new StaffUser(email.Trim(), role, salt, HashPassword(password, salt)));
    }

    /// <summary>
    /// PBKDF2 password hash helper (SHA256, 100k iterations). Deterministic for same salt.
    /// </summary>
    private static byte[] HashPassword(string password, byte[] salt)
        => Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, 100_000,
            HashAlgorithmName.SHA256, 32);

    /// <summary>
    /// Constant-time password check (prevents timing attacks). Re-hashes input and compares.
    /// </summary>
    private static bool VerifyPassword(string password, StaffUser user)
        => CryptographicOperations.FixedTimeEquals(
            HashPassword(password, user.Salt), user.PasswordHash);

    /// <summary>
    /// Mints a signed JWT with sub/email/name/role claims, issuer/audience, and expiry.
    /// Frontend sends it as <c>Authorization: Bearer &lt;token&gt;</c>; Program.cs validates it.
    /// </summary>
    private string CreateToken(StaffUser user, DateTime expiresAt)
    {
        // Symmetric key = shared secret; both sign (here) and validate (Program.cs) use it.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        // Claims = identity carried inside the token; Role drives [Authorize(Roles=...)].
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
        };
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// In-memory staff record: email identity + role + salted password hash.
    /// </summary>
    private sealed record StaffUser(string Email, string Role, byte[] Salt, byte[] PasswordHash);
}
