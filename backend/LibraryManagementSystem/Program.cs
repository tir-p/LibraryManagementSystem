using LibraryManagementSystem.API.Middleware;
using LibraryManagementSystem.Application;
using LibraryManagementSystem.Infrastructure;
using LibraryManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

// Composition root: the ONLY place that knows all layers.
// Controllers -> services -> repositories -> DbContext are wired here;
// layers themselves only depend on abstractions (interfaces).
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Use cases (BookService, LoanService...).
builder.Services.AddApplication();
// DbContext + generic repositories (connection string from appsettings.json).
builder.Services.AddInfrastructure(builder.Configuration);
// Maps exceptions to HTTP codes (404 / 400 / 500) so controllers need no try/catch.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
// Allow Vite dev server to call the API from the browser.
builder.Services.AddCors((options) =>
{
    options.AddPolicy(
        "AllowFrontend",
        (policy) =>
            policy
                .WithOrigins("http://localhost:5173", "http://localhost:5174")
                .AllowAnyHeader()
                .AllowAnyMethod()
    );
});

var app = builder.Build();

// Apply migrations + seed programming books / Atomic Habits on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    await db.Database.MigrateAsync();
    await DataSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Exception handler first so even CORS/HTTPS-redirect errors become JSON.
app.UseExceptionHandler();

// Skip HTTPS redirect locally: the "http" launch profile only listens on
// http://localhost:5008, so redirecting to https just breaks fetch().
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
