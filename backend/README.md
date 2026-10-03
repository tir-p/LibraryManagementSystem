# Backend — LibraryManagementSystem API

ASP.NET Core 10 Web API. Layered architecture: thin controllers → application
services (use cases) → generic repositories → EF Core. All business rules live
in the domain layer.

## Projects

- **LibraryManagementSystem** (API) — controllers, `Program.cs`, JWT auth wiring,
  `AuthService`, exception-handling middleware
- **LibraryManagementSystem.Application** — `IAuthService`/service interfaces,
  `*Service` use cases, DTO records
- **LibraryManagementSystem.Domain** — entities with constructor/method guards,
  enums, `DomainException`
- **LibraryManagementSystem.Infrastructure** — `DbContext`, EF configurations,
  generic repository, migrations, seeder

## Run

```powershell
cd backend/LibraryManagementSystem
dotnet run --urls "http://localhost:5008"
```

Uses SQL Server LocalDB (`ConnectionStrings:LibraryDb`). Migrations + demo seed run
on startup; seed is if-empty so restarts never wipe user data. Scalar API docs at
`/scalar/v1` (Development only).

## Auth design (roles)

- `POST /api/auth/login` validates against two demo staff accounts and mints a JWT
  carrying a role claim (`Librarian` or `Assistant`). `GET /api/auth/me` echoes it.
- Passwords are PBKDF2-hashed in memory (random salt, 100k SHA256 iterations) —
  plaintext lives only in config for this demo. `AuthService` is a singleton holding
  the store, registered in `Program.cs` (it lives in the API project, not
  Application, because JWT minting is infrastructure).
- Enforcement is per endpoint: GETs allow `UserRoles.ReadOnly`
  (`"Librarian,Assistant"`), every mutating endpoint requires `UserRoles.Librarian`.
  Role strings live in one `UserRoles` const class so renames touch one line.
- JWT validation (issuer/audience/lifetime/signing key, 1-min clock skew) is wired
  in `Program.cs`; `UseAuthentication()` runs before `UseAuthorization()`.
- Demo credentials: `appsettings.json` → `Auth` section; signing key in `Jwt:Key`
  (dev value committed — switch to user-secrets/a vault before any shared env).

## Domain rules (where the interesting logic lives)

- **Borrow cap:** `Member.CanBorrow(activeCount, maxLoans: 5)` — active membership +
  under 5 *active* loans (overdue loans don't count toward the cap).
- **Renewals:** max 2, active loans only (`Loan.Renew`).
- **Overdue:** flagged on read (`RefreshOverdueAsync` in `LoanService`), so status
  is never stale without a background job.
- **Deletes:** `Restrict` everywhere history matters (authors/categories/members
  with books/loans can't be deleted); `Cascade` only book → its copies.
- **Uniqueness:** ISBN, member email, barcode, category name (friendly 400s via
  pre-checks instead of raw SQL violations).
- **Errors:** `GlobalExceptionHandler` maps `KeyNotFoundException` → 404,
  `DomainException` → 400, everything else → 500. Controllers stay try/catch-free.

## Database & migrations

- Schema lives in `Infrastructure/Persistence` (`LibraryDbContext` + one
  `*Configuration` per entity + `Migrations/`). The app calls
  `MigrateAsync()` on startup, so a fresh LocalDB gets the schema with no
  manual steps.
- `DataSeeder` inserts the demo catalog (5 programming/self-help titles, 2 copies
  each) only when the Books table is empty — restarts never wipe user data.
- To evolve the schema after changing an entity/configuration:

```powershell
dotnet ef migrations add <Name> `
  --project backend/LibraryManagementSystem.Infrastructure `
  --startup-project backend/LibraryManagementSystem
```

## Conventions

- DTO records per operation (`BookDto` vs `CreateBookRequest`/`UpdateBookRequest`)
  so clients can't set ids/audit fields. JSON serializes camelCase by default,
  which the frontend types mirror.
- `GetAll` endpoints return full lists — no paging params. Fine for hundreds of
  rows; the frontend paginates client-side. Server-side paging is the known next
  step for larger catalogs.
- CORS allows the Vite dev servers (`5173`, `5174`); HTTPS redirect is
  production-only so local plain-HTTP fetch keeps working.

## Future improvements

1. Refresh tokens + sliding sessions (current JWTs are fixed 8h, no renewal).
2. Persistent users table behind `IAuthService` instead of the in-memory store.
3. Server-side paging/search/sort (`page`, `pageSize`, `q`, `sort` params).
4. Secrets management (no committed JWT key/passwords).
