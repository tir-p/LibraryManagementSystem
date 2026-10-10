/**
 * MembersPage.tsx — Member directory with search/status/sort, add/edit, activate/deactivate, loan history, CSV export.
 * Junior-dev guide:
 * - Props: canWrite gates Add/Edit/Activate buttons (Assistants read-only).
 * - State: members + loans lists, filters, expandable historyId, dialog + form fields.
 * - Loans are pre-loaded once so per-member history needs no extra fetch.
 */
import { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Collapse,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  TextField,
  Typography,
} from '@mui/material';
import { Add, Download, Edit, ExpandMore, Person, Refresh } from '@mui/icons-material';
import { api, type Loan, type Member } from '../api/library';
import PaginationBar, { usePagination } from '../components/PaginationBar';
import { dueLabel, exportCsv } from '../utils/libraryUtils';
import {
  LIMITS,
  clearFieldError,
  email as emailValidator,
  phone as phoneValidator,
  text,
  type FieldErrors,
} from '../utils/validation';

// Members: search + active/inactive filter, add/edit dialog, activate/
// deactivate. Backend UpdateMemberRequest edits names + phone (not email).
/**
 * MembersPage — Searchable member list with expandable rental history and status toggling.
 * @param canWrite Shows Add/Edit/Deactivate buttons only for Librarians.
 */
