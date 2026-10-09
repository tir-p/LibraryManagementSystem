// ---------------------------------------------------------------------------
// File: Program.cs
// Purpose: Composition root (minimal-API bootstrap) — wires DI, JWT auth, CORS,
//   EF Core, middleware pipeline, and startup seeding. The ONLY place that
//   knows all layers; layers themselves depend only on abstractions.
// ---------------------------------------------------------------------------
using System.Text;
using LibraryManagementSystem.API.Middleware;
using LibraryManagementSystem.API.Services;
using LibraryManagementSystem.Application;
using LibraryManagementSystem.Application.Interfaces;
using LibraryManagementSystem.Infrastructure;
using LibraryManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

// Composition root: the ONLY place that knows all layers.
// Controllers -> services -> repositories -> DbContext are wired here;
// layers themselves only depend on abstractions (interfaces).
var builder = WebApplication.CreateBuilder(args);

// DI: controllers are instantiated per-request with services injected via constructors.
builder.Services.AddControllers();
// Use cases (BookService, LoanService...).
builder.Services.AddApplication();
// Staff login (JWT minting). Singleton: holds the in-memory demo staff store.
builder.Services.AddSingleton<IAuthService, AuthService>();
// Staff JWT auth: Librarian (full access) + Assistant (read-only, enforced
// per-endpoint with [Authorize(Roles = ...)]). Demo credentials live in
// appsettings Auth section; signing key in Jwt:Key (use secrets in prod).
// JWT logic: bearer tokens signed with symmetric key (Jwt:Key). Issuer/audience/
// lifetime all validated; 1-min ClockSkew tolerates small server clock drift.
// Auth wiring: AddAuthentication selects JwtBearer; AddAuthorization enables [Authorize].
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
// Error handling: AddProblemDetails + AddExceptionHandler route all unhandled
// exceptions to GlobalExceptionHandler (404/400/500 JSON), so controllers need no try/catch.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
// Trust Azure's TLS-terminating proxy (App Service, Container Apps, ...).
// Known proxies/networks are cleared because the platform front-end is the
// only ingress; without this, forwarded proto/host are ignored and the
// HTTPS redirect below can loop behind the proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
// Allowed browser origins come from config so Azure needs no code change:
// local defaults here, production URL via Cors__AllowedOrigins__0 (App Settings).
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:5174"];
builder.Services.AddCors((options) =>
{
    options.AddPolicy(
        "AllowFrontend",
        (policy) =>
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
    );
});

var app = builder.Build();

// Startup step: run EF migrations then seed demo data before serving traffic.
// CreateScope = short-lived DI scope for scoped DbContext outside a request.
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

// Forwarded headers first: everything downstream (HTTPS redirect, auth,
// link generation) must see the proxy's original scheme/host, not the
// internal HTTP hop.
// Exception handler next so even CORS/HTTPS-redirect errors become JSON.
// Middleware pipeline (order matters — each step wraps the next):
// 1) ForwardedHeaders 2) ExceptionHandler (JSON errors) 3) HttpsRedirection (prod only)
// 4) CORS 5) Authentication (who are you?) 6) Authorization (what may you do?) 7) Controllers.
app.UseForwardedHeaders();
app.UseExceptionHandler();

// Skip HTTPS redirect locally: the "http" launch profile only listens on
// http://localhost:5008, so redirecting to https just breaks fetch().
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

// Auth middleware: reads Bearer JWT into HttpContext.User (must run before Authorization).
app.UseAuthentication();
app.UseAuthorization();

// Endpoint routing: maps api/[controller] routes to controller actions.
app.MapControllers();

app.Run();
