# Frontend — Library app

React 19 + TypeScript + Vite + Material UI. Tab-based shell (no router) with JWT
session auth against the .NET backend.

## Run

```powershell
cd frontend
npm install
npm run dev      # http://localhost:5173
npm test         # vitest, 33 tests
npm run build    # tsc + vite
npm run lint     # oxlint
```

Backend URL defaults to `http://localhost:5008`; override via `.env`
(`VITE_API_URL=...`).

## Structure

```
src/
  api/library.ts          every backend call + token handling + role helpers
  pages/                  one file per tab: Login, Dashboard, Books, Rentals,
                          Members, Authors, Categories
  components/             PaginationBar (+usePagination), ErrorBoundary,
                          DashboardCharts (lazy)
  utils/                  validation.ts, libraryUtils.ts (due labels, CSV, truncate)
  App.tsx                 auth gate, tabs, theme, nav badge
```

## Data flow

- All HTTP lives in `api/library.ts`. `req()` attaches `Authorization: Bearer …`
  from localStorage; `handleRes()` turns non-2xx into `Error`s with the backend's
  message, clears the session on 401, and gives 403 a read-only explanation.
- Pages load on mount (`useEffect` + `Promise.all`), then filter/sort/paginate
  **client-side** — the API has no query params, so a shared `usePagination` hook
  slices locally, clamps the page when filters shrink the list, and resets to
  page 1 on filter/sort change.
- Auth state is `{ email, role, token }` in `App`; `canWrite` (via `isLibrarian()`,
  compared against a single `LIBRARIAN_ROLE` const) is passed to pages, which hide
  every mutation button for Assistants. The backend 403s such calls anyway — UI
  gating is defense in depth.
- Expired tokens log you out: the badge fetch in `App` mirrors the storage clear
  into state so you land back on login instead of a stuck session.

## Architecture decisions

1. **No react-router — deliberate.** Six tabs fully served by string state; a router
   adds restructuring cost for shareable URLs nobody needs here. Each tab's state
   resets on switch (documented tradeoff, not a bug).
2. **Validation mirrors the backend** (`utils/validation.ts`): EF column caps and
   domain guards (ISBN 10/13 digits, year/pages ranges, cover-URL parsing, loan
   days 1–365, 5-active-loan cap surfaced in the borrow dropdown). Field-level
   errors inline; the banner is reserved for server failures. 27 unit tests pin it.
3. **Bundle discipline.** Tab pages are `React.lazy` (main chunk ~566KB → ~267KB)
   and recharts ships in its own chunk loaded only by the dashboard.
4. **Resilience over cleverness.** Per-tab `ErrorBoundary` (keyed by tab, so
   switching resets it), skeleton fallbacks for lazy chunks, empty states on every
   list, dark mode via a persisted MUI theme toggle.
5. **Dates/dues computed locally** (`dueLabel`: "Due in 3 days" / "2 days overdue")
   from `dueDate` + `status` — no extra endpoints needed.
6. **CSV export** serializes the full filtered set (not just the visible page) with
   RFC-4180 quoting.

## Suggested walkthrough (2 minutes)

1. One-click **Sign in as Librarian** → Books: search, stock filter, sort, add a
   book (try a bad ISBN to see inline validation), open Details, add a copy.
2. Rentals: borrow form shows `n/5 active` per member; return/renew a loan.
3. Dashboard: stat cards, charts, overdue list with day counts.
4. Log out → **Sign in as Assistant**: write buttons gone, warning banners shown,
   everything else readable.

## Future improvements

1. Server-side paging/search once lists grow (biggest scalability gap).
2. Silent token refresh instead of the current hard 8h logout.
3. More test coverage beyond validators/pagination (component + e2e).
