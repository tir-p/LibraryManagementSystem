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
public class AuthService : IAuthService
{
    private readonly List<StaffUser> _users = new();
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiresMinutes;

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

    public Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email?.Trim() ?? "";
        var user = _users.FirstOrDefault(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        if (user is null || !VerifyPassword(request.Password ?? "", user))
            throw new DomainException("Invalid email or password.");

        var expiresAt = DateTime.UtcNow.AddMinutes(_expiresMinutes);
        var token = CreateToken(user, expiresAt);
        return Task.FromResult(new LoginResponse(token, user.Email, user.Role, expiresAt));
    }

    private void AddUser(string email, string password, string role)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        _users.Add(new StaffUser(email.Trim(), role, salt, HashPassword(password, salt)));
    }

    private static byte[] HashPassword(string password, byte[] salt)
        => Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, 100_000,
            HashAlgorithmName.SHA256, 32);

    private static bool VerifyPassword(string password, StaffUser user)
        => CryptographicOperations.FixedTimeEquals(
            HashPassword(password, user.Salt), user.PasswordHash);

    private string CreateToken(StaffUser user, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
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

    private sealed record StaffUser(string Email, string Role, byte[] Salt, byte[] PasswordHash);
}
