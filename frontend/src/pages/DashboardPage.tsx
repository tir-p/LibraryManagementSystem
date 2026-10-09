/**
 * DashboardPage.tsx — At-a-glance stats (titles, copies, members, rentals), charts, and overdue list.
 * Junior-dev guide:
 * - Props: go(page) navigates by setting App's tab state (tiny router).
 * - State: counts object + raw lists for charts + overdue loans with pagination.
 * - Data: Promise.all loads books/availability/members/loans; no dedicated stats endpoint.
 */
import { Suspense, lazy, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Container,
  Grid,
  Skeleton,
  Typography,
} from '@mui/material';
import {
  LibraryBooks,
  People,
  ReceiptLong,
  Warning,
  CheckCircle,
  Refresh,
} from '@mui/icons-material';
import { api, type Book, type BookAvailability, type Loan } from '../api/library';
import PaginationBar, { usePagination } from '../components/PaginationBar';
import { dueLabel } from '../utils/libraryUtils';

// Lazy: recharts is heavy (~100KB+), so it loads as a separate chunk only
// when the dashboard mounts — the login + list pages never download it.
const DashboardCharts = lazy(() => import('../components/DashboardCharts'));

// Dashboard: at-a-glance counts + overdue list. All data comes from the
// same endpoints the other pages use — no dedicated stats endpoint needed.
/** Page — Tab ids accepted by the go() navigator (mirrors App.tsx Page type). */
type Page = 'dashboard' | 'books' | 'rentals' | 'members' | 'authors' | 'categories';

/**
 * DashboardPage — Shows count cards, lazy charts, and paginated overdue rentals.
 * @param go Callback to switch App tabs when a count card is clicked.
 */
export default function DashboardPage({ go }: { go: (p: Page) => void }) {
  // Loading/error flags + counts object (titles, copies, members, active/overdue loans).
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [counts, setCounts] = useState({
    books: 0,
    totalCopies: 0,
    availableCopies: 0,
    members: 0,
    activeMembers: 0,
    activeLoans: 0,
    overdue: 0,
  });
  const [overdueLoans, setOverdueLoans] = useState<Loan[]>([]);
  // Raw lists for the charts (counts above are derived from the same data).
  const [chartBooks, setChartBooks] = useState<Book[]>([]);
  const [chartLoans, setChartLoans] = useState<Loan[]>([]);
  const [chartAvail, setChartAvail] = useState<BookAvailability[]>([]);

  /** API call: parallel fetch of books/availability/members/loans, then derive counts + chart lists. */
  const load = async () => {
    setLoading(true);
    setError('');
    try {
      const [books, avail, members, loans] = await Promise.all([
        api.getBooks(),
        api.getAvailability().catch(() => []),
        api.getMembers(),
        api.getLoans(),
      ]);
      const totalCopies = avail.reduce((s, r) => s + r.totalCopies, 0);
      const availableCopies = avail.reduce((s, r) => s + r.availableCopies, 0);
      const overdue = loans.filter((l: Loan) => l.status === 'Overdue');
      const active = loans.filter(
        (l: Loan) => l.status === 'Active' || l.status === 'Overdue',
      );
      setCounts({
        books: books.length,
        totalCopies,
        availableCopies,
        members: members.length,
        activeMembers: members.filter((m) => m.isActive).length,
        activeLoans: active.length,
        overdue: overdue.length,
      });
      setOverdueLoans(overdue);
      setChartBooks(books);
      setChartLoans(loans);
      setChartAvail(avail);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load dashboard.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const cards = useMemo(
    () => [
      { label: 'Titles', value: counts.books, icon: <LibraryBooks color="primary" />, action: () => go('books') },
      { label: 'Copies (available / total)', value: `${counts.availableCopies} / ${counts.totalCopies}`, icon: <CheckCircle color="success" />, action: () => go('books') },
      { label: 'Active members', value: `${counts.activeMembers} / ${counts.members}`, icon: <People color="primary" />, action: () => go('members') },
      { label: 'Active rentals', value: counts.activeLoans, icon: <ReceiptLong color="primary" />, action: () => go('rentals') },
      { label: 'Overdue', value: counts.overdue, icon: <Warning color="error" />, action: () => go('rentals') },
    ],
    [counts, go],
  );

  // Overdue list is usually short, so 5 per page keeps the dashboard compact.
  const {
    page: overduePage,
    setPage: setOverduePage,
    pageSize: overduePageSize,
    setPageSize: setOverduePageSize,
    total: overdueTotal,
    totalPages: overdueTotalPages,
    paged: overduePaged,
  } = usePagination(overdueLoans, 5);

  return (
    <Container maxWidth="md" sx={{ py: 3 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
        <Typography variant="h6" sx={{ flexGrow: 1 }}>
          Dashboard
        </Typography>
        <Button variant="outlined" size="small" onClick={load} startIcon={<Refresh />}>
          Refresh
        </Button>
      </Box>

      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      )}
      {!loading && error && <Alert severity="error">{error}</Alert>}

      {!loading && !error && (
        <>
          <Grid container spacing={2}>
            {cards.map((c) => (
              <Grid key={c.label} size={{ xs: 12, sm: 6, md: 4 }}>
                <Card variant="outlined" sx={{ cursor: 'pointer' }} onClick={c.action}>
                  <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                    {c.icon}
                    <Box>
                      <Typography variant="h5">{c.value}</Typography>
                      <Typography variant="body2" color="text.secondary">
                        {c.label}
                      </Typography>
                    </Box>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>

          <Suspense
            fallback={<Skeleton variant="rounded" height={280} sx={{ mt: 2 }} />}
          >
            <DashboardCharts books={chartBooks} loans={chartLoans} availability={chartAvail} />
          </Suspense>

          <Typography variant="h6" sx={{ mt: 3, mb: 1 }}>
            Needs attention — overdue ({counts.overdue})
          </Typography>
          {overdueLoans.length === 0 ? (
            <Alert severity="success">Nothing overdue. Nice!</Alert>
          ) : (
            <>
              {overduePaged.map((l) => (
                <Card key={l.id} variant="outlined" sx={{ mb: 1 }}>
                  <CardContent>
                    <Typography variant="subtitle1">
                      {l.bookTitle ?? 'Unknown title'} — {l.memberName ?? l.memberId}
                    </Typography>
                    <Typography variant="body2" color="error">
                      {dueLabel(l.dueDate, l.status, l.returnedAt)} — due{' '}
                      {new Date(l.dueDate).toLocaleDateString()} • Copy{' '}
                      {l.barcode ?? l.bookCopyId}
                    </Typography>
                  </CardContent>
                </Card>
              ))}
              <PaginationBar
                page={overduePage}
                totalPages={overdueTotalPages}
                total={overdueTotal}
                pageSize={overduePageSize}
                onPageChange={setOverduePage}
                onPageSizeChange={(n) => {
                  setOverduePageSize(n);
                  setOverduePage(1);
                }}
                pageSizeOptions={[5, 10, 20]}
              />
            </>
          )}
        </>
      )}
    </Container>
  );
}
