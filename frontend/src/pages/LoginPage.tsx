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

// Hardcoded demo account - replace with backend call later
export const DEMO_EMAIL = 'admin@library.com';
export const DEMO_PASSWORD = 'admin123';

// Demo-only login: credentials are checked locally, no backend call yet.
// Props = inputs a parent passes in. Here App gives us `onLogin`, a callback
// we invoke on success so App can switch from login screen to the main UI.
type LoginPageProps = {
  onLogin: (email: string) => void;
};

export default function LoginPage({ onLogin }: LoginPageProps) {
  // Controlled inputs: the TextField's `value` always mirrors this state,
  // and every keystroke updates it via onChange. Source of truth = state, not the DOM.
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  // UI-only state: password visibility, error banner text, submit spinner.
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  // Form submit handler. preventDefault() stops the browser's native
  // full-page-reload form behavior so React stays in control.
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

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

    // Fake async login: setLoading disables the button + shows a spinner,
    // finally always resets loading whether login succeeded or failed.
    setLoading(true);
    try {
      await new Promise((resolve) => setTimeout(resolve, 800));
      if (email === DEMO_EMAIL && password === DEMO_PASSWORD) {
        onLogin(email);
      } else {
        setError('Invalid email or password. Try the demo account above.');
      }
    } catch {
      setError('Login failed. Please try again.');
    } finally {
      setLoading(false);
    }
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
              Sign in to manage books
            </Typography>
          </Box>

          <Alert severity="info" sx={{ mb: 2 }}>
            Demo login — Email: {DEMO_EMAIL} / Password: {DEMO_PASSWORD}
          </Alert>

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
                disabled={loading} blocks double-submits during the fake delay. */}
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
