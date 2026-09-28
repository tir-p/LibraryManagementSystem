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
  MenuItem,
  TextField,
  Typography,
} from '@mui/material';
import { api, type Book, type BookCopy, type Loan, type Member } from '../api/library';
import { fetchBooks } from '../api/books';

// Rentals: borrow form (member + book + physical copy dropdowns) + loan list.
// Key domain rule: you borrow a COPY (barcode), not a title — one book has many copies.
export default function RentalsPage() {
  // Base lists for the form and rows below.
  const [loans, setLoans] = useState<Loan[]>([]);
  const [members, setMembers] = useState<Member[]>([]);
  const [books, setBooks] = useState<Book[]>([]);
  // Copies of the currently selected book (refreshed whenever bookId changes).
  const [copies, setCopies] = useState<BookCopy[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Borrow form state
  const [memberId, setMemberId] = useState('');
  const [bookId, setBookId] = useState('');
  const [copyId, setCopyId] = useState('');
  const [loanDays, setLoanDays] = useState(14);
  const [borrowing, setBorrowing] = useState(false);
  const [formMsg, setFormMsg] = useState<{ type: 'error' | 'success'; text: string } | null>(null);

  const loadBase = async () => {
    setLoading(true);
    setError('');
    try {
      const [l, m, b] = await Promise.all([api.getLoans(), api.getMembers(), fetchBooks()]);
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
  // No cleanup needed: a stale response just overwrites with older data, then the
  // latest response wins (acceptable here; abort logic would be overkill).
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
    if (Number(loanDays) <= 0) {
      setFormMsg({ type: 'error', text: 'Loan days must be at least 1.' });
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
    try {
      await api.returnLoan(id);
      setLoans(await api.getLoans());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Return failed.');
    }
  };

  const handleRenew = async (id: string) => {
    try {
      await api.renewLoan(id);
      setLoans(await api.getLoans());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Renew failed.');
    }
  };

  // Derived value (not state): recomputed from `copies` on every render.
  // Only available copies are selectable; loaned ones are hidden entirely.
  const availableCopies = copies.filter((c) => c.isAvailable);

  return (
    <Container maxWidth="md" sx={{ py: 3 }}>
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
                    so the dropdown lists active members only. */}
                {members
                  .filter((m) => m.isActive)
                  .map((m) => (
                    <MenuItem key={m.id} value={m.id}>
                      {m.fullName} ({m.email})
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
                slotProps={{ htmlInput: { min: 1 } }}
              />
            </Box>

            <Button type="submit" variant="contained" disabled={borrowing} sx={{ alignSelf: 'flex-start' }}>
              {borrowing ? <CircularProgress size={20} /> : 'Borrow book'}
            </Button>
          </Box>
        </CardContent>
      </Card>

      <Typography variant="h6" sx={{ mb: 2 }}>
        Rentals ({loans.length})
      </Typography>
      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
          <CircularProgress />
        </Box>
      )}
      {!loading && error && <Alert severity="error">{error}</Alert>}

      {!loading &&
        !error &&
        loans.map((loan) => (
          <Card key={loan.id} variant="outlined" sx={{ mb: 1 }}>
            <CardContent
              sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}
            >
              <Box sx={{ flexGrow: 1 }}>
                <Typography variant="subtitle1">
                  {loan.bookTitle ?? 'Unknown title'} — {loan.memberName ?? loan.memberId}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Copy {loan.barcode ?? loan.bookCopyId} • Due{' '}
                  {new Date(loan.dueDate).toLocaleDateString()} • Renewals:{' '}
                  {loan.renewalCount}
                </Typography>
              </Box>
              <Chip
                label={loan.status}
                size="small"
                color={
                  loan.status === 'Active'
                    ? 'primary'
                    : loan.status === 'Overdue'
                      ? 'error'
                      : 'default'
                }
              />
              {(loan.status === 'Active' || loan.status === 'Overdue') && (
                <>
                  {loan.status === 'Active' && (
                    <Button size="small" variant="outlined" onClick={() => handleRenew(loan.id)}>
                      Renew
                    </Button>
                  )}
                  <Button size="small" variant="contained" onClick={() => handleReturn(loan.id)}>
                    Return
                  </Button>
                </>
              )}
            </CardContent>
          </Card>
        ))}
    </Container>
  );
}
