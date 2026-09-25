using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using LibraryManagementSystem.Domain.Entities;
using LibraryManagementSystem.Domain.Interfaces;

namespace LibraryManagementSystem.Application.Services;

// Same CRUD shape as AuthorService.
public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _categories;

    public CategoryService(IRepository<Category> categories)
    {
        _categories = categories;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync()
    {
        var list = await _categories.ListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<CategoryDto> GetByIdAsync(Guid id)
        => ToDto(await GetOrThrow(id));

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
    {
        var category = new Category(request.Name, request.Description);
        await _categories.AddAsync(category);
        await _categories.SaveChangesAsync();
        return ToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request)
    {
        var category = await GetOrThrow(id);
        category.Update(request.Name, request.Description);
        _categories.Update(category);
        await _categories.SaveChangesAsync();
        return ToDto(category);
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await GetOrThrow(id);
        _categories.Remove(category);
        await _categories.SaveChangesAsync();
    }

    private async Task<Category> GetOrThrow(Guid id)
        => await _categories.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Category {id} not found.");

    private static CategoryDto ToDto(Category c)
        => new(c.Id, c.Name, c.Description);
}
