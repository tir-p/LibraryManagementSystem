# Library Management System

Full-stack library app: ASP.NET Core 10 Web API + React 19 + TypeScript + Vite frontend
with Material UI. Staff log in with role-based access — **Librarians** get full write
access, **Assistant librarians** are read-only.

## Layout

```
backend/    .NET solution (API + Application + Domain + Infrastructure)
frontend/   React + Vite + MUI app
```

- `backend/README.md` — API architecture, auth design, domain rules
- `frontend/README.md` — UI architecture, data flow, key decisions

## Run it locally

**Backend** (needs .NET 10 SDK + SQL Server LocalDB):

```powershell
cd backend/LibraryManagementSystem
dotnet run --urls "http://localhost:5008"
```

Migrations apply and demo data seeds automatically on startup. API reference (Scalar)
at `http://localhost:5008/scalar/v1` in Development.

**Frontend:**

```powershell
cd frontend
npm install
npm run dev     # http://localhost:5173
```

The API URL defaults to `http://localhost:5008`; override with a `.env` file
(`VITE_API_URL=...`). Other useful scripts: `npm test`, `npm run build`, `npm run lint`.

## Demo accounts

| Role | Email | Password | Access |
|---|---|---|---|
| Librarian | `librarian@library.com` | `Librarian123!` | Full read + write |
| Assistant | `assistant@library.com` | `Assistant123!` | Read-only (writes return 403) |

One-click sign-in buttons for both are on the login page. Credentials are overridable
via `backend/.../appsettings.json` (`Auth` section) or environment variables.

## What it does

- **Books** — catalog with covers, descriptions, search/filter/sort, add/edit/delete,
  physical-copy management, CSV export
- **Rentals** — borrow/return/renew with due-date tracking, overdue flagging,
  member loan-cap enforcement (5 active), CSV export
- **Members** — CRUD, activate/deactivate, expandable loan history, CSV export
- **Authors / Categories** — full CRUD (deletion blocked while books reference them)
- **Dashboard** — stat cards, lazy-loaded charts (titles per category, borrows per
  month, availability donut), paginated overdue list

## Architecture decisions

1. **Roles enforced on both sides.** `[Authorize(Roles = …)]` per endpoint is the
   real gate; the UI hides write buttons as defense in depth. Verified live:
   no token → 401, assistant POST → 403, librarian POST → 201.
2. **In-memory staff store, PBKDF2-hashed.** No Users table/migration needed for two
   demo accounts; passwords never stored in plaintext. A real system would back
   `IAuthService` with a Users table.
3. **Client-side paging/filtering/sorting.** The API has no paging params, so pages
   slice locally via a shared `usePagination` hook. Correct at this scale; the
   natural next step is server-side paging past a few thousand rows.
4. **Validation mirrors the backend.** `utils/validation.ts` encodes the EF column
   caps and domain guards, so bad input fails inline instead of as API errors.
5. **Bundle discipline.** Tab pages and recharts are lazy-loaded; the main chunk
   dropped from ~566KB to ~267KB. Charts ship in their own chunk.
6. **Skipped deliberately:** full React Router (tab state is enough for 6 tabs —
   a hash sync would cover deep links), a notifications panel (duplicates the
   overdue list + nav badge), refresh tokens (needs backend support).

## Verification status

- Backend: builds with 0 warnings, 0 errors
- Frontend: 33/33 vitest tests pass, `vite build` passes, oxlint 0 errors

## Attribution

Developed with AI assistance — Muse Spark 1.3 via the OpenCode harness.
