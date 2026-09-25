using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using LibraryManagementSystem.Domain.Entities;
using LibraryManagementSystem.Domain.Interfaces;

namespace LibraryManagementSystem.Application.Services;

// Author use cases. Pattern for every simple service:
// load via repo -> call domain behavior -> save -> map to DTO.
// Missing rows throw KeyNotFoundException, which the API turns into HTTP 404.
public class AuthorService : IAuthorService
{
    private readonly IRepository<Author> _authors;

    public AuthorService(IRepository<Author> authors)
    {
        _authors = authors;
    }

    public async Task<IReadOnlyList<AuthorDto>> GetAllAsync()
    {
        var list = await _authors.ListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<AuthorDto> GetByIdAsync(Guid id)
        => ToDto(await GetOrThrow(id));

    public async Task<AuthorDto> CreateAsync(CreateAuthorRequest request)
    {
        // Constructor validates (blank name -> DomainException -> HTTP 400).
        var author = new Author(request.Name, request.Biography, request.DateOfBirth);
        await _authors.AddAsync(author);
        await _authors.SaveChangesAsync();
        return ToDto(author);
    }

    public async Task<AuthorDto> UpdateAsync(Guid id, UpdateAuthorRequest request)
    {
        var author = await GetOrThrow(id);
        author.Update(request.Name, request.Biography);
        _authors.Update(author);
        await _authors.SaveChangesAsync();
        return ToDto(author);
    }

    public async Task DeleteAsync(Guid id)
    {
        var author = await GetOrThrow(id);
        _authors.Remove(author);
        await _authors.SaveChangesAsync();
    }

    private async Task<Author> GetOrThrow(Guid id)
        => await _authors.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Author {id} not found.");

    private static AuthorDto ToDto(Author a)
        => new(a.Id, a.Name, a.Biography, a.DateOfBirth);
}
