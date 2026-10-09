// ---------------------------------------------------------------------------
// File: BooksController.cs
// Purpose: API layer for books + nested book-copy routes — maps HTTP to IBookService.
//   No business logic here; errors become 404/400 via GlobalExceptionHandler.
// ---------------------------------------------------------------------------
using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.API.Controllers;

// Books plus nested copy routes: POST /api/books/{id}/copies adds a physical copy.
// Reads: Librarian + Assistant. Writes: Librarian only (Assistant is read-only).
[ApiController]
[Route("api/[controller]")]
[Authorize]
/// <summary>
/// Book + book-copy endpoints. Base route: <c>api/books</c>.
/// Auth: controller requires JWT; reads allow Librarian+Assistant, writes require Librarian.
/// DI: <c>IBookService</c> injected via constructor; all work is delegated to it.
/// </summary>
public class BooksController : ControllerBase
{
    private readonly IBookService _service;

    /// <summary>
    /// Creates the controller. DI container supplies the book service (see Program.cs).
    /// </summary>
    public BooksController(IBookService service)
    {
        _service = service;
    }

    /// <summary>
    /// List all books. Route: <c>GET api/books</c>. Auth: JWT, Librarian or Assistant.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<IReadOnlyList<BookDto>>> GetAll()
        // Read-through to service; 200 OK with list.
        => Ok(await _service.GetAllAsync());

    /// <summary>
    /// Book availability summary. Route: <c>GET api/books/availability</c>. Auth: JWT, Librarian or Assistant.
    /// Must stay before <c>{id:guid}</c> route so "availability" is not treated as an id.
    /// </summary>
    [HttpGet("availability")]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<IReadOnlyList<BookAvailabilityDto>>> GetAvailability()
        // Delegates to service; 200 OK with per-book available/total counts.
        => Ok(await _service.GetAvailabilityAsync());

    /// <summary>
    /// Get one book. Route: <c>GET api/books/{id}</c>. Auth: JWT, Librarian or Assistant.
    /// Missing id -&gt; 404 via GlobalExceptionHandler.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<BookDto>> GetById(Guid id)
        // :guid constraint rejects non-GUIDs before reaching the service.
        => Ok(await _service.GetByIdAsync(id));

    /// <summary>
    /// Create a book. Route: <c>POST api/books</c>. Auth: JWT, Librarian only.
    /// Returns 201 Created with Location header to GET by id.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<BookDto>> Create(CreateBookRequest request)
    {
        // Delegate creation to service (validates author/category + saves).
        var dto = await _service.CreateAsync(request);
        // 201 Created + Location header pointing to GET api/books/{id}.
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Update a book. Route: <c>PUT api/books/{id}</c>. Auth: JWT, Librarian only.
    /// Missing id -&gt; 404; rule failure -&gt; 400.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<BookDto>> Update(Guid id, UpdateBookRequest request)
        // Pass id (from URL) + patch (from body) to service; 200 OK with updated DTO.
        => Ok(await _service.UpdateAsync(id, request));

    /// <summary>
    /// Delete a book. Route: <c>DELETE api/books/{id}</c>. Auth: JWT, Librarian only.
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

    /// <summary>
    /// List copies of one book. Route: <c>GET api/books/{id}/copies</c>. Auth: JWT, Librarian or Assistant.
    /// </summary>
    [HttpGet("{id:guid}/copies")]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<IReadOnlyList<BookCopyDto>>> GetCopies(Guid id)
        // Nested route: book id comes from URL, copies from service.
        => Ok(await _service.GetCopiesAsync(id));

    /// <summary>
    /// Add a physical copy. Route: <c>POST api/books/{id}/copies</c>. Auth: JWT, Librarian only.
    /// Returns 201 Created with Location header to the copies list.
    /// </summary>
    [HttpPost("{id:guid}/copies")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<BookCopyDto>> AddCopy(Guid id, CreateBookCopyRequest request)
    {
        // Book id from URL + copy details from body go to the service.
        var dto = await _service.AddCopyAsync(id, request);
        // 201 Created; Location points to GET api/books/{id}/copies.
        return CreatedAtAction(nameof(GetCopies), new { id }, dto);
    }
}
