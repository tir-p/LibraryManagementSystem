/**
 * App.tsx — Root shell: theme, auth gate, top nav tabs, and lazy page routing.
 * Junior-dev guide:
 * - No react-router; `page` string state picks which page component mounts.
 * - Auth session lives here; logged-out users only see <LoginPage/>.
 * - Pages are React.lazy() chunks; ErrorBoundary isolates per-tab crashes.
 */
import { Suspense, lazy, useEffect, useMemo, useState } from 'react';
import {
  AppBar,
  Badge,
  Box,
  Button,
  Chip,
  CssBaseline,
  IconButton,
  Skeleton,
  Tab,
  Tabs,
  Toolbar,
  Typography,
  ThemeProvider,
  createTheme,
} from '@mui/material';
import { Brightness4, Brightness7, LibraryBooks } from '@mui/icons-material';
import ErrorBoundary from './components/ErrorBoundary';
import {
  api,
  clearStoredAuth,
  getStoredAuth,
  isLibrarian,
  setStoredAuth,
  type AuthUser,
} from './api/library';
// Each screen lives in its own file under src/pages so App stays small.
// Tab pages are lazy: each becomes its own chunk, downloaded only when its
// tab opens (login stays in the initial bundle for fast first paint).
import LoginPage from './pages/LoginPage';

const DashboardPage = lazy(() => import('./pages/DashboardPage'));
const BooksPage = lazy(() => import('./pages/BooksPage'));
const RentalsPage = lazy(() => import('./pages/RentalsPage'));
const MembersPage = lazy(() => import('./pages/MembersPage'));
const AuthorsPage = lazy(() => import('./pages/AuthorsPage'));
const CategoriesPage = lazy(() => import('./pages/CategoriesPage'));

/**
 * Page — Tab identifiers used as a tiny client-side router (value of `page` state).
 */
export type Page = 'dashboard' | 'books' | 'rentals' | 'members' | 'authors' | 'categories';

const THEME_KEY = 'libraryTheme'; // localStorage key for persisted light/dark choice.

/**
 * App — Top-level component: provides MUI theme, nav bar, tab switching, and login gate.
 * @returns The full app UI (login screen when logged out, tabbed shell when logged in).
 */
