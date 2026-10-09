/**
 * RentalsPage.tsx — Borrow form (member + book + copy dropdowns) plus filterable/sortable loan list.
 * Junior-dev guide:
 * - Key rule: you borrow a COPY (barcode), not a title — one book has many copies.
 * - Props: canWrite gates borrow/renew/return (Assistants see read-only list).
 * - State: loans/members/books/copies, list filters, borrow-form fields, action spinners.
 * - Effects: loadBase on mount; loadCopies whenever selected bookId changes.
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
  Container,
  MenuItem,
  TextField,
  Typography,
} from '@mui/material';
import { Download, Refresh } from '@mui/icons-material';
import { api, type Book, type BookCopy, type Loan, type Member } from '../api/library';
import PaginationBar, { usePagination } from '../components/PaginationBar';
import { dueLabel, exportCsv, overdueDays } from '../utils/libraryUtils';
import { MAX_ACTIVE_LOANS, loanDays as loanDaysValidator } from '../utils/validation';

// Rentals: borrow form (member + book + physical copy dropdowns) + loan list
// with status filter + text search. Overdue loans are flagged by the backend
// on read (LoanService.RefreshOverdueAsync), so status is never stale.
// Key domain rule: you borrow a COPY (barcode), not a title — one book has many copies.
/**
 * RentalsPage — Borrow workflow plus loan tracking with return/renew actions.
 * @param canWrite Librarian-only flag for borrow/renew/return buttons.
 */
