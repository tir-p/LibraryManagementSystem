import { useState } from 'react';
import { AppBar, Box, Button, Tab, Tabs, Toolbar, Typography } from '@mui/material';
// Each screen lives in its own file under src/pages so App stays small.
import LoginPage from './pages/LoginPage';
import BooksPage from './pages/BooksPage';
import RentalsPage from './pages/RentalsPage';
import MembersPage from './pages/MembersPage';

// Key for remembering the login in the browser (see handleLogin/handleLogout).
const STORAGE_KEY = 'libraryUserEmail';

function App() {
  // useState = component memory. Changing it re-renders the UI.
  // Lazy initializer (() => ...) reads localStorage only once on first render.
  // null = logged out, string = logged-in email.
  const [userEmail, setUserEmail] = useState<string | null>(
    () => localStorage.getItem(STORAGE_KEY),
  );
  // Which tab is shown. Simple string state acts as our "router" (no react-router needed).
  const [page, setPage] = useState<'books' | 'rentals' | 'members'>('books');

  // Called by <LoginPage onLogin={handleLogin}> after the demo credential check passes.
  const handleLogin = (email: string) => {
    localStorage.setItem(STORAGE_KEY, email);
    setUserEmail(email);
  };

  // Clears the saved login; App re-renders and falls back to the login screen below.
  const handleLogout = () => {
    localStorage.removeItem(STORAGE_KEY);
    setUserEmail(null);
  };

  // Gate: no email -> login screen only. This is conditional rendering:
  // returning early means the nav + pages below never mount while logged out.
  if (!userEmail) {
    return <LoginPage onLogin={handleLogin} />;
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: '#f5f5f5' }}>
      <AppBar position="static">
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1 }}>
            Library System
          </Typography>
          <Typography variant="body2" sx={{ mr: 2 }}>
            {userEmail}
          </Typography>
          <Button color="inherit" onClick={handleLogout}>
            Log out
          </Button>
        </Toolbar>
        <Tabs
          value={page}
          onChange={(_, v) => setPage(v)}
          textColor="inherit"
          indicatorColor="secondary"
          sx={{ px: 2 }}
        >
          <Tab value="books" label="Books" />
          <Tab value="rentals" label="Rentals" />
          <Tab value="members" label="Members" />
        </Tabs>
      </AppBar>

      {/* Tab bar: value={page} highlights the active tab, onChange updates state. */}
      {/* Only the selected page mounts: {page === 'books' && <BooksPage />} renders
          BooksPage when true, nothing when false. Inactive pages lose their state. */}
      {page === 'books' && <BooksPage />}
      {page === 'rentals' && <RentalsPage />}
      {page === 'members' && <MembersPage />}
    </Box>
  );
}

export default App;
