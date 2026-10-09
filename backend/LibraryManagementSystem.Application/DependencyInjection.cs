using LibraryManagementSystem.Application.Interfaces;
using LibraryManagementSystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagementSystem.Application;

// -----------------------------------------------------------------------------
// File header:
// DependencyInjection wires up the Application layer for dependency injection.
// It registers each use-case service (authors, books, members, loans, etc.)
// so controllers can request an interface (e.g. IBookService) and get the
// matching implementation automatically. Called once in Program.cs.
// For junior devs: AddScoped means one instance per HTTP request.
// -----------------------------------------------------------------------------
/// <summary>
/// Extension methods that register Application-layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Application services with scoped lifetime.
    /// </summary>
    /// <param name="services">The service collection built in Program.cs.</param>
    /// <returns>The same collection, so calls can be chained.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Author use cases (create / list / update / delete authors).
        services.AddScoped<IAuthorService, AuthorService>();
        // Category use cases (genres/sections like "Fiction").
        services.AddScoped<ICategoryService, CategoryService>();
        // Book + BookCopy use cases (titles and physical copies).
        services.AddScoped<IBookService, BookService>();
        // Member use cases (register, update, activate/deactivate).
        services.AddScoped<IMemberService, MemberService>();
        // Loan use cases (borrow, return, renew — the core library rules).
        services.AddScoped<ILoanService, LoanService>();
        return services;
    }
}
