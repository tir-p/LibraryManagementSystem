// File: IRepository.cs
// Purpose: Defines the generic data-access contract for domain entities.
// Application services code against this interface; Infrastructure provides
// the EF Core implementation. This keeps the Domain layer free of EF knowledge.
using LibraryManagementSystem.Domain.Entities;

namespace LibraryManagementSystem.Domain.Interfaces;

/// <summary>
/// Generic data-access contract for a <see cref="BaseEntity"/> type.
/// Implemented by Infrastructure with EF Core; consumed by Application services.
/// </summary>
/// <typeparam name="T">Entity type this repository works with. Must inherit <see cref="BaseEntity"/>.</typeparam>
// Data-access contract with zero EF knowledge: Application services code
// against this, Infrastructure provides the EF implementation.
// Interview point: this is the Dependency Inversion that keeps layers decoupled.
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Finds a single entity by its unique id.
    /// </summary>
    /// <param name="id">The entity id to look up.</param>
    /// <returns>The entity if found; otherwise null.</returns>
    Task<T?> GetByIdAsync(Guid id);
    /// <summary>
    /// Returns all entities of this type as a read-only list.
    /// </summary>
    /// <returns>All stored entities. May be empty but never null.</returns>
    Task<IReadOnlyList<T>> ListAsync();
    /// <summary>
    /// Stages a new entity to be inserted on the next save.
    /// </summary>
    /// <param name="entity">The new entity to add.</param>
    Task AddAsync(T entity);
    /// <summary>
    /// Marks an existing tracked entity as modified.
    /// </summary>
    /// <param name="entity">The entity with updated values.</param>
    void Update(T entity);
    /// <summary>
    /// Marks an existing tracked entity for deletion.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(T entity);
    /// <summary>
    /// Persists all tracked changes in one go.
    /// Lets multi-entity changes (Loan + BookCopy in Borrow) commit atomically.
    /// </summary>
    /// <returns>The number of database rows written.</returns>
    // One save for everything tracked in this request, so multi-entity
    // changes (Loan + BookCopy in Borrow) commit atomically.
    Task<int> SaveChangesAsync();
}
