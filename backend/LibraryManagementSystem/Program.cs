using System.Text;
using LibraryManagementSystem.API.Middleware;
using LibraryManagementSystem.API.Services;
using LibraryManagementSystem.Application;
using LibraryManagementSystem.Application.Interfaces;
using LibraryManagementSystem.Infrastructure;
using LibraryManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

// Composition root: the ONLY place that knows all layers.
// Controllers -> services -> repositories -> DbContext are wired here;
// layers themselves only depend on abstractions (interfaces).
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Use cases (BookService, LoanService...).
builder.Services.AddApplication();
// Staff login (JWT minting). Singleton: holds the in-memory demo staff store.
builder.Services.AddSingleton<IAuthService, AuthService>();
// Staff JWT auth: Librarian (full access) + Assistant (read-only, enforced
// per-endpoint with [Authorize(Roles = ...)]). Demo credentials live in
// appsettings Auth section; signing key in Jwt:Key (use secrets in prod).
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
