using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using LibraryManagementSystem.Domain.Entities;
using LibraryManagementSystem.Domain.Interfaces;

namespace LibraryManagementSystem.Application.Services;

// Member use cases, including activate/deactivate (controls borrowing rights).
public class MemberService : IMemberService
{
    private readonly IRepository<Member> _members;

    public MemberService(IRepository<Member> members)
    {
        _members = members;
    }

    public async Task<IReadOnlyList<MemberDto>> GetAllAsync()
    {
        var list = await _members.ListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<MemberDto> GetByIdAsync(Guid id)
        => ToDto(await GetOrThrow(id));

    public async Task<MemberDto> CreateAsync(CreateMemberRequest request)
    {
        var member = new Member(request.FirstName, request.LastName, request.Email, request.Phone);
        await _members.AddAsync(member);
        await _members.SaveChangesAsync();
        return ToDto(member);
    }

    public async Task<MemberDto> UpdateAsync(Guid id, UpdateMemberRequest request)
    {
        var member = await GetOrThrow(id);
        member.Update(request.FirstName, request.LastName, request.Phone);
        _members.Update(member);
        await _members.SaveChangesAsync();
        return ToDto(member);
    }

    public async Task DeactivateAsync(Guid id)
    {
        var member = await GetOrThrow(id);
        member.Deactivate();
        _members.Update(member);
        await _members.SaveChangesAsync();
    }

    public async Task ReactivateAsync(Guid id)
    {
        var member = await GetOrThrow(id);
        member.Reactivate();
        _members.Update(member);
        await _members.SaveChangesAsync();
    }

    private async Task<Member> GetOrThrow(Guid id)
        => await _members.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Member {id} not found.");

    private static MemberDto ToDto(Member m)
        => new(m.Id, m.FirstName, m.LastName, m.FullName, m.Email,
            m.Phone, m.MembershipDate, m.IsActive);
}
