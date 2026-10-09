// ---------------------------------------------------------------------------
// File: MembersController.cs
// Purpose: API layer for member CRUD + activate/deactivate — maps HTTP to IMemberService.
//   No business logic here; errors become 404/400 via GlobalExceptionHandler.
// ---------------------------------------------------------------------------
using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.API.Controllers;

// Members plus activate/deactivate (controls whether they may borrow).
// Reads: both roles. All writes: Librarian only.
[ApiController]
[Route("api/[controller]")]
[Authorize]
/// <summary>
/// Member endpoints + activate/deactivate. Base route: <c>api/members</c>.
/// Auth: controller requires JWT; reads allow Librarian+Assistant, writes require Librarian.
/// DI: <c>IMemberService</c> injected via constructor; all work is delegated to it.
/// </summary>
public class MembersController : ControllerBase
{
    private readonly IMemberService _service;

    /// <summary>
    /// Creates the controller. DI container supplies the member service (see Program.cs).
    /// </summary>
    public MembersController(IMemberService service)
    {
        _service = service;
    }

    /// <summary>
    /// List all members. Route: <c>GET api/members</c>. Auth: JWT, Librarian or Assistant.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<IReadOnlyList<MemberDto>>> GetAll()
        // Read-through to service; 200 OK with list.
        => Ok(await _service.GetAllAsync());

    /// <summary>
    /// Get one member. Route: <c>GET api/members/{id}</c>. Auth: JWT, Librarian or Assistant.
    /// Missing id -&gt; 404 via GlobalExceptionHandler.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<MemberDto>> GetById(Guid id)
        // :guid constraint rejects non-GUIDs before reaching the service.
        => Ok(await _service.GetByIdAsync(id));

    /// <summary>
    /// Create a member. Route: <c>POST api/members</c>. Auth: JWT, Librarian only.
    /// Returns 201 Created with Location header to GET by id.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<MemberDto>> Create(CreateMemberRequest request)
    {
        // Delegate creation to service (validates + saves).
        var dto = await _service.CreateAsync(request);
        // 201 Created + Location header pointing to GET api/members/{id}.
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Update a member. Route: <c>PUT api/members/{id}</c>. Auth: JWT, Librarian only.
    /// Missing id -&gt; 404; rule failure -&gt; 400.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<MemberDto>> Update(Guid id, UpdateMemberRequest request)
        // Pass id (from URL) + patch (from body) to service; 200 OK with updated DTO.
        => Ok(await _service.UpdateAsync(id, request));

    /// <summary>
    /// Deactivate a member (blocks borrowing). Route: <c>POST api/members/{id}/deactivate</c>.
    /// Auth: JWT, Librarian only. Returns 204 No Content.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        // Delegate to service, then 204 (no body needed).
        await _service.DeactivateAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Reactivate a member (allows borrowing again). Route: <c>POST api/members/{id}/reactivate</c>.
    /// Auth: JWT, Librarian only. Returns 204 No Content.
    /// </summary>
    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        // Delegate to service, then 204 (no body needed).
        await _service.ReactivateAsync(id);
        return NoContent();
    }
}
