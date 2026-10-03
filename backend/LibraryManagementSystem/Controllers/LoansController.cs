using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.API.Controllers;

// Borrow/return/renew endpoints. Business outcomes surface as HTTP codes:
// success -> 200/201, unknown id -> 404, rule broken -> 400 (see GlobalExceptionHandler).
// Reads: both roles. Borrow/return/renew mutate state -> Librarian only.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoansController : ControllerBase
{
    private readonly ILoanService _service;

    public LoansController(ILoanService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<IReadOnlyList<LoanDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<LoanDto>> GetById(Guid id)
        => Ok(await _service.GetByIdAsync(id));

    [HttpPost("borrow")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<LoanDto>> Borrow(BorrowRequest request)
    {
        var dto = await _service.BorrowAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPost("{id:guid}/return")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<LoanDto>> Return(Guid id)
        => Ok(await _service.ReturnAsync(id));

    [HttpPost("{id:guid}/renew")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<LoanDto>> Renew(Guid id)
        => Ok(await _service.RenewAsync(id));
}
