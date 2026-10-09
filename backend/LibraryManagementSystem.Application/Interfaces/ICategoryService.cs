// -----------------------------------------------------------------------------
// File header:
// ICategoryService is the use-case contract for categories (genres/sections).
// Same CRUD shape as authors, with unique-name validation in the service.
// Controllers depend on this interface, not the concrete class.
// -----------------------------------------------------------------------------
using LibraryManagementSystem.Application.DTOs;

namespace LibraryManagementSystem.Application.Interfaces;

/// <summary>
/// Category use cases (CRUD).
/// </summary>
public interface ICategoryService
{
    /// <summary>Gets all categories.</summary>
    /// <returns>Read-only list of categories.</returns>
    Task<IReadOnlyList<CategoryDto>> GetAllAsync();
    /// <summary>Gets one category by ID.</summary>
    /// <param name="id">Category ID.</param>
    /// <returns>The matching category.</returns>
    Task<CategoryDto> GetByIdAsync(Guid id);
    /// <summary>Creates a new category (name must be unique).</summary>
    /// <param name="request">Name + description.</param>
    /// <returns>The created category.</returns>
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
    /// <summary>Updates an existing category (name must stay unique).</summary>
    /// <param name="id">Category ID.</param>
    /// <param name="request">New name + description.</param>
    /// <returns>The updated category.</returns>
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request);
    /// <summary>Deletes a category.</summary>
    /// <param name="id">Category ID.</param>
    Task DeleteAsync(Guid id);
}
