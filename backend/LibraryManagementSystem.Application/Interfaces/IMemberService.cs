// -----------------------------------------------------------------------------
// File header:
// IMemberService is the use-case contract for library members: CRUD plus
// deactivate/reactivate (which controls borrowing rights). Controllers depend
// on this interface for loose coupling and testing.
// -----------------------------------------------------------------------------
using LibraryManagementSystem.Application.DTOs;

namespace LibraryManagementSystem.Application.Interfaces;

/// <summary>
/// Member use cases (CRUD + activate/deactivate).
/// </summary>
public interface IMemberService
{
    /// <summary>Gets all members.</summary>
    /// <returns>Read-only list of members.</returns>
    Task<IReadOnlyList<MemberDto>> GetAllAsync();
    /// <summary>Gets one member by ID.</summary>
    /// <param name="id">Member ID.</param>
    /// <returns>The matching member.</returns>
    Task<MemberDto> GetByIdAsync(Guid id);
    /// <summary>Registers a new member (email must be unique).</summary>
    /// <param name="request">First/last name, email, phone.</param>
    /// <returns>The created member.</returns>
    Task<MemberDto> CreateAsync(CreateMemberRequest request);
    /// <summary>Updates member details.</summary>
    /// <param name="id">Member ID.</param>
    /// <param name="request">New names + phone.</param>
    /// <returns>The updated member.</returns>
    Task<MemberDto> UpdateAsync(Guid id, UpdateMemberRequest request);
    /// <summary>Deactivates a member (blocks future borrowing).</summary>
    /// <param name="id">Member ID.</param>
    Task DeactivateAsync(Guid id);
    /// <summary>Reactivates a member (restores borrowing rights).</summary>
    /// <param name="id">Member ID.</param>
    Task ReactivateAsync(Guid id);
}
