// ---------------------------------------------------------------------------
// File: AuthorsController.cs
// Purpose: API layer for author CRUD — maps HTTP routes to IAuthorService.
//   No business logic here; errors become 404/400 via GlobalExceptionHandler.
// ---------------------------------------------------------------------------
using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.API.Controllers;

// Thin HTTP layer: maps routes to service calls only.
// Errors are handled globally (GlobalExceptionHandler): missing -> 404, rule broken -> 400.
// Reads: Librarian + Assistant. Writes: Librarian only.
[ApiController]
[Route("api/[controller]")]
[Authorize]
/// <summary>
/// Author endpoints. Base route: <c>api/authors</c>.
/// Auth: controller requires JWT; reads allow Librarian+Assistant, writes require Librarian.
/// DI: <c>IAuthorService</c> injected via constructor; all work is delegated to it.
/// </summary>
public class AuthorsController : ControllerBase
{
    private readonly IAuthorService _service;

    /// <summary>
    /// Creates the controller. DI container supplies the author service (see Program.cs).
    /// </summary>
    public AuthorsController(IAuthorService service)
    {
        _service = service;
    }

    /// <summary>
    /// List all authors. Route: <c>GET api/authors</c>. Auth: JWT, Librarian or Assistant.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<IReadOnlyList<AuthorDto>>> GetAll()
        // Read-through to service; 200 OK with list (empty list if none).
        => Ok(await _service.GetAllAsync());

    /// <summary>
    /// Get one author. Route: <c>GET api/authors/{id}</c>. Auth: JWT, Librarian or Assistant.
    /// Missing id -&gt; 404 via GlobalExceptionHandler.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<AuthorDto>> GetById(Guid id)
        // :guid constraint rejects non-GUIDs before reaching the service.
        => Ok(await _service.GetByIdAsync(id));

    /// <summary>
    /// Create an author. Route: <c>POST api/authors</c>. Auth: JWT, Librarian only.
    /// Returns 201 Created with Location header to GET by id.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<AuthorDto>> Create(CreateAuthorRequest request)
    {
        // Delegate creation to service (validates + saves).
        var dto = await _service.CreateAsync(request);
        // 201 Created + Location header pointing to GET api/authors/{id}.
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Update an author. Route: <c>PUT api/authors/{id}</c>. Auth: JWT, Librarian only.
    /// Missing id -&gt; 404; rule failure -&gt; 400.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<AuthorDto>> Update(Guid id, UpdateAuthorRequest request)
        // Pass id (from URL) + patch (from body) to service; 200 OK with updated DTO.
        => Ok(await _service.UpdateAsync(id, request));

    /// <summary>
    /// Delete an author. Route: <c>DELETE api/authors/{id}</c>. Auth: JWT, Librarian only.
    /// Returns 204 No Content; missing id -&gt; 404.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<IActionResult> Delete(Guid id)
    {
        // Delegate delete to service, then 204 (no body needed).
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