export default function MembersPage({ canWrite }: { canWrite: boolean }) {
  // members = list from backend, loans = all rentals (used for history + counts, no extra fetch per member).
  // query/statusFilter/sort = what the user picked in the filter row.
  const [members, setMembers] = useState<Member[]>([]);
  const [loans, setLoans] = useState<Loan[]>([]);
  const [loading, setLoading] = useState(true); // true = show big spinner
  const [error, setError] = useState(''); // non-empty = show red banner
  const [query, setQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const [sort, setSort] = useState<'name' | 'newest' | 'oldest'>('name');
  // Loan history: which member card is expanded. Just an id (or null = all collapsed).
  // Loans are already loaded above, so expanding needs no new API call.
  const [historyId, setHistoryId] = useState<string | null>(null);

  // Dialog visibility + one state per form field (controlled inputs).
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Member | null>(null);
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  // Change handler that also clears the field's validation error on edit,
  // so fixed input immediately un-marks the field without re-submitting.
  const onEdit =
    (field: string, setter: (v: string) => void) => (e: React.ChangeEvent<HTMLInputElement>) => {
      setter(e.target.value);
      clearFieldError(setFieldErrors, field);
    };

  // Single loader reused by mount, Refresh button, create, and toggle.
  /** API call: fetch members + loans together; loans power history + active counts. */
  // Promise.all runs both GETs at the same time (faster than one after the other).
  // If loans fail we use [] so members still show (history just stays empty).
  const load = async () => {
    setLoading(true);
    setError('');
    try {
      const [m, l] = await Promise.all([
        api.getMembers(), // GET /api/members
        api.getLoans().catch(() => [] as Loan[]), // GET /api/loans, fallback to empty on error
      ]);
      setMembers(m);
      setLoans(l);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load members.');
    } finally {
      setLoading(false); // always stop spinner
    }
  };

  // Fetch-on-mount: [] dependency array = run once when the page opens.
  useEffect(() => {
    load();
  }, []);

  // Derived map: memberId -> how many open rentals they have.
  // Example: { "abc123": 2 } means that member has 2 active/overdue loans.
  // Recomputes only when loans change. Used in the card ("2 active rental(s)") + CSV export.
  const activeCountByMember = useMemo(() => {
    const map: Record<string, number> = {};
    loans.forEach((l) => {
      if (l.status === 'Active' || l.status === 'Overdue') {
        map[l.memberId] = (map[l.memberId] ?? 0) + 1;
      }
    });
    return map;
  }, [loans]);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    return members.filter((m) => {
      if (statusFilter === 'active' && !m.isActive) return false;
      if (statusFilter === 'inactive' && m.isActive) return false;
      if (!q) return true;
      return (
        m.fullName.toLowerCase().includes(q) ||
        m.email.toLowerCase().includes(q) ||
        (m.phone ?? '').toLowerCase().includes(q)
      );
    });
  }, [members, query, statusFilter]);

  const sorted = useMemo(() => {
    const arr = [...filtered];
    switch (sort) {
      case 'newest':
        return arr.sort(
          (a, b) => +new Date(b.membershipDate) - +new Date(a.membershipDate),
        );
      case 'oldest':
        return arr.sort(
          (a, b) => +new Date(a.membershipDate) - +new Date(b.membershipDate),
        );
      default:
        return arr.sort((a, b) => a.fullName.localeCompare(b.fullName));
    }
  }, [filtered, sort]);

  // Loans grouped by member for the expandable history section.
  // Shape: { memberId: [loan1, loan2] }. (map[id] ??= []) means "create empty array first time we see this id".
  // Each member's list is sorted newest-first. No API call here, just regrouping what we already loaded.
  const loansByMember = useMemo(() => {
    const map: Record<string, Loan[]> = {};
    loans.forEach((l) => {
      (map[l.memberId] ??= []).push(l);
    });
    Object.values(map).forEach((list) =>
      list.sort((a, b) => +new Date(b.borrowedAt) - +new Date(a.borrowedAt)),
    );
    return map;
  }, [loans]);

  // Paginate the sorted list. paged = cards for current page.
  // Reset to page 1 when filters change so you never sit on an empty page.
  const {
    page,
    setPage,
    pageSize,
    setPageSize,
    total,
    totalPages,
    paged,
  } = usePagination(sorted, 8);

  useEffect(() => {
    setPage(1);
  }, [query, statusFilter, sort, setPage]);

  // Export the whole sorted list (not just current page) to members.csv in Downloads.
  const handleExport = () => {
    exportCsv(
      'members.csv',
      ['FullName', 'Email', 'Phone', 'MembershipDate', 'Status', 'ActiveRentals'],
      sorted.map((m) => [
        m.fullName,
        m.email,
        m.phone ?? '',
        m.membershipDate ? new Date(m.membershipDate).toLocaleDateString() : '',
        m.isActive ? 'Active' : 'Inactive',
        activeCountByMember[m.id] ?? 0,
      ]),
    );
  };

  // Open empty dialog for Add. editing = null means create mode.
  const openCreate = () => {
    setEditing(null);
    setFirstName('');
    setLastName('');
    setEmail('');
    setPhone('');
    setFormError('');
    setFieldErrors({});
    setOpen(true);
  };

  // Open dialog filled with this member's values for Edit.
  const openEdit = (m: Member) => {
    setEditing(m);
    setFirstName(m.firstName);
    setLastName(m.lastName);
    setEmail(m.email);
    setPhone(m.phone ?? '');
    setFormError('');
    setFieldErrors({});
    setOpen(true);
  };

  // Validate every field locally (mirrors backend caps + guards), then
  // POST/PUT -> close + reload list so the row appears.
  // If errs has even one entry, return early: no API call happens.
  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault(); // stop browser full-page reload on form submit
    setFormError('');
    const errs: FieldErrors = {};
    const put = (field: string, msg: string | null) => {
      if (msg) errs[field] = msg;
    };
    put('firstName', text(firstName, 'First name', LIMITS.firstName));
    put('lastName', text(lastName, 'Last name', LIMITS.lastName));
    // Email is immutable after creation (field is disabled when editing).
    if (!editing) put('email', emailValidator(email));
    put('phone', phoneValidator(phone));
    setFieldErrors(errs);
    if (Object.keys(errs).length > 0) return;
    setSaving(true);
    try {
      if (editing) {
        await api.updateMember(editing.id, {
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          phone: phone.trim() || undefined,
        });
      } else {
        await api.createMember({
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          email: email.trim(),
          phone: phone.trim() || undefined,
        });
      }
      setOpen(false);
      await load();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Save failed.');
    } finally {
      setSaving(false);
    }
  };

  // One button toggles both directions; the label follows current state.
  // Reload after the POST so the chip + Rentals dropdown stay in sync.
  // Example: isActive=true -> button says "Deactivate" and calls deactivateMember.
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
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2, flexWrap: 'wrap' }}>
        <Person color="primary" />
        <Typography variant="h6" sx={{ flexGrow: 1 }}>
          Members ({filtered.length}/{members.length})
        </Typography>
        <Button variant="outlined" size="small" onClick={load} startIcon={<Refresh />}>
          Refresh
        </Button>
        <Button variant="outlined" size="small" onClick={handleExport} startIcon={<Download />} disabled={sorted.length === 0}>
          Export
        </Button>
        {canWrite && (
          <Button variant="contained" size="small" onClick={openCreate} startIcon={<Add />}>
            Add Member
          </Button>
        )}
      </Box>

      {!canWrite && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          You are signed in as Assistant (read-only). Sign in as a Librarian to manage members.
        </Alert>
      )}

      <Box sx={{ display: 'flex', gap: 1, mb: 2, flexWrap: 'wrap' }}>
        <TextField
          label="Search name, email, phone…"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          size="small"
          sx={{ flex: 2, minWidth: 200 }}
        />
        <TextField
          select
          label="Status"
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value as typeof statusFilter)}
          size="small"
          sx={{ minWidth: 150 }}
        >
          <MenuItem value="all">All</MenuItem>
          <MenuItem value="active">Active</MenuItem>
          <MenuItem value="inactive">Inactive</MenuItem>
        </TextField>
        <TextField
          select
          label="Sort"
          value={sort}
          onChange={(e) => setSort(e.target.value as typeof sort)}
          size="small"
          sx={{ minWidth: 150 }}
        >
          <MenuItem value="name">Name A–Z</MenuItem>
          <MenuItem value="newest">Newest first</MenuItem>
          <MenuItem value="oldest">Oldest first</MenuItem>
        </TextField>
      </Box>

      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      )}
      {!loading && error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {!loading && !error && filtered.length === 0 && (
        <Alert severity="info">No members match the current search / filter.</Alert>
      )}

      {!loading &&
        !error &&
        paged.map((m) => {
          const history = loansByMember[m.id] ?? [];
          const expanded = historyId === m.id;
          return (
            <Card key={m.id} variant="outlined" sx={{ mb: 1 }}>
              <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
                <Box sx={{ flexGrow: 1, minWidth: 200 }}>
                  <Typography variant="subtitle1">{m.fullName}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    {m.email}
                    {m.phone ? ` • ${m.phone}` : ''} • since{' '}
                    {m.membershipDate
                      ? new Date(m.membershipDate).toLocaleDateString()
                      : '—'}
                  </Typography>
                  {(activeCountByMember[m.id] ?? 0) > 0 && (
                    <Typography variant="caption" color="text.secondary">
                      {activeCountByMember[m.id]} active rental(s)
                    </Typography>
                  )}
                </Box>
                <Chip
                  label={m.isActive ? 'Active' : 'Inactive'}
                  size="small"
                  color={m.isActive ? 'success' : 'default'}
                />
                <Button
                  size="small"
                  variant="text"
                  endIcon={
                    <ExpandMore
                      sx={{
                        transform: expanded ? 'rotate(180deg)' : undefined,
                        transition: 'transform 0.2s',
                      }}
                    />
                  }
                  onClick={() => setHistoryId(expanded ? null : m.id)}
                >
                  History ({history.length})
                </Button>
                {canWrite && (
                  <>
                    <Button size="small" variant="outlined" startIcon={<Edit />} onClick={() => openEdit(m)}>
                      Edit
                    </Button>
                    <Button size="small" variant="outlined" onClick={() => toggleActive(m)}>
                      {m.isActive ? 'Deactivate' : 'Reactivate'}
                    </Button>
                  </>
                )}
              </CardContent>
              <Collapse in={expanded} timeout="auto" unmountOnExit>
                <Box sx={{ px: 2, pb: 2, display: 'flex', flexDirection: 'column', gap: 1 }}>
                  {history.length === 0 ? (
                    <Typography variant="body2" color="text.secondary">
                      No rentals yet for this member.
                    </Typography>
                  ) : (
                    history.map((l) => (
                      <Box
                        key={l.id}
                        sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}
                      >
                        <Typography variant="body2" sx={{ flexGrow: 1 }}>
                          {l.bookTitle ?? 'Unknown title'} • Copy {l.barcode ?? l.bookCopyId}
                        </Typography>
                        <Typography
                          variant="caption"
                          color={l.status === 'Overdue' ? 'error' : 'text.secondary'}
                        >
                          {dueLabel(l.dueDate, l.status, l.returnedAt)}
                        </Typography>
                        <Chip
                          label={l.status}
                          size="small"
                          color={
                            l.status === 'Active'
                              ? 'primary'
                              : l.status === 'Overdue'
                                ? 'error'
                                : 'default'
                          }
                        />
                      </Box>
                    ))
                  )}
                </Box>
              </Collapse>
            </Card>
          );
        })}

      {!loading && !error && total > 0 && (
        <PaginationBar
          page={page}
          totalPages={totalPages}
          total={total}
          pageSize={pageSize}
          onPageChange={setPage}
          onPageSizeChange={(n) => {
            setPageSize(n);
            setPage(1);
          }}
        />
      )}

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>{editing ? 'Edit member' : 'Add member'}</DialogTitle>
        <Box component="form" onSubmit={handleSave}>
          <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {formError && <Alert severity="error">{formError}</Alert>}
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="First name"
                value={firstName}
                onChange={onEdit('firstName', setFirstName)}
                required
                fullWidth
                error={!!fieldErrors.firstName}
                helperText={fieldErrors.firstName}
                slotProps={{ htmlInput: { maxLength: LIMITS.firstName } }}
              />
              <TextField
                label="Last name"
                value={lastName}
                onChange={onEdit('lastName', setLastName)}
                required
                fullWidth
                error={!!fieldErrors.lastName}
                helperText={fieldErrors.lastName}
                slotProps={{ htmlInput: { maxLength: LIMITS.lastName } }}
              />
            </Box>
            <TextField
              label="Email"
              type="email"
              value={email}
              onChange={onEdit('email', setEmail)}
              required
              fullWidth
              disabled={!!editing}
              error={!!fieldErrors.email}
              helperText={fieldErrors.email ?? (editing ? 'Email cannot be changed after creation.' : undefined)}
              slotProps={{ htmlInput: { maxLength: LIMITS.email } }}
            />
            <TextField
              label="Phone (optional)"
              value={phone}
              onChange={onEdit('phone', setPhone)}
              fullWidth
              error={!!fieldErrors.phone}
              helperText={fieldErrors.phone}
              slotProps={{ htmlInput: { maxLength: LIMITS.phone } }}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={saving}>
              {saving ? <CircularProgress size={20} /> : editing ? 'Save changes' : 'Save member'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Container>
  );
}
