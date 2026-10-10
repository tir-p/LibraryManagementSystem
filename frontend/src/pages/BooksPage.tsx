/**
 * BooksPage.tsx — Book catalog with search/filters/sort, add/edit/delete, copy management, CSV export.
 * Junior-dev guide:
 * - Props: canWrite gates Add/Edit/Delete/Copy buttons (Assistants are read-only).
 * - State: server lists (books/authors/categories/counts), filters, dialogs, form + validation errors.
 * - Effects: loadAll() on mount; setPage(1) on filter change. Filtering/sorting/pagination are client-side.
 */
import { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CardMedia,
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
import { Add, Delete, Download, Edit, LibraryBooks, Refresh, Visibility } from '@mui/icons-material';
import { api, type Author, type Book, type BookAvailability, type BookCopy, type Category } from '../api/library';
import PaginationBar, { usePagination } from '../components/PaginationBar';
import { exportCsv, truncate } from '../utils/libraryUtils';
import {
  LIMITS,
  barcode as barcodeValidator,
  clearFieldError,
  imageUrl,
  isbn as isbnValidator,
  optionalText,
  pages as pagesValidator,
  required,
  shelf as shelfValidator,
  text,
  year as yearValidator,
  type FieldErrors,
} from '../utils/validation';

// Books catalog: search + author/category/availability filters, add/edit/
// delete dialogs, and a detail dialog showing physical copies.
// CopyCount maps book.id -> counts so each card renders without its own fetch.
// Example: copyCounts["book123"] = { total: 3, available: 1 } means 1 of 3 copies can be borrowed.
/** CopyCount — Aggregated stock for one title (total vs currently borrowable). */
type CopyCount = { total: number; available: number };

// Starting values for the Add dialog. We reset form to this so old typing never leaks into the next Add.
const emptyForm = {
  title: '',
  isbn: '',
  authorId: '',
  categoryId: '',
  year: new Date().getFullYear(),
  pages: 200,
  language: 'English',
  publisher: '',
  description: '',
  coverImageUrl: '',
};

// canWrite = false for the Assistant role: hides every mutation button
// (Add/Edit/Delete/Copy). The backend independently rejects such calls
// with 403, so the UI gating is defense in depth.
/**
 * BooksPage — Main catalog UI with filtering, sorting, pagination, and copy handling.
 * @param canWrite Librarian-only flag that shows mutation buttons when true.
 */
export default function BooksPage({ canWrite }: { canWrite: boolean }) {
  // Server data loaded once on mount. books = titles, copyCounts = stock per title, authors/categories = dropdown options.
  const [books, setBooks] = useState<Book[]>([]);
  // copyCounts avoids N extra API calls: one availability call fills every card's "2 of 3 available" chip.
  const [copyCounts, setCopyCounts] = useState<Record<string, CopyCount>>({});
  const [authors, setAuthors] = useState<Author[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true); // big spinner while loadAll runs
  const [error, setError] = useState(''); // red page banner

  // Search + filters are client-side only; the API has no ?search params, so we filter the loaded array.
  // Empty string = "All" (no filtering on that field).
  const [query, setQuery] = useState('');
  const [authorFilter, setAuthorFilter] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [availFilter, setAvailFilter] = useState<'all' | 'available' | 'unavailable' | 'nocopies'>('all');
  const [sort, setSort] = useState<'title' | 'year-desc' | 'year-asc' | 'available'>('title');

  // Add / edit dialog state. `editing` null = create mode, object = edit mode.
  // form holds every TextField in one object (instead of 10 separate useStates like MembersPage).
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Book | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState(''); // server errors (like duplicate ISBN) show here
  // Per-field errors (client validation). formError stays for server failures.
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [copyMsg, setCopyMsg] = useState(''); // blue info banner after "+ Copy" succeeds/fails

  // Detail dialog: selected book + its physical copies (barcodes). null = dialog closed.
  const [detail, setDetail] = useState<Book | null>(null);
  const [detailCopies, setDetailCopies] = useState<BookCopy[]>([]);
  const [detailLoading, setDetailLoading] = useState(false); // spinner inside detail dialog
  const [newBarcode, setNewBarcode] = useState(''); // empty = auto-generate barcode
  const [newShelf, setNewShelf] = useState('');

  // Delete confirmation: which book did we click Delete on? null = dialog closed.
  const [confirmDelete, setConfirmDelete] = useState<Book | null>(null);
  const [deleting, setDeleting] = useState(false);

  // Loads everything in parallel (Promise.all) instead of one-after-the-other, so 4 GETs finish in ~1x time.
  // Availability comes from ONE /books/availability call (not one per book, which would be N slow calls).
  // try/catch/finally: error banner on failure, spinner always stops.
  /** API call: load books + authors + categories + availability in parallel; rebuilds copyCounts map. */
  const loadAll = async () => {
    setLoading(true);
    setError('');
    try {
      const [b, a, c, avail] = await Promise.all([
        api.getBooks(),
        api.getAuthors(),
        api.getCategories(),
        api.getAvailability().catch(() => [] as BookAvailability[]),
      ]);
      setBooks(b);
      setAuthors(a);
      setCategories(c);

      // Single availability call replaces N per-book copies fetches.
      // Turn array [{bookId, totalCopies, availableCopies}] into lookup {bookId: {total, available}}.
      const counts: Record<string, CopyCount> = {};
      (avail as BookAvailability[]).forEach((row) => {
        counts[row.bookId] = { total: row.totalCopies, available: row.availableCopies };
      });
      // Books missing from the response (e.g. just created) default to 0/0.
      // ??= means "only assign if key does not exist yet".
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
  // This is the fetch-on-open pattern you saw in AuthorsPage.
  useEffect(() => {
    loadAll();
  }, []);

  // Step 1: filter. Each if returns false = "hide this book".
  // Order matters: cheap dropdown checks first, slow text search last.
  // ?? '' turns null authorName into empty string so .toLowerCase() never crashes.
  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase(); // trim + lowercase so "  Clean " matches "clean"
    return books.filter((b) => {
      if (authorFilter && b.authorId !== authorFilter) return false;
      if (categoryFilter && b.categoryId !== categoryFilter) return false;
      const count = copyCounts[b.id];
      if (availFilter === 'available' && !(count && count.available > 0)) return false;
      if (availFilter === 'unavailable' && !(count && count.total > 0 && count.available === 0)) return false;
      if (availFilter === 'nocopies' && !(count && count.total === 0)) return false;
      if (!q) return true; // no search text = keep everything that passed dropdowns
      return (
        b.title.toLowerCase().includes(q) ||
        b.isbn.toLowerCase().includes(q) ||
        (b.authorName ?? '').toLowerCase().includes(q) ||
        (b.categoryName ?? '').toLowerCase().includes(q)
      );
    });
  }, [books, query, authorFilter, categoryFilter, availFilter, copyCounts]);

  // Step 2: sorting runs after filtering (client-side; the API has no sort params).
  // [...filtered] copies the array first because .sort() changes the array in place.
  const sorted = useMemo(() => {
    const arr = [...filtered];
    switch (sort) {
      case 'year-desc':
        return arr.sort((a, b) => b.publishedYear - a.publishedYear);
      case 'year-asc':
        return arr.sort((a, b) => a.publishedYear - b.publishedYear);
      case 'available':
        return arr.sort(
          (a, b) => (copyCounts[b.id]?.available ?? 0) - (copyCounts[a.id]?.available ?? 0),
        );
      default:
        return arr.sort((a, b) => a.title.localeCompare(b.title));
    }
  }, [filtered, sort, copyCounts]);

  // Client-side pagination over the filtered + sorted list. Resets to page 1
  // whenever a filter changes so you never sit on an empty page after narrowing.
  // paged = only the 8 cards for the current page (what .map() renders below).
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
  }, [query, authorFilter, categoryFilter, availFilter, sort, setPage]);

  // CSV export covers the whole sorted set (not just the visible page).
  // Event handler: export the current filtered+sorted list to books.csv in Downloads.
  const handleExport = () => {
    exportCsv(
      'books.csv',
      ['Title', 'ISBN', 'Author', 'Category', 'Year', 'Pages', 'Language', 'Publisher', 'TotalCopies', 'AvailableCopies'],
      sorted.map((b) => [
        b.title,
        b.isbn,
        b.authorName ?? '',
        b.categoryName ?? '',
        b.publishedYear,
        b.pages,
        b.language,
        b.publisher ?? '',
        copyCounts[b.id]?.total ?? 0,
        copyCounts[b.id]?.available ?? 0,
      ]),
    );
  };

  const openCreate = () => {
    setEditing(null);
    setForm(emptyForm);
    setFormError('');
    setFieldErrors({});
    setOpen(true);
  };

  const openEdit = (b: Book) => {
    setEditing(b);
    setForm({
      title: b.title,
      isbn: b.isbn,
      authorId: b.authorId,
      categoryId: b.categoryId,
      year: b.publishedYear,
      pages: b.pages,
      language: b.language || 'English',
      publisher: b.publisher ?? '',
      description: '',
      coverImageUrl: b.coverImageUrl ?? '',
    });
    // Description isn't in the list DTO? It is — but keep it simple: fetch full record.
    api.getBook(b.id).then((full) => {
      setForm((f) => ({ ...f, description: full.description ?? '' }));
    }).catch(() => undefined);
    setFormError('');
    setFieldErrors({});
    setOpen(true);
  };

  // Dialog submit: validate every field locally first (mirrors backend caps
  // and domain guards), then create (POST with author/category IDs) or update
  // (PUT of editable details — backend UpdateBookRequest has no author/category).
  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError('');

    const errs: FieldErrors = {};
    const put = (field: string, msg: string | null) => {
      if (msg) errs[field] = msg;
    };
    put('title', text(form.title, 'Title', LIMITS.bookTitle));
    // ISBN is immutable after creation (field is disabled when editing).
    if (!editing) {
      put('isbn', isbnValidator(form.isbn));
      put('authorId', required(form.authorId, 'Author'));
      put('categoryId', required(form.categoryId, 'Category'));
    }
    put('year', yearValidator(form.year));
    put('pages', pagesValidator(form.pages));
    put('language', text(form.language, 'Language', LIMITS.language));
    put('publisher', optionalText(form.publisher, LIMITS.publisher, 'Publisher'));
    put('description', optionalText(form.description, LIMITS.bookDescription, 'Description'));
    put('coverImageUrl', imageUrl(form.coverImageUrl));
    setFieldErrors(errs);
    if (Object.keys(errs).length > 0) return;

    setSaving(true);
    try {
      if (editing) {
        await api.updateBook(editing.id, {
          title: form.title.trim(),
          description: form.description.trim() || undefined,
          publisher: form.publisher.trim() || undefined,
          publishedYear: Number(form.year) || 0,
          pages: Number(form.pages) || 0,
          language: form.language.trim() || 'English',
          coverImageUrl: form.coverImageUrl.trim() || undefined,
        });
      } else {
        await api.createBook({
          title: form.title.trim(),
          isbn: form.isbn.trim(),
          authorId: form.authorId,
          categoryId: form.categoryId,
          publishedYear: Number(form.year) || 0,
          pages: Number(form.pages) || 0,
          description: form.description.trim() || undefined,
          publisher: form.publisher.trim() || undefined,
          language: form.language.trim() || 'English',
          coverImageUrl: form.coverImageUrl.trim() || undefined,
        });
      }
      setOpen(false);
      await loadAll();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Save failed.');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!confirmDelete) return;
    setDeleting(true);
    try {
      await api.deleteBook(confirmDelete.id);
      setConfirmDelete(null);
      await loadAll();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Delete failed.');
      setConfirmDelete(null);
    } finally {
      setDeleting(false);
    }
  };

  // Adds one physical (borrowable) copy. A title can have many copies, each with its own barcode.
  // Quick "+ Copy" button calls with no barcode -> we auto-generate BC-12345678. Detail dialog passes typed values.
  // Then patches just that book's counts so the chip updates without full reload.
  // setCopyCounts(prev => ...) uses the functional form: prev is the latest state, so we never overwrite with stale data.
  const handleAddCopy = async (bookId: string, barcode?: string, shelf?: string) => {
    setCopyMsg(''); // clear old blue banner
    // User-typed barcodes/shelves are validated locally (backend caps are
    // 50 chars; uniqueness is still enforced server-side). Auto-generated
    // barcodes from the quick "+ Copy" button always pass.
    // barcode !== undefined means "user typed something" (detail dialog). undefined means quick button.
    if (barcode !== undefined) {
      const err = barcodeValidator(barcode) ?? shelfValidator(shelf); // ?? = use shelf error only if barcode passed
      if (err) {
        setCopyMsg(err);
        return; // stop here, don't call backend with bad input
      }
    }
    try {
      // If no barcode given, make one from current time. .trim() removes accidental spaces.
      const bc = (barcode ?? `BC-${Date.now().toString().slice(-8)}`).trim();
      await api.addCopy(bookId, bc, shelf || 'A1'); // POST /copies, A1 = default shelf
      setCopyMsg(`Copy ${bc} added! It is now available for rental.`);
      // Refresh counts so the new copy shows immediately.
      const copies: BookCopy[] = await api.getCopies(bookId);
      setCopyCounts((prev) => ({
        ...prev, // keep all other books' counts, only replace this one bookId
        [bookId]: {
          total: copies.length,
          available: copies.filter((cp) => cp.isAvailable).length,
        },
      }));
      // If detail dialog is open on this same book, update its copy list too.
      if (detail && detail.id === bookId) setDetailCopies(copies);
    } catch (err) {
      setCopyMsg(err instanceof Error ? err.message : 'Could not add copy.');
    }
  };

  // Open detail dialog for one book, then fetch its copies.
  // We open first (setDetail) so dialog appears instantly, then fill copies when they arrive.
  const openDetail = async (b: Book) => {
    setDetail(b);
    setDetailLoading(true); // spinner inside dialog while copies load
    setNewBarcode(''); // clear Add-copy inputs from last time
    setNewShelf('');
    try {
      setDetailCopies(await api.getCopies(b.id)); // GET /books/{id}/copies
    } catch {
      setDetailCopies([]); // on error show "No copies yet" instead of crashing
    } finally {
      setDetailLoading(false);
    }
  };

  // Updating a field clears its validation error so fixed input
  // immediately un-marks the field without waiting for re-submit.
  // Example: set('title', 'Clean Code') updates form.title and clears fieldErrors.title.
  const set = (k: keyof typeof emptyForm, v: string | number) => {
    setForm((f) => ({ ...f, [k]: v }));
    clearFieldError(setFieldErrors, k);
  };

  return (
    <Container maxWidth="md" sx={{ py: 3 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2, flexWrap: 'wrap' }}>
        <LibraryBooks color="primary" />
        <Typography variant="h6" sx={{ flexGrow: 1 }}>
          Books ({filtered.length}/{books.length})
        </Typography>
        <Button variant="outlined" size="small" onClick={loadAll} startIcon={<Refresh />}>
          Refresh
        </Button>
        <Button variant="outlined" size="small" onClick={handleExport} startIcon={<Download />} disabled={sorted.length === 0}>
          Export
        </Button>
        {canWrite && (
          <Button variant="contained" size="small" onClick={openCreate} startIcon={<Add />}>
            Add Book
          </Button>
        )}
      </Box>

      {!canWrite && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          You are signed in as Assistant (read-only). Sign in as a Librarian to add, edit, or delete books.
        </Alert>
      )}

      <Box sx={{ display: 'flex', gap: 1, mb: 2, flexWrap: 'wrap' }}>
        <TextField
          label="Search title, ISBN, author…"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          size="small"
          sx={{ flex: 2, minWidth: 200 }}
        />
        <TextField select label="Author" value={authorFilter} onChange={(e) => setAuthorFilter(e.target.value)} size="small" sx={{ flex: 1, minWidth: 140 }}>
          <MenuItem value="">All authors</MenuItem>
          {authors.map((a) => (
            <MenuItem key={a.id} value={a.id}>{a.name}</MenuItem>
          ))}
        </TextField>
        <TextField select label="Category" value={categoryFilter} onChange={(e) => setCategoryFilter(e.target.value)} size="small" sx={{ flex: 1, minWidth: 140 }}>
          <MenuItem value="">All categories</MenuItem>
          {categories.map((c) => (
            <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>
          ))}
        </TextField>
        <TextField select label="Stock" value={availFilter} onChange={(e) => setAvailFilter(e.target.value as typeof availFilter)} size="small" sx={{ minWidth: 150 }}>
          <MenuItem value="all">All</MenuItem>
          <MenuItem value="available">Available</MenuItem>
          <MenuItem value="unavailable">All loaned out</MenuItem>
          <MenuItem value="nocopies">No copies yet</MenuItem>
        </TextField>
        <TextField select label="Sort" value={sort} onChange={(e) => setSort(e.target.value as typeof sort)} size="small" sx={{ minWidth: 150 }}>
          <MenuItem value="title">Title A–Z</MenuItem>
          <MenuItem value="year-desc">Newest first</MenuItem>
          <MenuItem value="year-asc">Oldest first</MenuItem>
          <MenuItem value="available">Most available</MenuItem>
        </TextField>
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

      {!loading && !error && filtered.length === 0 && (
        <Alert severity="info">
          {books.length === 0
            ? 'No books yet — click Add Book. You need at least 1 author + 1 category first (see Authors / Categories tabs).'
            : 'No books match the current search / filters.'}
        </Alert>
      )}

      {/* .map() turns the books array into one <Card> per book.
          key={book.id} lets React track which card is which on re-render.
          paged = current slice of `filtered` (see usePagination above). */}
      <Box
        sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, mt: 2 }}
      >
        {paged.map((book) => (
          <Card key={book.id} variant="outlined">
            {book.coverImageUrl ? (
              <CardMedia
                component="img"
                height="140"
                image={book.coverImageUrl}
                alt={book.title}
                sx={{ objectFit: 'cover' }}
              />
            ) : (
              <Box
                sx={{
                  height: 140,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  bgcolor: 'primary.main',
                  color: 'primary.contrastText',
                }}
              >
                <LibraryBooks sx={{ fontSize: 48, opacity: 0.8 }} />
              </Box>
            )}
            <CardContent>
              <Typography variant="h6">{book.title}</Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                {book.authorName ?? 'Unknown author'} • {book.publishedYear} • {book.pages} pages
              </Typography>
              {book.description && (
                <Typography
                  variant="body2"
                  color="text.secondary"
                  sx={{
                    mb: 1,
                    display: '-webkit-box',
                    WebkitLineClamp: 2,
                    WebkitBoxOrient: 'vertical',
                    overflow: 'hidden',
                  }}
                >
                  {truncate(book.description, 140)}
                </Typography>
              )}
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
              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                <Button size="small" variant="outlined" startIcon={<Visibility />} onClick={() => openDetail(book)}>
                  Details
                </Button>
                {canWrite && (
                  <>
                    <Button size="small" variant="outlined" startIcon={<Edit />} onClick={() => openEdit(book)}>
                      Edit
                    </Button>
                    <Button size="small" variant="outlined" color="error" startIcon={<Delete />} onClick={() => setConfirmDelete(book)}>
                      Delete
                    </Button>
                    <Button size="small" variant="text" onClick={() => handleAddCopy(book.id)}>
                      + Copy
                    </Button>
                  </>
                )}
              </Box>
            </CardContent>
          </Card>
        ))}
      </Box>

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
          pageSizeOptions={[6, 8, 12, 24, 48]}
        />
      )}

      {/* Add / Edit dialog */}
      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>{editing ? 'Edit book' : 'Add a new book'}</DialogTitle>
        <Box component="form" onSubmit={handleSave}>
          <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {formError && <Alert severity="error">{formError}</Alert>}
            <TextField
              label="Title"
              value={form.title}
              onChange={(e) => set('title', e.target.value)}
              required
              fullWidth
              error={!!fieldErrors.title}
              helperText={fieldErrors.title}
              slotProps={{ htmlInput: { maxLength: LIMITS.bookTitle } }}
            />
            <TextField
              label="ISBN (10 or 13 digits, no hyphens)"
              value={form.isbn}
              onChange={(e) => set('isbn', e.target.value)}
              required
              fullWidth
              disabled={!!editing}
              error={!!fieldErrors.isbn}
              helperText={fieldErrors.isbn ?? (editing ? 'ISBN cannot be changed after creation.' : undefined)}
              slotProps={{ htmlInput: { maxLength: LIMITS.isbn } }}
            />
            {!editing && (
              <>
                <TextField select label="Author" value={form.authorId} onChange={(e) => set('authorId', e.target.value)} required fullWidth error={!!fieldErrors.authorId} helperText={fieldErrors.authorId}>
                  {authors.map((a) => (
                    <MenuItem key={a.id} value={a.id}>{a.name}</MenuItem>
                  ))}
                </TextField>
                <TextField select label="Category" value={form.categoryId} onChange={(e) => set('categoryId', e.target.value)} required fullWidth error={!!fieldErrors.categoryId} helperText={fieldErrors.categoryId}>
                  {categories.map((c) => (
                    <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>
                  ))}
                </TextField>
              </>
            )}
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField label="Year" type="number" value={form.year} onChange={(e) => set('year', Number(e.target.value))} fullWidth error={!!fieldErrors.year} helperText={fieldErrors.year} />
              <TextField label="Pages" type="number" value={form.pages} onChange={(e) => set('pages', Number(e.target.value))} fullWidth error={!!fieldErrors.pages} helperText={fieldErrors.pages} />
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField label="Language" value={form.language} onChange={(e) => set('language', e.target.value)} fullWidth error={!!fieldErrors.language} helperText={fieldErrors.language} slotProps={{ htmlInput: { maxLength: LIMITS.language } }} />
              <TextField label="Publisher (optional)" value={form.publisher} onChange={(e) => set('publisher', e.target.value)} fullWidth error={!!fieldErrors.publisher} helperText={fieldErrors.publisher} slotProps={{ htmlInput: { maxLength: LIMITS.publisher } }} />
            </Box>
            <TextField label="Description (optional)" value={form.description} onChange={(e) => set('description', e.target.value)} fullWidth multiline rows={3} error={!!fieldErrors.description} helperText={fieldErrors.description} slotProps={{ htmlInput: { maxLength: LIMITS.bookDescription } }} />
            <TextField label="Cover image URL (optional)" value={form.coverImageUrl} onChange={(e) => set('coverImageUrl', e.target.value)} fullWidth error={!!fieldErrors.coverImageUrl} helperText={fieldErrors.coverImageUrl} slotProps={{ htmlInput: { maxLength: LIMITS.coverImageUrl } }} />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={saving}>
              {saving ? <CircularProgress size={20} /> : editing ? 'Save changes' : 'Save book'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      {/* Detail dialog with copies */}
      <Dialog open={!!detail} onClose={() => setDetail(null)} fullWidth maxWidth="sm">
        <DialogTitle>{detail?.title}</DialogTitle>
        <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
          {detail && (
            <>
              {detail.coverImageUrl && (
                <Box
                  component="img"
                  src={detail.coverImageUrl}
                  alt={detail.title}
                  sx={{ maxHeight: 220, objectFit: 'contain', alignSelf: 'center', borderRadius: 1 }}
                />
              )}
              <Typography variant="body2" color="text.secondary">
                {detail.authorName} • {detail.categoryName} • {detail.publishedYear} • {detail.pages} pages • {detail.language}
              </Typography>
              <Typography variant="body2">ISBN: {detail.isbn}</Typography>
              {detail.publisher && <Typography variant="body2">Publisher: {detail.publisher}</Typography>}
              {detail.description && <Typography variant="body2">{detail.description}</Typography>}
              <Typography variant="h6" sx={{ mt: 2 }}>
                Copies ({detailCopies.length})
              </Typography>
              {detailLoading && <CircularProgress size={24} />}
              {!detailLoading && detailCopies.length === 0 && (
                <Alert severity="info">No physical copies yet — add one below.</Alert>
              )}
              {!detailLoading &&
                detailCopies.map((c) => (
                  <Box key={c.id} sx={{ display: 'flex', gap: 1, alignItems: 'center', flexWrap: 'wrap' }}>
                    <Typography variant="body2" sx={{ flexGrow: 1 }}>
                      {c.barcode} {c.shelfLocation ? `— Shelf ${c.shelfLocation}` : ''}
                    </Typography>
                    <Chip label={c.status} size="small" color={c.isAvailable ? 'success' : 'default'} />
                  </Box>
                ))}
              {canWrite && (
                <Box sx={{ display: 'flex', gap: 1, mt: 2, flexWrap: 'wrap' }}>
                  <TextField label="New barcode" value={newBarcode} onChange={(e) => setNewBarcode(e.target.value)} size="small" placeholder="auto if empty" sx={{ flex: 1, minWidth: 160 }} slotProps={{ htmlInput: { maxLength: LIMITS.barcode } }} />
                  <TextField label="Shelf" value={newShelf} onChange={(e) => setNewShelf(e.target.value)} size="small" sx={{ width: 110 }} slotProps={{ htmlInput: { maxLength: LIMITS.shelf } }} />
                  <Button
                    variant="outlined"
                    size="small"
                    onClick={() => detail && handleAddCopy(detail.id, newBarcode || undefined, newShelf || undefined)}
                  >
                    Add copy
                  </Button>
                </Box>
              )}
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDetail(null)}>Close</Button>
        </DialogActions>
      </Dialog>

      {/* Delete confirmation */}
      <Dialog open={!!confirmDelete} onClose={() => setConfirmDelete(null)} maxWidth="xs" fullWidth>
        <DialogTitle>Delete book?</DialogTitle>
        <DialogContent>
          <Typography variant="body2">
            Delete “{confirmDelete?.title}”? Copies and loan history for this title
            may block deletion on the backend.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmDelete(null)}>Cancel</Button>
          <Button variant="contained" color="error" onClick={handleDelete} disabled={deleting}>
            {deleting ? <CircularProgress size={20} /> : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
}
