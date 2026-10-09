// -----------------------------------------------------------------------------
// File header:
// IAuthorService is the use-case contract for authors. Controllers depend on
// this interface, not the concrete class (Dependency Inversion), so the
// implementation can be swapped/mocked in tests.
// -----------------------------------------------------------------------------
using LibraryManagementSystem.Application.DTOs;

namespace LibraryManagementSystem.Application.Interfaces;

// Use-case contract for authors. Controllers depend on this,
// not on the service class (Dependency Inversion).
/// <summary>
/// Author use cases (CRUD).
/// </summary>
public interface IAuthorService
{
    /// <summary>Gets all authors.</summary>
    /// <returns>Read-only list of authors.</returns>
    Task<IReadOnlyList<AuthorDto>> GetAllAsync();
    /// <summary>Gets one author by ID.</summary>
    /// <param name="id">Author ID.</param>
    /// <returns>The matching author.</returns>
    Task<AuthorDto> GetByIdAsync(Guid id);
    /// <summary>Creates a new author.</summary>
    /// <param name="request">Name, bio, birth date.</param>
    /// <returns>The created author.</returns>
    Task<AuthorDto> CreateAsync(CreateAuthorRequest request);
    /// <summary>Updates name/bio of an existing author.</summary>
    /// <param name="id">Author ID.</param>
    /// <param name="request">New name and bio.</param>
    /// <returns>The updated author.</returns>
    Task<AuthorDto> UpdateAsync(Guid id, UpdateAuthorRequest request);
    /// <summary>Deletes an author.</summary>
    /// <param name="id">Author ID.</param>
    Task DeleteAsync(Guid id);
}
