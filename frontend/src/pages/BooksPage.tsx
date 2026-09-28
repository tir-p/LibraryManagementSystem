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
  MenuItem,
  TextField,
  Typography,
} from '@mui/material';
import { Add, LibraryBooks, Refresh } from '@mui/icons-material';
import { api, type Author, type Book, type BookAvailability, type BookCopy, type Category } from '../api/library';
import { fetchBooks } from '../api/books';

// Books catalog: lists titles with copy-availability chips, plus an add-book dialog.
// CopyCount maps book.id -> counts so each card can render without its own fetch.
type CopyCount = { total: number; available: number };

export default function BooksPage() {
  // Server data (loaded once on mount) + page status flags.
  const [books, setBooks] = useState<Book[]>([]);
  const [copyCounts, setCopyCounts] = useState<Record<string, CopyCount>>({});
  const [authors, setAuthors] = useState<Author[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Add-book dialog state
  const [open, setOpen] = useState(false);
  const [title, setTitle] = useState('');
  const [isbn, setIsbn] = useState('');
  const [authorId, setAuthorId] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [year, setYear] = useState(new Date().getFullYear());
  const [pages, setPages] = useState(200);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');
  const [copyMsg, setCopyMsg] = useState('');

  // Loads everything in parallel (Promise.all) instead of sequentially.
  // Availability comes from ONE /books/availability call (not one per book).
  // try/catch/finally: error banner on failure, spinner always stops.
  const loadAll = async () => {
    setLoading(true);
    setError('');
    try {
      const [b, a, c, avail] = await Promise.all([
        fetchBooks(),
        api.getAuthors(),
        api.getCategories(),
        api.getAvailability().catch(() => [] as BookAvailability[]),
      ]);
      setBooks(b);
      setAuthors(a);
      setCategories(c);

      // Single availability call replaces N per-book copies fetches.
      const counts: Record<string, CopyCount> = {};
      (avail as BookAvailability[]).forEach((row) => {
        counts[row.bookId] = { total: row.totalCopies, available: row.availableCopies };
      });
      // Books missing from the response (e.g. just created) default to 0/0.
      b.forEach((book: Book) => {
        counts[book.id] ??= { total: 0, available: 0 };
      });
      setCopyCounts(counts);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load data.');
    } finally {
      setLoading(false);
    }
  };

  // useEffect with [] runs once after first render = "on page load".
  // (StrictMode double-invokes it in dev; the second call just reloads same data.)
  useEffect(() => {
    loadAll();
  }, []);

  // Dialog submit: validate locally (ISBN max 13 = DB column limit), POST,
  // close + clear the form, then reload so the new book appears.
  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError('');
    if (!title || !isbn || !authorId || !categoryId) {
      setFormError('Title, ISBN, author and category are required.');
      return;
    }
    if (isbn.length > 13) {
      setFormError('ISBN must be max 13 characters, no hyphens (e.g. 9780132350884).');
      return;
    }
    setSaving(true);
    try {
      await api.createBook({
        title,
        isbn,
        authorId,
        categoryId,
        publishedYear: Number(year),
        pages: Number(pages),
      });
      setOpen(false);
      setTitle('');
      setIsbn('');
      setAuthorId('');
      setCategoryId('');
      await loadAll();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Create failed.');
    } finally {
      setSaving(false);
    }
  };

  // Adds one physical (borrowable) copy with an auto-generated barcode,
  // then patches just that book's counts so the chip updates without full reload.
  // setCopyCounts(prev => ...) uses the functional form to avoid stale state.
  const handleAddCopy = async (bookId: string) => {
    setCopyMsg('');
    try {
      const barcode = `BC-${Date.now().toString().slice(-8)}`;
      await api.addCopy(bookId, barcode, 'A1');
      setCopyMsg(`Copy ${barcode} added! It is now available for rental.`);
      // Refresh counts so the new copy shows immediately.
      const copies: BookCopy[] = await api.getCopies(bookId);
      setCopyCounts((prev) => ({
        ...prev,
        [bookId]: {
          total: copies.length,
          available: copies.filter((cp) => cp.isAvailable).length,
        },
      }));
    } catch (err) {
      setCopyMsg(err instanceof Error ? err.message : 'Could not add copy.');
    }
  };

  return (
    <Container maxWidth="md" sx={{ py: 3 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
        <LibraryBooks color="primary" />
        <Typography variant="h6" sx={{ flexGrow: 1 }}>
          Books ({books.length})
        </Typography>
        <Button variant="outlined" size="small" onClick={loadAll} startIcon={<Refresh />}>
          Refresh
        </Button>
        <Button variant="contained" size="small" onClick={() => setOpen(true)} startIcon={<Add />}>
          Add Book
        </Button>
      </Box>

      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      )}
      {!loading && error && <Alert severity="error">{error}</Alert>}
      {copyMsg && (
        <Alert severity="info" sx={{ mt: 2 }} onClose={() => setCopyMsg('')}>
          {copyMsg}
        </Alert>
      )}

      {!loading && !error && books.length === 0 && (
        <Alert severity="info">
          No books yet — click Add Book. You need at least 1 author + 1 category in the
          backend first (create via /scalar/v1 if lists are empty).
        </Alert>
      )}

      {/* .map() turns the books array into one <Card> per book.
          key={book.id} lets React track which card is which on re-render. */}
      <Box
        sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, mt: 2 }}
      >
        {books.map((book) => (
          <Card key={book.id} variant="outlined">
            <CardContent>
              <Typography variant="h6">{book.title}</Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                {book.authorName ?? 'Unknown author'} • {book.publishedYear} • {book.pages} pages
              </Typography>
              <Box sx={{ display: 'flex', gap: 1, mb: 1, flexWrap: 'wrap' }}>
                {book.categoryName && <Chip label={book.categoryName} size="small" />}
                <Chip label={book.language} size="small" variant="outlined" />
                {(() => {
                  const count = copyCounts[book.id];
                  if (!count) return null;
                  const label =
                    count.total === 0
                      ? 'No copies'
                      : `${count.available} of ${count.total} available`;
                  return (
                    <Chip
                      label={label}
                      size="small"
                      color={count.available > 0 ? 'success' : 'default'}
                    />
                  );
                })()}
              </Box>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                ISBN: {book.isbn}
              </Typography>
              <Button size="small" variant="outlined" onClick={() => handleAddCopy(book.id)}>
                + Add physical copy
              </Button>
            </CardContent>
          </Card>
        ))}
      </Box>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Add a new book</DialogTitle>
        <Box component="form" onSubmit={handleCreate}>
          <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {formError && <Alert severity="error">{formError}</Alert>}
            <TextField
              label="Title"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              required
              fullWidth
            />
            <TextField
              label="ISBN (13 chars, no hyphens)"
              value={isbn}
              onChange={(e) => setIsbn(e.target.value)}
              required
              fullWidth
              slotProps={{ htmlInput: { maxLength: 13 } }}
            />
            <TextField
              select
              label="Author"
              value={authorId}
              onChange={(e) => setAuthorId(e.target.value)}
              required
              fullWidth
            >
              {authors.map((a) => (
                <MenuItem key={a.id} value={a.id}>
                  {a.name}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              select
              label="Category"
              value={categoryId}
              onChange={(e) => setCategoryId(e.target.value)}
              required
              fullWidth
            >
              {categories.map((c) => (
                <MenuItem key={c.id} value={c.id}>
                  {c.name}
                </MenuItem>
              ))}
            </TextField>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Year"
                type="number"
                value={year}
                onChange={(e) => setYear(Number(e.target.value))}
                fullWidth
              />
              <TextField
                label="Pages"
                type="number"
                value={pages}
                onChange={(e) => setPages(Number(e.target.value))}
                fullWidth
              />
            </Box>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={saving}>
              {saving ? <CircularProgress size={20} /> : 'Save book'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Container>
  );
}
