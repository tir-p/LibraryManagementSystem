// ---------------------------------------------------------------------------
// File: LoansController.cs
// Purpose: API layer for borrowing — borrow/return/renew + loan lookups.
//   Maps HTTP routes to ILoanService; business rules live in the service layer.
// ---------------------------------------------------------------------------
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
/// <summary>
/// Loan endpoints (list, borrow, return, renew). Base route: <c>api/loans</c>.
/// Auth: controller requires JWT; reads allow Librarian+Assistant, mutations require Librarian.
/// DI: <c>ILoanService</c> injected via constructor; all work is delegated to it.
/// </summary>
public class LoansController : ControllerBase
{
    private readonly ILoanService _service;

    /// <summary>
    /// Creates the controller. DI container supplies the loan service (see Program.cs).
    /// </summary>
    public LoansController(ILoanService service)
    {
        _service = service;
    }

    /// <summary>
    /// List all loans. Route: <c>GET api/loans</c>. Auth: JWT, Librarian or Assistant.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<IReadOnlyList<LoanDto>>> GetAll()
        // Read-through to service; 200 OK with list.
        => Ok(await _service.GetAllAsync());

    /// <summary>
    /// Get one loan. Route: <c>GET api/loans/{id}</c>. Auth: JWT, Librarian or Assistant.
    /// Missing id -&gt; 404 via GlobalExceptionHandler.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = UserRoles.ReadOnly)]
    public async Task<ActionResult<LoanDto>> GetById(Guid id)
        // :guid constraint rejects non-GUIDs before reaching the service.
        => Ok(await _service.GetByIdAsync(id));

    /// <summary>
    /// Borrow a copy. Route: <c>POST api/loans/borrow</c>. Auth: JWT, Librarian only.
    /// Rule failures (no copy, inactive member) -&gt; 400; returns 201 Created on success.
    /// </summary>
    [HttpPost("borrow")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<LoanDto>> Borrow(BorrowRequest request)
    {
        // Delegate borrow rules (copy available? member active?) to service.
        var dto = await _service.BorrowAsync(request);
        // 201 Created + Location header pointing to GET api/loans/{id}.
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Return a loan. Route: <c>POST api/loans/{id}/return</c>. Auth: JWT, Librarian only.
    /// Marks copy available again; missing/ already-returned -&gt; 404/400.
    /// </summary>
    [HttpPost("{id:guid}/return")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<LoanDto>> Return(Guid id)
        // Delegate return logic to service; 200 OK with updated loan.
        => Ok(await _service.ReturnAsync(id));

    /// <summary>
    /// Renew a loan (extend due date). Route: <c>POST api/loans/{id}/renew</c>. Auth: JWT, Librarian only.
    /// Rule failures (overdue, max renewals) -&gt; 400 via GlobalExceptionHandler.
    /// </summary>
    [HttpPost("{id:guid}/renew")]
    [Authorize(Roles = UserRoles.Librarian)]
    public async Task<ActionResult<LoanDto>> Renew(Guid id)
        // Delegate renew logic to service; 200 OK with updated loan.
        => Ok(await _service.RenewAsync(id));
}
