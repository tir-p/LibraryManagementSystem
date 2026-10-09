/**
 * ErrorBoundary.tsx — Class-based crash catcher that isolates one tab's render failure.
 * Junior-dev guide:
 * - Must be a class (function components cannot be error boundaries).
 * - Wrap each tab in App.tsx with key={page} so switching tabs resets the error.
 * - Shows a friendly fallback with Try again / Reload buttons.
 */
import { Component, type ReactNode } from 'react';
import { Alert, Box, Button, Container, Typography } from '@mui/material';
import { Refresh } from '@mui/icons-material';

// Catches render crashes in the tab below it so one broken page can't take
// down the nav + toolbar. Must be a class: function components can't be
// error boundaries. `key` it by tab in App so switching tabs resets it.
/**
 * Props — children is the tab content; onReset optionally runs when user clicks Try again.
 */
type Props = {
  children: ReactNode;
  onReset?: () => void;
};

/** State — holds the caught Error, or null when healthy. */
type State = {
  error: Error | null;
};

/**
 * ErrorBoundary — Catches child render errors and shows a fallback instead of a blank screen.
 */
export default class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null };

  /** React lifecycle: copy a thrown render error into state so fallback UI shows. */
  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  /** React lifecycle: log the crash (devtools console; no external reporting service). */
  componentDidCatch(error: Error) {
    // Visible in devtools + prod logs; no reporting service in this app.
    console.error('Page crashed:', error);
  }

  /** Render either the fallback Alert + retry buttons (on error) or the normal children. */
  render() {
    if (this.state.error) {
      return (
        <Container maxWidth="md" sx={{ py: 4 }}>
          <Alert severity="error" sx={{ mb: 2 }}>
            Something went wrong loading this page.
          </Alert>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            {this.state.error.message}
          </Typography>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button
              variant="contained"
              size="small"
              startIcon={<Refresh />}
              onClick={() => {
                this.setState({ error: null });
                this.props.onReset?.();
              }}
            >
              Try again
            </Button>
            <Button size="small" onClick={() => window.location.reload()}>
              Reload app
            </Button>
          </Box>
        </Container>
      );
    }
    return this.props.children;
  }
}
