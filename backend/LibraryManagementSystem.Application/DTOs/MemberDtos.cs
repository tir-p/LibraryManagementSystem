namespace LibraryManagementSystem.Application.DTOs;

public record MemberDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? Phone,
    DateTime MembershipDate,
    bool IsActive);

public record CreateMemberRequest(string FirstName, string LastName, string Email, string? Phone);
public record UpdateMemberRequest(string FirstName, string LastName, string? Phone);
