using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using LibraryManagementSystem.Domain.Entities;
using LibraryManagementSystem.Domain.Exceptions;
using LibraryManagementSystem.Domain.Interfaces;

namespace LibraryManagementSystem.Application.Services;

// -----------------------------------------------------------------------------
// File header:
// MemberService implements member use cases: list/get/register/update plus
// deactivate/reactivate (which gates borrowing via Member.CanBorrow).
// Email uniqueness -> friendly 400; missing rows -> 404; domain methods own
// the state transitions.
// -----------------------------------------------------------------------------
// Member use cases, including activate/deactivate (controls borrowing rights).
/// <summary>
/// Member use cases backed by the generic repository.
/// </summary>
public class MemberService : IMemberService
{
    // Repository for Member entities.
    private readonly IRepository<Member> _members;

    /// <summary>
    /// Initializes the service (injected by DI).
    /// </summary>
    /// <param name="members">Member repository.</param>
    public MemberService(IRepository<Member> members)
    {
        _members = members;
    }

    /// <summary>Gets all members.</summary>
    /// <returns>All members as DTOs.</returns>
    public async Task<IReadOnlyList<MemberDto>> GetAllAsync()
    {
        // Load all rows and map to read models.
        var list = await _members.ListAsync();
        return list.Select(ToDto).ToList();
    }

    /// <summary>Gets one member or throws 404.</summary>
    /// <param name="id">Member ID.</param>
    /// <returns>The matching member DTO.</returns>
    public async Task<MemberDto> GetByIdAsync(Guid id)
        => ToDto(await GetOrThrow(id));

    /// <summary>Registers a new member with unique email.</summary>
    /// <param name="request">First/last name, email, phone.</param>
    /// <returns>The created member DTO.</returns>
    public async Task<MemberDto> CreateAsync(CreateMemberRequest request)
    {
        // Enforce unique email (trim + case-insensitive) for a friendly 400.
        var existing = await _members.ListAsync();
        if (existing.Any(m => m.Email.Equals(request.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("A member with this email already exists.");

        // Constructor validates (blank name, bad email -> 400); then persist.
        var member = new Member(request.FirstName, request.LastName, request.Email, request.Phone);
        await _members.AddAsync(member);
        await _members.SaveChangesAsync();
        return ToDto(member);
    }

    /// <summary>Updates member details.</summary>
    /// <param name="id">Member ID.</param>
    /// <param name="request">New names + phone (email immutable).</param>
    /// <returns>The updated member DTO.</returns>
    public async Task<MemberDto> UpdateAsync(Guid id, UpdateMemberRequest request)
    {
        // Load or 404, apply domain update (validates names), save.
        var member = await GetOrThrow(id);
        member.Update(request.FirstName, request.LastName, request.Phone);
        _members.Update(member);
        await _members.SaveChangesAsync();
        return ToDto(member);
    }

    /// <summary>Deactivates a member (blocks future borrowing).</summary>
    /// <param name="id">Member ID.</param>
    public async Task DeactivateAsync(Guid id)
    {
        // Load or 404, flip to inactive via domain, save.
        var member = await GetOrThrow(id);
        member.Deactivate();
        _members.Update(member);
        await _members.SaveChangesAsync();
    }

    /// <summary>Reactivates a member (restores borrowing rights).</summary>
    /// <param name="id">Member ID.</param>
    public async Task ReactivateAsync(Guid id)
    {
        // Load or 404, flip back to active via domain, save.
        var member = await GetOrThrow(id);
        member.Reactivate();
        _members.Update(member);
        await _members.SaveChangesAsync();
    }

    /// <summary>Loads a member or throws 404.</summary>
    /// <param name="id">Member ID.</param>
    /// <returns>The member entity.</returns>
    private async Task<Member> GetOrThrow(Guid id)
        => await _members.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Member {id} not found.");

    /// <summary>Maps a Member to its DTO.</summary>
    /// <param name="m">Domain entity.</param>
    /// <returns>DTO for API responses.</returns>
    private static MemberDto ToDto(Member m)
        => new(m.Id, m.FirstName, m.LastName, m.FullName, m.Email,
            m.Phone, m.MembershipDate, m.IsActive);
}
