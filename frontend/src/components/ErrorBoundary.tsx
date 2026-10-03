import { Component, type ReactNode } from 'react';
import { Alert, Box, Button, Container, Typography } from '@mui/material';
import { Refresh } from '@mui/icons-material';

// Catches render crashes in the tab below it so one broken page can't take
// down the nav + toolbar. Must be a class: function components can't be
// error boundaries. `key` it by tab in App so switching tabs resets it.
type Props = {
  children: ReactNode;
  onReset?: () => void;
};

type State = {
  error: Error | null;
};

export default class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error) {
    // Visible in devtools + prod logs; no reporting service in this app.
    console.error('Page crashed:', error);
  }

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
