// ---------------------------------------------------------------------------
// File: CategoriesController.cs
// Purpose: API layer for category CRUD — maps HTTP routes to ICategoryService.
//   No business logic here; errors become 404/400 via GlobalExceptionHandler.
// ---------------------------------------------------------------------------
using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.API.Controllers;

// Same thin shape as AuthorsController. Reads: both roles. Writes: Librarian only.
[ApiController]
[Route("api/[controller]")]
[Authorize]
/// <summary>
/// Category endpoints. Base route: <c>api/categories</c>.
/// Auth: controller requires JWT; reads allow Librarian+Assistant, writes require Librarian.
/// DI: <c>ICategoryService</c> injected via constructor; all work is delegated to it.
/// </summary>
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _service;

    /// <summary>
    /// Creates the controller. DI container supplies the category service (see Program.cs).
    /// </summary>
    public CategoriesController(ICategoryService service)
    {
        _service = service;
    }

    /// <summary>
    /// List all categories. Route: <c>GET api/categories</c>. Auth: JWT, Librarian or Assistant.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll()
        // Read-through to service; 200 OK with list.
        => Ok(await _service.GetAllAsync());

    /// <summary>
    /// Get one category. Route: <c>GET api/categories/{id}</c>. Auth: JWT, Librarian or Assistant.
    /// Missing id -&gt; 404 via GlobalExceptionHandler.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id)
        // :guid constraint rejects non-GUIDs before reaching the service.
        => Ok(await _service.GetByIdAsync(id));

    /// <summary>
    /// Create a category. Route: <c>POST api/categories</c>. Auth: JWT, Librarian only.
    /// Returns 201 Created with Location header to GET by id.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryRequest request)
    {
        // Delegate creation to service (validates + saves).
        var dto = await _service.CreateAsync(request);
        // 201 Created + Location header pointing to GET api/categories/{id}.
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Update a category. Route: <c>PUT api/categories/{id}</c>. Auth: JWT, Librarian only.
    /// Missing id -&gt; 404; rule failure -&gt; 400.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, UpdateCategoryRequest request)
        // Pass id (from URL) + patch (from body) to service; 200 OK with updated DTO.
        => Ok(await _service.UpdateAsync(id, request));

    /// <summary>
    /// Delete a category. Route: <c>DELETE api/categories/{id}</c>. Auth: JWT, Librarian only.
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
