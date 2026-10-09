/**
 * DashboardCharts.tsx — Three recharts visuals for the dashboard (category bars, monthly borrows, stock donut).
 * Junior-dev guide:
 * - Pure presentational: all data arrives via props, no fetch() here.
 * - useMemo aggregates books/loans/availability so recalculation only runs when inputs change.
 * - ResponsiveContainer makes each chart fill its Card.
 */
import { useMemo } from 'react';
import { Box, Card, CardContent, Grid, Typography, useTheme } from '@mui/material';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import type { Book, BookAvailability, Loan } from '../api/library';
import { truncate } from '../utils/libraryUtils';

// Lazy-loaded (see DashboardPage): recharts stays out of the initial bundle
// and is fetched only when the dashboard mounts. All data comes from props —
// no extra API calls, just aggregation of already-loaded lists.
/**
 * Props — Pre-loaded lists passed from DashboardPage (already fetched, just aggregated here).
 */
type Props = {
  books: Book[];
  loans: Loan[];
  availability: BookAvailability[];
};

/**
 * DashboardCharts — Renders category bar chart, 6-month borrow chart, and availability donut.
 * @param books Full book list for per-category counts.
 * @param loans Full loan list for monthly borrow buckets.
 * @param availability Per-title stock rows for the available vs on-loan split.
 */
export default function DashboardCharts({ books, loans, availability }: Props) {
  // useTheme: pull palette colors so charts follow light/dark mode automatically.
  const theme = useTheme();
  const tick = { fill: theme.palette.text.secondary, fontSize: 12 };
  const tooltipStyle = {
    backgroundColor: theme.palette.background.paper,
    border: `1px solid ${theme.palette.divider}`,
    borderRadius: 8,
    fontSize: 13,
  };

  // Titles per category (top 8). categoryName is denormalized on BookDto,
  // so no extra categories fetch is needed.
  const byCategory = useMemo(() => {
    const map = new Map<string, number>();
    books.forEach((b) => {
      const name = b.categoryName ?? 'Uncategorized';
      map.set(name, (map.get(name) ?? 0) + 1);
    });
    return [...map.entries()]
      .map(([name, count]) => ({ name: truncate(name, 14), count }))
      .sort((a, b) => b.count - a.count)
      .slice(0, 8);
  }, [books]);

  // Borrow events per month for the trailing 6 months (oldest -> newest).
  const byMonth = useMemo(() => {
    const buckets: { key: string; label: string; count: number }[] = [];
    const now = new Date();
    for (let i = 5; i >= 0; i--) {
      const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
      const key = `${d.getFullYear()}-${d.getMonth()}`;
      buckets.push({
        key,
        label: d.toLocaleDateString(undefined, { month: 'short' }),
        count: 0,
      });
    }
    const byKey = new Map(buckets.map((b) => [b.key, b]));
    loans.forEach((l) => {
      const d = new Date(l.borrowedAt);
      const hit = byKey.get(`${d.getFullYear()}-${d.getMonth()}`);
      if (hit) hit.count += 1;
    });
    return buckets;
  }, [loans]);

  // Physical-copy stock split for the donut.
  const stock = useMemo(() => {
    const total = availability.reduce((s, r) => s + r.totalCopies, 0);
    const available = availability.reduce((s, r) => s + r.availableCopies, 0);
    return [
      { name: 'Available', value: available },
      { name: 'On loan', value: Math.max(0, total - available) },
    ];
  }, [availability]);
  const hasStock = stock.some((s) => s.value > 0);

  return (
    <Grid container spacing={2} sx={{ mt: 1 }}>
      <Grid size={{ xs: 12, md: 6 }}>
        <Card variant="outlined">
          <CardContent>
            <Typography variant="subtitle1" sx={{ mb: 1 }}>
              Titles per category
            </Typography>
            {byCategory.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                No books yet.
              </Typography>
            ) : (
              <Box sx={{ height: 260 }}>
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={byCategory} margin={{ top: 8, right: 8, left: -16, bottom: 8 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke={theme.palette.divider} />
                    <XAxis dataKey="name" tick={tick} interval={0} />
                    <YAxis tick={tick} allowDecimals={false} />
                    <Tooltip contentStyle={tooltipStyle} />
                    <Bar dataKey="count" name="Titles" fill={theme.palette.primary.main} radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </Box>
            )}
          </CardContent>
        </Card>
      </Grid>

      <Grid size={{ xs: 12, md: 6 }}>
        <Card variant="outlined">
          <CardContent>
            <Typography variant="subtitle1" sx={{ mb: 1 }}>
              Borrows per month (last 6)
            </Typography>
            <Box sx={{ height: 260 }}>
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={byMonth} margin={{ top: 8, right: 8, left: -16, bottom: 8 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke={theme.palette.divider} />
                  <XAxis dataKey="label" tick={tick} />
                  <YAxis tick={tick} allowDecimals={false} />
                  <Tooltip contentStyle={tooltipStyle} />
                  <Bar dataKey="count" name="Borrows" fill={theme.palette.secondary.main} radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </Box>
          </CardContent>
        </Card>
      </Grid>

      <Grid size={{ xs: 12, md: 6 }}>
        <Card variant="outlined">
          <CardContent>
            <Typography variant="subtitle1" sx={{ mb: 1 }}>
              Copy availability
            </Typography>
            {!hasStock ? (
              <Typography variant="body2" color="text.secondary">
                No physical copies yet — add some from the Books tab.
              </Typography>
            ) : (
              <Box sx={{ height: 260 }}>
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie
                      data={stock}
                      dataKey="value"
                      nameKey="name"
                      innerRadius={60}
                      outerRadius={90}
                      paddingAngle={3}
                      label={({ name, value }) => `${name}: ${value}`}
                    >
                      <Cell fill={theme.palette.success.main} />
                      <Cell fill={theme.palette.warning.main} />
                    </Pie>
                    <Tooltip contentStyle={tooltipStyle} />
                    <Legend />
                  </PieChart>
                </ResponsiveContainer>
              </Box>
            )}
          </CardContent>
        </Card>
      </Grid>
    </Grid>
  );
}
