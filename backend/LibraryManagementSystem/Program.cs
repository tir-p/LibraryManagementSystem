using LibraryManagementSystem.API.Middleware;
using LibraryManagementSystem.Application;
using LibraryManagementSystem.Infrastructure;
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Placed before MapControllers so controller exceptions route through the handler.
app.UseExceptionHandler();

app.MapControllers();

app.Run();
