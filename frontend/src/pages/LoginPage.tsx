/**
 * LoginPage.tsx — Staff sign-in form + one-click demo logins for both roles.
 * Junior-dev guide:
 * - Props: onLogin(user) lifts the AuthUser up to App so it can switch to the main UI.
 * - State: controlled email/password inputs + showPassword/error/loading UI flags.
 * - Flow: validate locally -> POST /api/auth/login -> setStoredAuth + onLogin.
 */
import { useState } from 'react';
import {
  Box,
  Button,
  Container,
  TextField,
  Typography,
  Paper,
  InputAdornment,
  IconButton,
  Alert,
  CircularProgress,
} from '@mui/material';
import { Visibility, VisibilityOff, LibraryBooks } from '@mui/icons-material';
import { api, setStoredAuth, type AuthUser } from '../api/library';

// Demo staff accounts (seeded by the backend AuthService, overridable via
// appsettings Auth section). Librarian = full write access,
// Assistant = read-only (GETs only, writes return 403).
export const LIBRARIAN_EMAIL = 'librarian@library.com';
export const LIBRARIAN_PASSWORD = 'Librarian123!';
export const ASSISTANT_EMAIL = 'assistant@library.com';
export const ASSISTANT_PASSWORD = 'Assistant123!';

// Backend login: POST /api/auth/login mints a JWT with the role claim.
// Props = inputs a parent passes in. Here App gives us `onLogin`, a callback
// we invoke on success so App can switch from login screen to the main UI.
/**
 * LoginPageProps — onLogin lifts the logged-in session to App.tsx.
 */
type LoginPageProps = {
  onLogin: (user: AuthUser) => void;
};

/**
 * LoginPage — Email/password form with validation, visibility toggle, and demo quick-logins.
 * @param onLogin Callback invoked with the AuthUser after successful login.
 */
export default function LoginPage({ onLogin }: LoginPageProps) {
  // Controlled inputs: the TextField's `value` always mirrors this state,
  // and every keystroke updates it via onChange. Source of truth = state, not the DOM.
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  // UI-only state: password visibility, error banner text, submit spinner.
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  // Single login path used by both the form and the one-click demo buttons.
  // async = this talks to the backend and waits for an answer without freezing the screen.
  const doLogin = async (e: string, p: string) => {
    setError(''); // clear old error banner before trying again
    setLoading(true); // show spinner + disable buttons so you can't click twice
    try {
      // This tells TypeScript: user will look like AuthUser (token + email + role).
      // await pauses here until POST /api/auth/login answers.
      const user: AuthUser = await api.login(e, p);
      setStoredAuth(user); // save token in localStorage so refresh keeps you logged in
      onLogin(user); // tell App "login worked" so it switches from login screen to tabs
    } catch (err) {
      // Any network or wrong-password error lands here and shows in the red banner.
      setError(err instanceof Error ? err.message : 'Login failed. Please try again.');
    } finally {
      setLoading(false); // always stop spinner, success or fail
    }
  };

  // One-click demo sign-in: fills the form AND submits against the backend,
  // so each role is a single click. Errors surface in the same banner.
  // void = "run the promise but don't wait here" (doLogin handles its own loading/error).
  const quickLogin = (e: string, p: string) => {
    setEmail(e);
    setPassword(p);
    void doLogin(e, p);
  };

  // Form submit handler. preventDefault() stops the browser's native
  // full-page-reload form behavior so React stays in control.
  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    // Validate locally before any network call to fail fast with clear messages.
    // `return` exits early: nothing below runs when validation fails.
    if (!email || !password) {
      setError('Please enter both email and password.');
      return;
    }
    if (!/\S+@\S+\.\S+/.test(email)) {
      setError('Please enter a valid email address.');
      return;
    }

    // .trim() removes accidental spaces at start/end before sending to backend.
    // Password is not trimmed on purpose: spaces could be part of a password.
    void doLogin(email.trim(), password);
  };

  return (
    <Container maxWidth="xs">
      <Box
        sx={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Paper elevation={3} sx={{ p: 4, width: '100%', borderRadius: 2 }}>
          <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', mb: 2 }}>
            <LibraryBooks color="primary" sx={{ fontSize: 48, mb: 1 }} />
            <Typography variant="h5" component="h1" sx={{ fontWeight: 'bold' }}>
              Library Login
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Sign in with your staff account
            </Typography>
          </Box>

          <Alert severity="info" sx={{ mb: 1 }}>
            Librarian (full access) — {LIBRARIAN_EMAIL} / {LIBRARIAN_PASSWORD}
          </Alert>
          <Alert severity="warning" sx={{ mb: 2 }}>
            Assistant (read-only) — {ASSISTANT_EMAIL} / {ASSISTANT_PASSWORD}
          </Alert>
          <Box sx={{ display: 'flex', gap: 1, mb: 1 }}>
            <Button
              size="small"
              variant="contained"
              disabled={loading}
              onClick={() => quickLogin(LIBRARIAN_EMAIL, LIBRARIAN_PASSWORD)}
            >
              Sign in as Librarian
            </Button>
            <Button
              size="small"
              variant="outlined"
              disabled={loading}
              onClick={() => quickLogin(ASSISTANT_EMAIL, ASSISTANT_PASSWORD)}
            >
              Sign in as Assistant
            </Button>
          </Box>

          {/* Conditional rendering: {error && (...)} shows the Alert only when
              error is a non-empty string, otherwise renders nothing. */}
          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}

          {/* Ternary: type switches between dots/password and plain text.
              slotProps.input.endAdornment injects the eye button inside the field. */}
          <Box component="form" onSubmit={handleSubmit} noValidate>
            <TextField
              label="Email"
              type="email"
              fullWidth
              required
              margin="normal"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              autoComplete="email"
              autoFocus
            />
            <TextField
              label="Password"
              type={showPassword ? 'text' : 'password'}
              fullWidth
              required
              margin="normal"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
              slotProps={{
                input: {
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton onClick={() => setShowPassword((s) => !s)} edge="end">
                        {showPassword ? <VisibilityOff /> : <Visibility />}
                      </IconButton>
                    </InputAdornment>
                  ),
                },
              }}
            />
            {/* Ternary in JSX: while loading show a spinner, else the label.
                disabled={loading} blocks double-submits during login. */}
            <Button
              type="submit"
              variant="contained"
              fullWidth
              size="large"
              disabled={loading}
              sx={{ mt: 3, mb: 2 }}
            >
              {loading ? <CircularProgress size={24} color="inherit" /> : 'Sign In'}
            </Button>
          </Box>
        </Paper>
      </Box>
    </Container>
  );
}
