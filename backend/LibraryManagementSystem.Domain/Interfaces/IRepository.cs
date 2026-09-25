using LibraryManagementSystem.Domain.Entities;

namespace LibraryManagementSystem.Domain.Interfaces;

// Data-access contract with zero EF knowledge: Application services code
// against this, Infrastructure provides the EF implementation.
// Interview point: this is the Dependency Inversion that keeps layers decoupled.
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<T>> ListAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    // One save for everything tracked in this request, so multi-entity
    // changes (Loan + BookCopy in Borrow) commit atomically.
    Task<int> SaveChangesAsync();
}
