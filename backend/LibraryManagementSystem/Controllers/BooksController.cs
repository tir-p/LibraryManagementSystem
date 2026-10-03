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
public class BooksController : ControllerBase
{
    private readonly IBookService _service;

    public BooksController(IBookService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<IReadOnlyList<BookDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("availability")]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<IReadOnlyList<BookAvailabilityDto>>> GetAvailability()
        => Ok(await _service.GetAvailabilityAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<BookDto>> GetById(Guid id)
        => Ok(await _service.GetByIdAsync(id));

    [HttpPost]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<BookDto>> Create(CreateBookRequest request)
    {
        var dto = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<BookDto>> Update(Guid id, UpdateBookRequest request)
        => Ok(await _service.UpdateAsync(id, request));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id:guid}/copies")]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<IReadOnlyList<BookCopyDto>>> GetCopies(Guid id)
        => Ok(await _service.GetCopiesAsync(id));

    [HttpPost("{id:guid}/copies")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<BookCopyDto>> AddCopy(Guid id, CreateBookCopyRequest request)
    {
        var dto = await _service.AddCopyAsync(id, request);
        return CreatedAtAction(nameof(GetCopies), new { id }, dto);
    }
}
