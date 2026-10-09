// -----------------------------------------------------------------------------
// File header:
// MemberDtos defines library member shapes: MemberDto for reads (includes
// FullName + IsActive), Create/Update requests for writes. Email must be
// unique; IsActive controls borrowing rights.
// For junior devs: FullName is computed in the domain, not typed by users.
// -----------------------------------------------------------------------------
namespace LibraryManagementSystem.Application.DTOs;

/// <summary>
/// Library member data returned to clients.
/// </summary>
/// <param name="Id">Unique member identifier.</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="FullName">Computed "FirstName LastName".</param>
/// <param name="Email">Unique email address.</param>
/// <param name="Phone">Optional phone number.</param>
/// <param name="MembershipDate">UTC date the member joined.</param>
/// <param name="IsActive">False = deactivated, cannot borrow.</param>
public record MemberDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? Phone,
    DateTime MembershipDate,
    bool IsActive);

/// <summary>
/// Payload for registering a new member.
/// </summary>
/// <param name="FirstName">Required first name.</param>
/// <param name="LastName">Required last name.</param>
/// <param name="Email">Required unique email.</param>
/// <param name="Phone">Optional phone.</param>
public record CreateMemberRequest(string FirstName, string LastName, string Email, string? Phone);
/// <summary>
/// Payload for updating member details (email is immutable here).
/// </summary>
/// <param name="FirstName">Updated first name.</param>
/// <param name="LastName">Updated last name.</param>
/// <param name="Phone">Updated phone.</param>
public record UpdateMemberRequest(string FirstName, string LastName, string? Phone);
