// -----------------------------------------------------------------------------
// File header:
// CategoryDtos defines category (genre/section) shapes: CategoryDto for reads,
// Create/Update requests for writes. Names must be unique (enforced in service).
// For junior devs: categories group books, e.g. "Fiction", "Science".
// -----------------------------------------------------------------------------
namespace LibraryManagementSystem.Application.DTOs;

/// <summary>
/// Category data returned to clients.
/// </summary>
/// <param name="Id">Unique category identifier.</param>
/// <param name="Name">Unique category name.</param>
/// <param name="Description">Optional description of the category.</param>
public record CategoryDto(Guid Id, string Name, string? Description);

/// <summary>
/// Payload for creating a new category.
/// </summary>
/// <param name="Name">Required unique name.</param>
/// <param name="Description">Optional description.</param>
public record CreateCategoryRequest(string Name, string? Description);
/// <summary>
/// Payload for updating an existing category.
/// </summary>
/// <param name="Name">Updated unique name.</param>
/// <param name="Description">Updated description.</param>
public record UpdateCategoryRequest(string Name, string? Description);
