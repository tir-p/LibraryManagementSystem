using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using LibraryManagementSystem.Domain.Entities;
using LibraryManagementSystem.Domain.Exceptions;
using LibraryManagementSystem.Domain.Interfaces;

namespace LibraryManagementSystem.Application.Services;

// -----------------------------------------------------------------------------
// File header:
// CategoryService implements category CRUD with unique-name validation.
// Same shape as AuthorService, plus duplicate checks on create AND update
// (update excludes itself). DomainException -> HTTP 400, missing -> 404.
// ----------------------------------------------------------------------------
// Same CRUD shape as AuthorService.
/// <summary>
/// Category use cases backed by the generic repository.
/// </summary>
public class CategoryService : ICategoryService
{
    // Repository for Category entities.
    private readonly IRepository<Category> _categories;

    /// <summary>
    /// Initializes the service (injected by DI).
    /// </summary>
    /// <param name="categories">Category repository.</param>
    public CategoryService(IRepository<Category> categories)
    {
        _categories = categories;
    }

    /// <summary>Gets all categories.</summary>
    /// <returns>All categories as DTOs.</returns>
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync()
    {
        // Load all rows and map to read models.
        var list = await _categories.ListAsync();
        return list.Select(ToDto).ToList();
    }

    /// <summary>Gets one category or throws 404.</summary>
    /// <param name="id">Category ID.</param>
    /// <returns>The matching category DTO.</returns>
    public async Task<CategoryDto> GetByIdAsync(Guid id)
        => ToDto(await GetOrThrow(id));

    /// <summary>Creates a new category with unique name.</summary>
    /// <param name="request">Name + description.</param>
    /// <returns>The created category DTO.</returns>
    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
    {
        // Enforce unique name (trim + case-insensitive) for a friendly 400.
        var existing = await _categories.ListAsync();
        if (existing.Any(c => c.Name.Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("A category with this name already exists.");

        // Constructor validates (blank name -> 400); then persist.
        var category = new Category(request.Name, request.Description);
        await _categories.AddAsync(category);
        await _categories.SaveChangesAsync();
        return ToDto(category);
    }

    /// <summary>Updates a category, keeping the name unique.</summary>
    /// <param name="id">Category ID.</param>
    /// <param name="request">New name + description.</param>
    /// <returns>The updated category DTO.</returns>
    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request)
    {
        // Load or 404.
        var category = await GetOrThrow(id);
        // Check duplicates excluding self (so unchanged name passes).
        var existing = await _categories.ListAsync();
        if (existing.Any(c => c.Id != id && c.Name.Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("A category with this name already exists.");

        // Apply domain update, then save.
        category.Update(request.Name, request.Description);
        _categories.Update(category);
        await _categories.SaveChangesAsync();
        return ToDto(category);
    }

    /// <summary>Deletes a category.</summary>
    /// <param name="id">Category ID.</param>
    public async Task DeleteAsync(Guid id)
    {
        // Load or 404, then remove and save.
        var category = await GetOrThrow(id);
        _categories.Remove(category);
        await _categories.SaveChangesAsync();
    }

    /// <summary>Loads a category or throws 404.</summary>
    /// <param name="id">Category ID.</param>
    /// <returns>The category entity.</returns>
    private async Task<Category> GetOrThrow(Guid id)
        => await _categories.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Category {id} not found.");

    /// <summary>Maps a Category to its DTO.</summary>
    /// <param name="c">Domain entity.</param>
    /// <returns>DTO for API responses.</returns>
    private static CategoryDto ToDto(Category c)
        => new(c.Id, c.Name, c.Description);
}
