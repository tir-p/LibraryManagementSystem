using LibraryManagementSystem.Domain.Interfaces;
using LibraryManagementSystem.Infrastructure.Persistence;
using LibraryManagementSystem.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagementSystem.Infrastructure;

// Single entry point for Infrastructure DI. API calls AddInfrastructure once;
// connection string stays in appsettings.json, never hardcoded here.
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("LibraryDb")
            ?? throw new InvalidOperationException("Connection string 'LibraryDb' not found.");

        // Scoped DbContext (one per request) sharing the connection.
        services.AddDbContext<LibraryDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Open generics: IRepository<Book>, IRepository<Member>... all resolve to GenericRepository<>.
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

        return services;
    }
}
