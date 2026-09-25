using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.API.Controllers;

// Borrow/return/renew endpoints. Business outcomes surface as HTTP codes:
// success -> 200/201, unknown id -> 404, rule broken -> 400 (see GlobalExceptionHandler).
[ApiController]
[Route("api/[controller]")]
public class LoansController : ControllerBase
{
    private readonly ILoanService _service;

    public LoansController(ILoanService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LoanDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LoanDto>> GetById(Guid id)
        => Ok(await _service.GetByIdAsync(id));

    [HttpPost("borrow")]
    public async Task<ActionResult<LoanDto>> Borrow(BorrowRequest request)
    {
        var dto = await _service.BorrowAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPost("{id:guid}/return")]
    public async Task<ActionResult<LoanDto>> Return(Guid id)
        => Ok(await _service.ReturnAsync(id));

    [HttpPost("{id:guid}/renew")]
    public async Task<ActionResult<LoanDto>> Renew(Guid id)
        => Ok(await _service.RenewAsync(id));
}
