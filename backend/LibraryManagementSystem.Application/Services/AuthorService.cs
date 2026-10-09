using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using LibraryManagementSystem.Domain.Entities;
using LibraryManagementSystem.Domain.Interfaces;

namespace LibraryManagementSystem.Application.Services;

// -----------------------------------------------------------------------------
// File header:
// AuthorService implements author use cases: list/get/create/update/delete.
// Pattern for every simple service: load via repo -> call domain behavior ->
// save -> map to DTO. Missing rows throw KeyNotFoundException (API -> 404).
// For junior devs: this is the "how", IAuthorService is the "what".
// -----------------------------------------------------------------------------
// Author use cases. Pattern for every simple service:
// load via repo -> call domain behavior -> save -> map to DTO.
// Missing rows throw KeyNotFoundException, which the API turns into HTTP 404.
/// <summary>
/// Author use cases backed by the generic repository.
/// </summary>
public class AuthorService : IAuthorService
{
    // Repository for Author entities (handles DB access).
    private readonly IRepository<Author> _authors;

    /// <summary>
    /// Initializes the service with its repository (injected by DI).
    /// </summary>
    /// <param name="authors">Author repository.</param>
    public AuthorService(IRepository<Author> authors)
    {
        _authors = authors;
    }

    /// <summary>Gets all authors as DTOs.</summary>
    /// <returns>All authors mapped to DTOs.</returns>
    public async Task<IReadOnlyList<AuthorDto>> GetAllAsync()
    {
        // Load all rows, then map each entity to its read model.
        var list = await _authors.ListAsync();
        return list.Select(ToDto).ToList();
    }

    /// <summary>Gets one author or throws 404.</summary>
    /// <param name="id">Author ID.</param>
    /// <returns>The matching author DTO.</returns>
    public async Task<AuthorDto> GetByIdAsync(Guid id)
        => ToDto(await GetOrThrow(id));

    /// <summary>Creates a new author.</summary>
    /// <param name="request">Name, bio, birth date.</param>
    /// <returns>The created author DTO.</returns>
    public async Task<AuthorDto> CreateAsync(CreateAuthorRequest request)
    {
        // Constructor validates (blank name -> DomainException -> HTTP 400).
        var author = new Author(request.Name, request.Biography, request.DateOfBirth);
        // Stage the new entity, then persist it.
        await _authors.AddAsync(author);
        await _authors.SaveChangesAsync();
        // Map the saved entity (now with Id) to DTO.
        return ToDto(author);
    }

    /// <summary>Updates name/bio of an existing author.</summary>
    /// <param name="id">Author ID.</param>
    /// <param name="request">New name and bio.</param>
    /// <returns>The updated author DTO.</returns>
    public async Task<AuthorDto> UpdateAsync(Guid id, UpdateAuthorRequest request)
    {
        // Load or 404, then apply domain update (validates name).
        var author = await GetOrThrow(id);
        author.Update(request.Name, request.Biography);
        // Mark dirty and save.
        _authors.Update(author);
        await _authors.SaveChangesAsync();
        return ToDto(author);
    }

    /// <summary>Deletes an author.</summary>
    /// <param name="id">Author ID.</param>
    public async Task DeleteAsync(Guid id)
    {
        // Load or 404, then remove and save.
        var author = await GetOrThrow(id);
        _authors.Remove(author);
        await _authors.SaveChangesAsync();
    }

    /// <summary>Loads an author or throws 404.</summary>
    /// <param name="id">Author ID.</param>
    /// <returns>The author entity.</returns>
    private async Task<Author> GetOrThrow(Guid id)
        => await _authors.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Author {id} not found.");

    /// <summary>Maps an Author entity to its read DTO.</summary>
    /// <param name="a">Domain entity.</param>
    /// <returns>DTO for API responses.</returns>
    private static AuthorDto ToDto(Author a)
        => new(a.Id, a.Name, a.Biography, a.DateOfBirth);
}