function App() {
  // Auth session: null = logged out, AuthUser = JWT + email + role.
  // Lazy initializer (() => getStoredAuth()) reads localStorage only once on first render, not every render.
  const [user, setUser] = useState<AuthUser | null>(() => getStoredAuth());
  // Which tab is shown. Simple string state acts as our "router" (no react-router needed).
  // Example: setPage('books') unmounts Dashboard and mounts BooksPage.
  const [page, setPage] = useState<Page>('dashboard');
  // Dark mode, persisted like the session. Theme object is memoized so the
  // whole tree doesn't re-render on every keystroke elsewhere.
  // () => ... reads saved choice once: 'dark' stays dark, anything else starts light.
  const [mode, setMode] = useState<'light' | 'dark'>(
    () => (localStorage.getItem(THEME_KEY) === 'dark' ? 'dark' : 'light'),
  );
  // Open/overdue rental counts for the nav badge. Refreshed on login and
  // every tab switch so the badge is fresh after returning a book.
  // Example: openCount=5, overdueCount=2 -> Rentals tab shows red "2".
  const [openCount, setOpenCount] = useState(0);
  const [overdueCount, setOverdueCount] = useState(0);

  // useMemo: rebuild the MUI theme object only when `mode` flips (perf: avoids re-render churn).
  // Without useMemo, a new theme object every render would re-style the whole app every keystroke.
  const theme = useMemo(
    () =>
      createTheme({
        palette: {
          mode,
          primary: { main: '#1a5fb4' },
          secondary: { main: '#e5a50a' },
        },
      }),
    [mode],
  );

  // useEffect: side-effect that fetches loans after login/tab change to refresh the Rentals badge counts.
  // [user, page] = re-run when you log in/out OR switch tabs. if (!user) return = skip while logged out.
  // cancelled flag prevents "set state on unmounted component" if you log out mid-fetch.
  useEffect(() => {
    if (!user) return; // logged out: no badge to update, do nothing
    let cancelled = false; // becomes true in cleanup below if effect re-runs or unmounts
    api
      .getLoans()
      .then((loans) => {
        if (cancelled) return; // fetch finished late: ignore it, screen already changed
        const open = loans.filter((l) => l.status === 'Active' || l.status === 'Overdue');
        setOpenCount(open.length);
        setOverdueCount(loans.filter((l) => l.status === 'Overdue').length);
      })
      .catch(() => {
        // handleRes() already cleared storage on 401 (expired token) — mirror
        // that into state so the app falls back to login instead of sitting
        // "logged in" with a dead token.
        if (!cancelled && !getStoredAuth()) setUser(null);
      });
    return () => {
      cancelled = true;
    };
  }, [user, page]);

  // Only Librarians mutate data; Assistants get a read-only UI (the backend
  // also rejects their writes with 403, so this is defense in depth).
  const canWrite = isLibrarian(user);

  // Event handler: flips light/dark mode and persists the choice to localStorage.
  const toggleMode = () => {
    setMode((m) => {
      const next = m === 'light' ? 'dark' : 'light';
      localStorage.setItem(THEME_KEY, next);
      return next;
    });
  };

  // Called by <LoginPage onLogin={handleLogin}> after POST /api/auth/login succeeds.
  const handleLogin = (u: AuthUser) => {
    setStoredAuth(u);
    setUser(u);
  };

  // Clears the saved session; App re-renders and falls back to the login screen below.
  const handleLogout = () => {
    clearStoredAuth();
    setUser(null);
  };

  // Gate: no session -> login screen only. This is conditional rendering:
  // returning early means the nav + pages below never mount while logged out.
  if (!user) {
    return (
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <LoginPage onLogin={handleLogin} />
      </ThemeProvider>
    );
  }

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
        <AppBar position="static">
          <Toolbar>
            <LibraryBooks sx={{ mr: 1 }} />
            <Typography variant="h6" sx={{ flexGrow: 1 }}>
              Library System
            </Typography>
            <Chip
              label={user.role}
              size="small"
              color={canWrite ? 'secondary' : 'default'}
              sx={{ mr: 1, color: canWrite ? undefined : '#fff' }}
            />
            <Typography variant="body2" sx={{ mr: 1 }}>
              {user.email}
            </Typography>
            <IconButton color="inherit" onClick={toggleMode} title={mode === 'light' ? 'Dark mode' : 'Light mode'}>
              {mode === 'light' ? <Brightness4 /> : <Brightness7 />}
            </IconButton>
            <Button color="inherit" onClick={handleLogout}>
              Log out
            </Button>
          </Toolbar>
          <Tabs
            value={page}
            onChange={(_, v) => setPage(v)}
            textColor="inherit"
            indicatorColor="secondary"
            variant="scrollable"
            scrollButtons="auto"
            sx={{ px: 2 }}
          >
            <Tab value="dashboard" label="Dashboard" />
            <Tab value="books" label="Books" />
            <Tab
              value="rentals"
              label={
                <Badge
                  badgeContent={overdueCount > 0 ? overdueCount : openCount}
                  color={overdueCount > 0 ? 'error' : 'secondary'}
                  max={99}
                >
                  Rentals
                </Badge>
              }
            />
            <Tab value="members" label="Members" />
            <Tab value="authors" label="Authors" />
            <Tab value="categories" label="Categories" />
          </Tabs>
        </AppBar>

        {/* Tab bar: value={page} highlights the active tab, onChange updates state. */}
        {/* Only the selected page mounts: {page === 'books' && <BooksPage />} renders
            BooksPage when true, nothing when false. Inactive pages lose their state. */}
        {/* ErrorBoundary (keyed by tab) isolates crashes to the open page —
            the nav + toolbar stay alive, and switching tabs resets it. */}
        <ErrorBoundary key={page}>
          <Suspense
            fallback={
              <Box sx={{ p: 3, display: 'flex', flexDirection: 'column', gap: 2 }}>
                <Skeleton variant="rounded" height={56} />
                <Skeleton variant="rounded" height={120} />
                <Skeleton variant="rounded" height={120} />
              </Box>
            }
          >
            {page === 'dashboard' && <DashboardPage go={setPage} />}
            {page === 'books' && <BooksPage canWrite={canWrite} />}
            {page === 'rentals' && <RentalsPage canWrite={canWrite} />}
            {page === 'members' && <MembersPage canWrite={canWrite} />}
            {page === 'authors' && <AuthorsPage canWrite={canWrite} />}
            {page === 'categories' && <CategoriesPage canWrite={canWrite} />}
          </Suspense>
        </ErrorBoundary>
      </Box>
    </ThemeProvider>
  );
}

export default App;