export default function RentalsPage({ canWrite }: { canWrite: boolean }) {
  // Base lists for the form and rows below.
  const [loans, setLoans] = useState<Loan[]>([]);
  const [members, setMembers] = useState<Member[]>([]);
  const [books, setBooks] = useState<Book[]>([]);
  // Copies of the currently selected book (refreshed whenever bookId changes).
  const [copies, setCopies] = useState<BookCopy[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // List filters.
  const [query, setQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'Active' | 'Overdue' | 'Returned' | 'open'>('all');
  const [memberFilter, setMemberFilter] = useState('');
  const [sort, setSort] = useState<'newest' | 'due-asc' | 'overdue-first' | 'title'>('newest');

  // Borrow form state
  const [memberId, setMemberId] = useState('');
  const [bookId, setBookId] = useState('');
  const [copyId, setCopyId] = useState('');
  const [loanDays, setLoanDays] = useState(14);
  const [borrowing, setBorrowing] = useState(false);
  const [actingId, setActingId] = useState<string | null>(null);
  const [formMsg, setFormMsg] = useState<{ type: 'error' | 'success'; text: string } | null>(null);

  /** API call: load loans + members + books for the form dropdowns and the list below. */
  const loadBase = async () => {
    setLoading(true);
    setError('');
    try {
      const [l, m, b] = await Promise.all([api.getLoans(), api.getMembers(), api.getBooks()]);
      setLoans(l);
      setMembers(m);
      setBooks(b);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load rentals.');
    } finally {
      setLoading(false);
    }
  };

  // Runs once on mount: loans + members + books load together via Promise.all.
  useEffect(() => {
    loadBase();
  }, []);

  // Dependent fetch: [bookId] means "re-run whenever the selected book changes".
  // Auto-selects the first available copy so the form is usually one click.
  useEffect(() => {
    const loadCopies = async () => {
      if (!bookId) {
        setCopies([]);
        setCopyId('');
        return;
      }
      try {
        const data: BookCopy[] = await api.getCopies(bookId);
        setCopies(data);
        const firstAvailable = data.find((c) => c.isAvailable);
        setCopyId(firstAvailable ? firstAvailable.id : '');
      } catch {
        setCopies([]);
      }
    };
    loadCopies();
  }, [bookId]);

  // POST /loans/borrow, then refresh loans + that book's copies so the
  // loaned copy leaves the "available" dropdown immediately.
  const handleBorrow = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormMsg(null);
    if (!memberId || !copyId) {
      setFormMsg({ type: 'error', text: 'Select a member and an available copy.' });
      return;
    }
    const daysErr = loanDaysValidator(loanDays);
    if (daysErr) {
      setFormMsg({ type: 'error', text: daysErr });
      return;
    }
    const active = activeLoansByMember[memberId] ?? 0;
    if (active >= MAX_ACTIVE_LOANS) {
      setFormMsg({
        type: 'error',
        text: `This member already has ${active} active loans (max ${MAX_ACTIVE_LOANS}). Return one first.`,
      });
      return;
    }
    setBorrowing(true);
    try {
      await api.borrow(copyId, memberId, Number(loanDays));
      setFormMsg({ type: 'success', text: 'Rental created!' });
      const freshLoans: Loan[] = await api.getLoans();
      setLoans(freshLoans);
      // refresh copies so the just-loaned copy disappears from available
      if (bookId) {
        const freshCopies: BookCopy[] = await api.getCopies(bookId);
        setCopies(freshCopies);
        const firstAvailable = freshCopies.find((c) => c.isAvailable);
        setCopyId(firstAvailable ? firstAvailable.id : '');
      }
    } catch (err) {
      setFormMsg({ type: 'error', text: err instanceof Error ? err.message : 'Borrow failed.' });
    } finally {
      setBorrowing(false);
    }
  };

  const handleReturn = async (id: string) => {
    setActingId(id);
    try {
      await api.returnLoan(id);
      setLoans(await api.getLoans());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Return failed.');
    } finally {
      setActingId(null);
    }
  };

  const handleRenew = async (id: string) => {
    setActingId(id);
    try {
      await api.renewLoan(id);
      setLoans(await api.getLoans());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Renew failed. Max 2 renewals allowed.');
    } finally {
      setActingId(null);
    }
  };

  // Derived value (not state): recomputed from `copies` on every render.
  // Only available copies are selectable; loaned ones are hidden entirely.
  const availableCopies = copies.filter((c) => c.isAvailable);

  // Active-loan count per member, mirroring the backend borrow policy
  // (LoanService counts Status == Active only, cap = MAX_ACTIVE_LOANS).
  // Shown in the dropdown and enforced below so the failure isn't a surprise 400.
  const activeLoansByMember = useMemo(() => {
    const map: Record<string, number> = {};
    loans.forEach((l) => {
      if (l.status === 'Active') map[l.memberId] = (map[l.memberId] ?? 0) + 1;
    });
    return map;
  }, [loans]);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    const list = loans.filter((l) => {
      if (statusFilter === 'open' && !(l.status === 'Active' || l.status === 'Overdue')) return false;
      if (statusFilter !== 'all' && statusFilter !== 'open' && l.status !== statusFilter) return false;
      if (memberFilter && l.memberId !== memberFilter) return false;
      if (!q) return true;
      return (
        (l.bookTitle ?? '').toLowerCase().includes(q) ||
        (l.memberName ?? '').toLowerCase().includes(q) ||
        (l.barcode ?? '').toLowerCase().includes(q)
      );
    });
    // Sorting runs after filtering (client-side; the API has no sort params).
    switch (sort) {
      case 'due-asc':
        return list.sort((a, b) => +new Date(a.dueDate) - +new Date(b.dueDate));
      case 'overdue-first':
        return list.sort(
          (a, b) => overdueDays(b.dueDate, b.status) - overdueDays(a.dueDate, a.status),
        );
      case 'title':
        return list.sort((a, b) => (a.bookTitle ?? '').localeCompare(b.bookTitle ?? ''));
      default:
        return list.sort((a, b) => +new Date(b.borrowedAt) - +new Date(a.borrowedAt));
    }
  }, [loans, query, statusFilter, memberFilter, sort]);

  // Paginate the filtered + sorted loans. Page resets on any filter change.
  const {
    page,
    setPage,
    pageSize,
    setPageSize,
    total,
    totalPages,
    paged,
  } = usePagination(filtered, 8);

  useEffect(() => {
    setPage(1);
  }, [query, statusFilter, memberFilter, sort, setPage]);

  const openCount = loans.filter((l) => l.status === 'Active' || l.status === 'Overdue').length;

  const handleExport = () => {
    exportCsv(
      'rentals.csv',
      ['Book', 'Barcode', 'Member', 'Borrowed', 'Due', 'Returned', 'Status', 'Renewals'],
      filtered.map((l) => [
        l.bookTitle ?? '',
        l.barcode ?? '',
        l.memberName ?? '',
        new Date(l.borrowedAt).toLocaleDateString(),
        new Date(l.dueDate).toLocaleDateString(),
        l.returnedAt ? new Date(l.returnedAt).toLocaleDateString() : '',
        l.status,
        l.renewalCount,
      ]),
    );
  };

  const chipColor = (status: string) =>
    status === 'Active' ? 'primary' : status === 'Overdue' ? 'error' : 'default';

  return (
    <Container maxWidth="md" sx={{ py: 3 }}>
      {!canWrite && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          You are signed in as Assistant (read-only). Borrowing, renewing, and returning require a Librarian account.
        </Alert>
      )}

      {canWrite && (
        <>
          <Typography variant="h6" sx={{ mb: 2 }}>
            New rental
          </Typography>

          <Card variant="outlined" sx={{ mb: 3 }}>
            <CardContent>
              <Box component="form" onSubmit={handleBorrow} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                {formMsg && <Alert severity={formMsg.type}>{formMsg.text}</Alert>}
                <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                  <TextField
                    select
                    label="Member"
                    value={memberId}
                    onChange={(e) => setMemberId(e.target.value)}
                    required
                    sx={{ flex: 1, minWidth: 200 }}
                  >
                    {/* Inactive members can't borrow (backend rejects them too),
                        so the dropdown lists active members only, each with
                        their current active-loan count against the max. */}
                    {members
                      .filter((m) => m.isActive)
                      .map((m) => (
                        <MenuItem key={m.id} value={m.id}>
                          {m.fullName} ({m.email}) — {activeLoansByMember[m.id] ?? 0}/
                          {MAX_ACTIVE_LOANS} active
                        </MenuItem>
                      ))}
                  </TextField>
                  <TextField
                    select
                    label="Book"
                    value={bookId}
                    onChange={(e) => setBookId(e.target.value)}
                    required
                    sx={{ flex: 1, minWidth: 200 }}
                  >
                    {books.map((b) => (
                      <MenuItem key={b.id} value={b.id}>
                        {b.title}
                      </MenuItem>
                    ))}
                  </TextField>
                </Box>

                <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                  <TextField
                    select
                    label={availableCopies.length ? 'Available copy' : 'No available copies'}
                    value={copyId}
                    onChange={(e) => setCopyId(e.target.value)}
                    required
                    disabled={!availableCopies.length}
                    sx={{ flex: 1, minWidth: 200 }}
                    helperText={
                      bookId
                        ? `${availableCopies.length} of ${copies.length} copies available`
                        : 'Pick a book first'
                    }
                  >
                    {availableCopies.map((c) => (
                      <MenuItem key={c.id} value={c.id}>
                        {c.barcode} {c.shelfLocation ? `— Shelf ${c.shelfLocation}` : ''}
                      </MenuItem>
                    ))}
                  </TextField>
                  <TextField
                    label="Days"
                    type="number"
                    value={loanDays}
                    onChange={(e) => setLoanDays(Number(e.target.value))}
                    sx={{ width: 120 }}
                    slotProps={{ htmlInput: { min: 1, max: 365 } }}
                    helperText="1–365, default 14"
                  />
                </Box>

                <Button type="submit" variant="contained" disabled={borrowing} sx={{ alignSelf: 'flex-start' }}>
                  {borrowing ? <CircularProgress size={20} /> : 'Borrow book'}
                </Button>
              </Box>
            </CardContent>
          </Card>
        </>
      )}

      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2, flexWrap: 'wrap' }}>
        <Typography variant="h6" sx={{ flexGrow: 1 }}>
          Rentals ({filtered.length}/{loans.length}) — {openCount} open
        </Typography>
        <Button variant="outlined" size="small" onClick={loadBase} startIcon={<Refresh />}>
          Refresh
        </Button>
        <Button variant="outlined" size="small" onClick={handleExport} startIcon={<Download />} disabled={filtered.length === 0}>
          Export
        </Button>
      </Box>

      <Box sx={{ display: 'flex', gap: 1, mb: 2, flexWrap: 'wrap' }}>
        <TextField
          label="Search title, member, barcode…"
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
          <MenuItem value="open">Open (active + overdue)</MenuItem>
          <MenuItem value="Active">Active</MenuItem>
          <MenuItem value="Overdue">Overdue</MenuItem>
          <MenuItem value="Returned">Returned</MenuItem>
        </TextField>
        <TextField
          select
          label="Member"
          value={memberFilter}
          onChange={(e) => setMemberFilter(e.target.value)}
          size="small"
          sx={{ minWidth: 170 }}
        >
          <MenuItem value="">All members</MenuItem>
          {members.map((m) => (
            <MenuItem key={m.id} value={m.id}>
              {m.fullName}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          label="Sort"
          value={sort}
          onChange={(e) => setSort(e.target.value as typeof sort)}
          size="small"
          sx={{ minWidth: 150 }}
        >
          <MenuItem value="newest">Newest first</MenuItem>
          <MenuItem value="due-asc">Due soonest</MenuItem>
          <MenuItem value="overdue-first">Most overdue</MenuItem>
          <MenuItem value="title">Title A–Z</MenuItem>
        </TextField>
      </Box>

      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
          <CircularProgress />
        </Box>
      )}
      {!loading && error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {!loading && !error && filtered.length === 0 && (
        <Alert severity="info">No rentals match the current search / filters.</Alert>
      )}

      {!loading &&
        !error &&
        paged.map((loan) => (
          <Card key={loan.id} variant="outlined" sx={{ mb: 1 }}>
            <CardContent
              sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}
            >
              <Box sx={{ flexGrow: 1, minWidth: 220 }}>
                <Typography variant="subtitle1">
                  {loan.bookTitle ?? 'Unknown title'} — {loan.memberName ?? loan.memberId}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Copy {loan.barcode ?? loan.bookCopyId} • Borrowed{' '}
                  {new Date(loan.borrowedAt).toLocaleDateString()} • Renewals:{' '}
                  {loan.renewalCount}/2
                </Typography>
                <Typography
                  variant="body2"
                  color={loan.status === 'Overdue' ? 'error' : 'text.secondary'}
                  sx={{ fontWeight: loan.status === 'Overdue' ? 'bold' : undefined }}
                >
                  {dueLabel(loan.dueDate, loan.status, loan.returnedAt)}
                  {' • '}
                  due {new Date(loan.dueDate).toLocaleDateString()}
                </Typography>
              </Box>
              <Chip label={loan.status} size="small" color={chipColor(loan.status)} />
              {canWrite && (loan.status === 'Active' || loan.status === 'Overdue') && (
                <>
                  {loan.status === 'Active' && (
                    <Button
                      size="small"
                      variant="outlined"
                      onClick={() => handleRenew(loan.id)}
                      disabled={actingId === loan.id}
                    >
                      {actingId === loan.id ? <CircularProgress size={16} /> : 'Renew'}
                    </Button>
                  )}
                  <Button
                    size="small"
                    variant="contained"
                    onClick={() => handleReturn(loan.id)}
                    disabled={actingId === loan.id}
                  >
                    {actingId === loan.id ? <CircularProgress size={16} /> : 'Return'}
                  </Button>
                </>
              )}
            </CardContent>
          </Card>
        ))}

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
    </Container>
  );
}
