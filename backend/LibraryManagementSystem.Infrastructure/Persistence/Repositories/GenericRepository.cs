// ---------------------------------------------------------------------------
// File: GenericRepository.cs
// Purpose: Single generic IRepository<T> implementation for all entities.
// Layer: Infrastructure / Persistence / Repositories.
// For juniors: Avoids per-entity boilerplate; registered as open generics in
//   DI so IRepository<Book>, IRepository<Member>, etc. all resolve here.
// ---------------------------------------------------------------------------
using LibraryManagementSystem.Domain.Entities;
using LibraryManagementSystem.Domain.Interfaces;
using LibraryManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic repository serving every entity type (Book, Member, etc.).
/// </summary>
/// <typeparam name="T">Entity type, must inherit BaseEntity (has Id).</typeparam>
/// <remarks>
/// For juniors: One class avoids per-entity boilerplate. Registered as open
/// generics in DI so IRepository&lt;T&gt; resolves here.
/// </remarks>
// The only repository implementation: one generic class serves every entity,
// so no per-entity boilerplate. Registered as open generics in DI
// (IRepository<Book>, IRepository<Member>... all resolve here).
public class GenericRepository<T> : IRepository<T> where T : BaseEntity
{
    /// <summary>EF context used for SaveChanges and tracking.</summary>
    private readonly LibraryDbContext _db;
    /// <summary>Typed table handle for the entity (e.g. Books, Members).</summary>
    private readonly DbSet<T> _set;

    /// <summary>
    /// Creates the repository with a scoped DbContext.
    /// </summary>
    /// <param name="db">Scoped LibraryDbContext (one per request).</param>
    public GenericRepository(LibraryDbContext db)
    {
        // Keep context for SaveChanges (unit-of-work commit).
        _db = db;
        // Resolve typed table handle once (e.g. db.Set<Book>()).
        _set = db.Set<T>();
    }

    /// <summary>
    /// Gets one entity by primary key, or null if not found.
    /// </summary>
    /// <param name="id">Entity Id (from BaseEntity).</param>
    /// <returns>The entity or null; services map null to 404.</returns>
    // Returns null when missing; services turn that into a 404 (KeyNotFoundException).
    public async Task<T?> GetByIdAsync(Guid id)
        // FindAsync: checks tracked cache first, then DB by PK - fastest lookup.
        => await _set.FindAsync(id);

    /// <summary>
    /// Lists all entities (read-only, no tracking).
    /// </summary>
    /// <returns>All rows as a read-only list.</returns>
    // AsNoTracking: reads are faster and don't attach entities we won't modify.
    public async Task<IReadOnlyList<T>> ListAsync()
        => await _set.AsNoTracking().ToListAsync();

    /// <summary>
    /// Stages a new entity for insert (call SaveChanges to persist).
    /// </summary>
    /// <param name="entity">New entity to add.</param>
    public async Task AddAsync(T entity)
        // AddAsync only tracks; actual INSERT happens on SaveChangesAsync.
        => await _set.AddAsync(entity);

    /// <summary>
    /// Marks an existing entity as modified.
    /// </summary>
    /// <param name="entity">Entity with updated values.</param>
    public void Update(T entity) => _set.Update(entity);

    /// <summary>
    /// Marks an entity for deletion.
    /// </summary>
    /// <param name="entity">Entity to remove.</param>
    public void Remove(T entity) => _set.Remove(entity);

    /// <summary>
    /// Persists all staged changes to the database.
    /// </summary>
    /// <returns>Number of rows written.</returns>
    public Task<int> SaveChangesAsync()
        // Delegates to DbContext so Unit-of-Work commits Add/Update/Remove together.
        => _db.SaveChangesAsync();
}
