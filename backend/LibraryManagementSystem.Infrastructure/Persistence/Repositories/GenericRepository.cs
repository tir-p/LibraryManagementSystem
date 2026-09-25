using LibraryManagementSystem.Domain.Entities;
using LibraryManagementSystem.Domain.Interfaces;
using LibraryManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Infrastructure.Persistence.Repositories;

// The only repository implementation: one generic class serves every entity,
// so no per-entity boilerplate. Registered as open generics in DI
// (IRepository<Book>, IRepository<Member>... all resolve here).
public class GenericRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly LibraryDbContext _db;
    private readonly DbSet<T> _set;

    public GenericRepository(LibraryDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    // Returns null when missing; services turn that into a 404 (KeyNotFoundException).
    public async Task<T?> GetByIdAsync(Guid id)
        => await _set.FindAsync(id);

    // AsNoTracking: reads are faster and don't attach entities we won't modify.
    public async Task<IReadOnlyList<T>> ListAsync()
        => await _set.AsNoTracking().ToListAsync();

    public async Task AddAsync(T entity)
        => await _set.AddAsync(entity);

    public void Update(T entity) => _set.Update(entity);

    public void Remove(T entity) => _set.Remove(entity);

    public Task<int> SaveChangesAsync()
        => _db.SaveChangesAsync();
}
