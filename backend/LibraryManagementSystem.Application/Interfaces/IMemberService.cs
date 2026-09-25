using LibraryManagementSystem.Application.DTOs;

namespace LibraryManagementSystem.Application.Interfaces;

public interface IMemberService
{
    Task<IReadOnlyList<MemberDto>> GetAllAsync();
    Task<MemberDto> GetByIdAsync(Guid id);
    Task<MemberDto> CreateAsync(CreateMemberRequest request);
    Task<MemberDto> UpdateAsync(Guid id, UpdateMemberRequest request);
    Task DeactivateAsync(Guid id);
    Task ReactivateAsync(Guid id);
}
