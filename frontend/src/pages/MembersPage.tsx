import { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
  Typography,
} from '@mui/material';
import { Add, Person, Refresh } from '@mui/icons-material';
import { api, type Member } from '../api/library';

// Members: list with active/inactive chips, add-member dialog, activate/deactivate.
// Deactivating hides a member from the Rentals dropdown (backend also blocks them).
export default function MembersPage() {
  // members = server list; loading/error = page status.
  const [members, setMembers] = useState<Member[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Dialog visibility + one state per form field (controlled inputs).
  const [open, setOpen] = useState(false);
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  // Single loader reused by mount, Refresh button, create, and toggle.
  const load = async () => {
    setLoading(true);
    setError('');
    try {
      setMembers(await api.getMembers());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load members.');
    } finally {
      setLoading(false);
    }
  };

  // Fetch-on-mount: [] dependency array = run once when the page opens.
  useEffect(() => {
    load();
  }, []);

  // Validate -> POST -> close + clear form -> reload list so the row appears.
  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError('');
    if (!firstName || !lastName || !email) {
      setFormError('First name, last name and email are required.');
      return;
    }
    setSaving(true);
    try {
      await api.createMember({ firstName, lastName, email, phone: phone || undefined });
      setOpen(false);
      setFirstName('');
      setLastName('');
      setEmail('');
      setPhone('');
      await load();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Create failed.');
    } finally {
      setSaving(false);
    }
  };

  // One button toggles both directions; the label follows current state.
  // Reload after the POST so the chip + Rentals dropdown stay in sync.
  const toggleActive = async (m: Member) => {
    try {
      if (m.isActive) await api.deactivateMember(m.id);
      else await api.reactivateMember(m.id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Update failed.');
    }
  };

  return (
    <Container maxWidth="md" sx={{ py: 3 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
        <Person color="primary" />
        <Typography variant="h6" sx={{ flexGrow: 1 }}>
          Members ({members.length})
        </Typography>
        <Button variant="outlined" size="small" onClick={load} startIcon={<Refresh />}>
          Refresh
        </Button>
        <Button variant="contained" size="small" onClick={() => setOpen(true)} startIcon={<Add />}>
          Add Member
        </Button>
      </Box>

      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      )}
      {!loading && error && <Alert severity="error">{error}</Alert>}

      {!loading &&
        !error &&
        members.map((m) => (
          <Card key={m.id} variant="outlined" sx={{ mb: 1 }}>
            <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
              <Box sx={{ flexGrow: 1 }}>
                <Typography variant="subtitle1">{m.fullName}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {m.email}
                </Typography>
              </Box>
              <Chip
                label={m.isActive ? 'Active' : 'Inactive'}
                size="small"
                color={m.isActive ? 'success' : 'default'}
              />
              <Button size="small" variant="outlined" onClick={() => toggleActive(m)}>
                {m.isActive ? 'Deactivate' : 'Reactivate'}
              </Button>
            </CardContent>
          </Card>
        ))}

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Add member</DialogTitle>
        <Box component="form" onSubmit={handleCreate}>
          <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {formError && <Alert severity="error">{formError}</Alert>}
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="First name"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                required
                fullWidth
              />
              <TextField
                label="Last name"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                required
                fullWidth
              />
            </Box>
            <TextField
              label="Email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              fullWidth
            />
            <TextField
              label="Phone (optional)"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              fullWidth
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={saving}>
              {saving ? <CircularProgress size={20} /> : 'Save member'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Container>
  );
}
