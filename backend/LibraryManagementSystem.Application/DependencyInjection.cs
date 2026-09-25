using LibraryManagementSystem.Application.Interfaces;
using LibraryManagementSystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagementSystem.Application;

// Registers all use-case services. API calls AddApplication once in Program.cs.
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthorService, AuthorService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<ILoanService, LoanService>();
        return services;
    }
}
