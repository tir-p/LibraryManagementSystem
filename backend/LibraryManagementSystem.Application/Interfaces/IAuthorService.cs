using LibraryManagementSystem.Application.DTOs;

namespace LibraryManagementSystem.Application.Interfaces;

// Use-case contract for authors. Controllers depend on this,
// not on the service class (Dependency Inversion).
public interface IAuthorService
{
    Task<IReadOnlyList<AuthorDto>> GetAllAsync();
    Task<AuthorDto> GetByIdAsync(Guid id);
    Task<AuthorDto> CreateAsync(CreateAuthorRequest request);
    Task<AuthorDto> UpdateAsync(Guid id, UpdateAuthorRequest request);
    Task DeleteAsync(Guid id);
}
