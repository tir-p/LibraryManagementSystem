// ---------------------------------------------------------------------------
// File: AuthController.cs
// Purpose: API layer for authentication — public login (issues JWT) and
//   authenticated "who am I" lookup. Thin controller: delegates to IAuthService.
//   All other API controllers require a JWT (see Program.cs).
// ---------------------------------------------------------------------------
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.API.Controllers;

// Public login + authenticated "who am I". All other controllers require a
// JWT (see Program.cs); write endpoints additionally require the Librarian role.
[ApiController]
[Route("api/[controller]")]
/// <summary>
/// Handles authentication endpoints. Base route: <c>api/auth</c> (from [Route]).
/// DI: <see cref="IAuthService"/> is injected via constructor.
/// </summary>
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    /// <summary>
    /// Creates the controller. DI container supplies the auth service (see Program.cs).
    /// </summary>
    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Login. Route: <c>POST api/auth/login</c>. Auth: anonymous (no JWT needed).
    /// On success returns 200 + JWT; bad credentials bubble as 400 via GlobalExceptionHandler.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        // Delegate to AuthService; Ok() wraps the LoginResponse as HTTP 200.
        => Ok(await _auth.LoginAsync(request));

    /// <summary>
    /// Current user. Route: <c>GET api/auth/me</c>. Auth: JWT required (any role).
    /// Reads email/role claims put in the JWT by AuthService.CreateToken.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<CurrentUserDto> Me()
    {
        // User is built from the validated JWT (see JWT setup in Program.cs).
        // Try Name claim first, fall back to Email claim for compatibility.
        var email = User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Email);
        // Role claim decides Librarian vs Assistant UI on the frontend.
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        // 200 OK with email + role; empty strings if claims missing.
        return Ok(new CurrentUserDto(email ?? "", role));
    }
}
