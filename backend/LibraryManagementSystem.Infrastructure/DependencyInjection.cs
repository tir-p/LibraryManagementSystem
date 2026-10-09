// ---------------------------------------------------------------------------
// File: DependencyInjection.cs
// Purpose: Wires up Infrastructure services (DbContext + repositories) for DI.
// Layer: Infrastructure - called once from Program.cs via AddInfrastructure().
// For juniors: This is the composition root for persistence; connection string
//   comes from configuration (appsettings.json), never hardcoded here.
// ---------------------------------------------------------------------------
using LibraryManagementSystem.Domain.Interfaces;
using LibraryManagementSystem.Infrastructure.Persistence;
using LibraryManagementSystem.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagementSystem.Infrastructure;

/// <summary>
/// Single entry point for Infrastructure dependency injection.
/// </summary>
/// <remarks>
/// For juniors: API calls AddInfrastructure() once in Program.cs.
/// </remarks>
// Single entry point for Infrastructure DI. API calls AddInfrastructure once;
// connection string stays in appsettings.json, never hardcoded here.
public static class DependencyInjection
{
    /// <summary>
    /// Registers DbContext and repositories into the DI container.
    /// </summary>
    /// <param name="services">The service collection to add to (DI container).</param>
    /// <param name="config">App configuration holding the LibraryDb connection string.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Read connection string named "LibraryDb" from appsettings.json.
        // Fail fast with clear error if missing (avoids cryptic null later).
        var connectionString = config.GetConnectionString("LibraryDb")
            ?? throw new InvalidOperationException("Connection string 'LibraryDb' not found.");

        // Scoped DbContext (one per request) sharing the connection.
        // UseSqlServer selects the SQL Server provider; options carry conn string.
        services.AddDbContext<LibraryDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Open generics: IRepository<Book>, IRepository<Member>... all resolve to GenericRepository<>.
        // Scoped lifetime matches DbContext (safe: same context per request).
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

        // Return services to allow chaining (builder pattern).
        return services;
    }
}
