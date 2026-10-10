/**
 * CategoriesPage.tsx — CRUD UI for book categories (search list + add/edit dialog + delete confirm).
 * Junior-dev guide:
 * - Props: canWrite hides Add/Edit/Delete for read-only Assistants.
 * - State: categories list, loading/error, query, dialog fields, validation errors.
 * - Pattern mirrors AuthorsPage: load-on-mount, useMemo filter, usePagination slice.
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
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
  Typography,
} from '@mui/material';
import { Add, Delete, Edit, Refresh } from '@mui/icons-material';
import { api, type Category } from '../api/library';
import PaginationBar, { usePagination } from '../components/PaginationBar';
import {
  LIMITS,
  clearFieldError,
  optionalText,
  text,
  type FieldErrors,
} from '../utils/validation';

// Categories: full CRUD against /api/categories. Same delete caveat as
// authors — books referencing a category block its deletion (FK).
/**
 * CategoriesPage — Lists, searches, creates, edits, and deletes categories.
 * @param canWrite True for Librarians; false hides mutation buttons for Assistants.
 */
export default function CategoriesPage({ canWrite }: { canWrite: boolean }) {
  // Same pattern as AuthorsPage: this is the page's memory.
  // categories = list from backend, query = what you typed in search.
  // Changing them with set...() re-renders the screen.
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true); // true = show spinner
  const [error, setError] = useState(''); // non-empty = show red banner
  const [query, setQuery] = useState('');

  // Dialog memory: open = visible?, editing = null means create, object means edit.
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Category | null>(null);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [saving, setSaving] = useState(false); // spinner on Save button
  const [formError, setFormError] = useState(''); // red banner inside dialog (server errors)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({}); // red text under each bad field

  // Helper for controlled TextFields: saves typing into state + clears that field's red error.
  const onEdit =
    (field: string, setter: (v: string) => void) => (e: React.ChangeEvent<HTMLInputElement>) => {
      setter(e.target.value);
      clearFieldError(setFieldErrors, field);
    };

  // Which category did we click Delete on? null = confirm dialog closed.
  const [confirmDelete, setConfirmDelete] = useState<Category | null>(null);
  const [deleting, setDeleting] = useState(false); // spinner on Delete button

  /** API call: fetch all categories; reused by mount, Refresh button, and after save/delete. */
  // try/catch/finally: try backend, catch shows banner, finally always stops spinner.
  const load = async () => {
    setLoading(true);
    setError('');
    try {
      setCategories(await api.getCategories()); // GET /api/categories
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load categories.');
    } finally {
      setLoading(false); // always stop spinner
    }
  };

  // Run once when page opens. Empty [] = "no dependencies, so never re-run".
  useEffect(() => {
    load();
  }, []);

  // Derived list: filter categories by search text. Re-runs only when categories/query change.
  // If q is empty, return full list with no filtering.
  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return categories;
    return categories.filter(
      (c) =>
        c.name.toLowerCase().includes(q) ||
        (c.description ?? '').toLowerCase().includes(q),
    );
  }, [categories, query]);

  // Cut filtered list into pages of 8. paged = only the cards for the current page.
  const {
    page,
    setPage,
    pageSize,
    setPageSize,
    total,
    totalPages,
    paged,
  } = usePagination(filtered, 8);

  // If you search while on page 3, jump back to page 1 or you could sit on an empty page.
  useEffect(() => {
    setPage(1);
  }, [query, setPage]);

  // Open empty dialog for "Add". editing = null means create mode.
  const openCreate = () => {
    setEditing(null);
    setName('');
    setDescription('');
    setFormError('');
    setFieldErrors({});
    setOpen(true);
  };

  // Open dialog filled with this row's values for "Edit". editing = object means edit mode.
  const openEdit = (c: Category) => {
    setEditing(c);
    setName(c.name);
    setDescription(c.description ?? ''); // ?? '' turns null from backend into empty string for TextField
    setFormError('');
    setFieldErrors({});
    setOpen(true);
  };

  // Save button in dialog. preventDefault stops full page reload on form submit.
  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError('');
    // errs collects one message per bad field. put() only adds when message is not null.
    const errs: FieldErrors = {};
    const put = (field: string, msg: string | null) => {
      if (msg) errs[field] = msg;
    };
    // Name is unique server-side (duplicate -> 400 banner); length is checked here.
    put('name', text(name, 'Name', LIMITS.categoryName));
    put('description', optionalText(description, LIMITS.categoryDescription, 'Description'));
    setFieldErrors(errs);
    // If even one field failed, stop here. No API call happens.
    if (Object.keys(errs).length > 0) return;
    setSaving(true);
    try {
      const body = { name: name.trim(), description: description.trim() || undefined };
      if (editing) await api.updateCategory(editing.id, body); // PUT
      else await api.createCategory(body); // POST
      setOpen(false);
      await load(); // re-fetch so new/edited row appears (backend owns the real id)
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Save failed.');
    } finally {
      setSaving(false); // always stop spinner
    }
  };

  // Delete button in confirm dialog. Refreshes list so deleted row disappears.
  const handleDelete = async () => {
    if (!confirmDelete) return; // dialog already closed, nothing to do
    setDeleting(true);
    try {
      await api.deleteCategory(confirmDelete.id);
      setConfirmDelete(null);
      await load();
    } catch (err) {
      // Example: books still use this category -> backend refuses, show message in page banner.
      setError(err instanceof Error ? err.message : 'Delete failed.');
      setConfirmDelete(null);
    } finally {
      setDeleting(false);
    }
  };

  return (
    <Container maxWidth="md" sx={{ py: 3 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2, flexWrap: 'wrap' }}>
        <Typography variant="h6" sx={{ flexGrow: 1 }}>
          Categories ({filtered.length}/{categories.length})
        </Typography>
        <Button variant="outlined" size="small" onClick={load} startIcon={<Refresh />}>
          Refresh
        </Button>
        {canWrite && (
          <Button variant="contained" size="small" onClick={openCreate} startIcon={<Add />}>
            Add Category
          </Button>
        )}
      </Box>

      {!canWrite && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          You are signed in as Assistant (read-only). Sign in as a Librarian to manage categories.
        </Alert>
      )}

      <TextField
        label="Search categories"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        fullWidth
        size="small"
        sx={{ mb: 2 }}
        placeholder="Type a name or description…"
      />

      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      )}
      {!loading && error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {!loading && !error && filtered.length === 0 && (
        <Alert severity="info">No categories found. Click “Add Category” to create one.</Alert>
      )}

      {!loading &&
        !error &&
        paged.map((c) => (
          <Card key={c.id} variant="outlined" sx={{ mb: 1 }}>
            <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
              <Box sx={{ flexGrow: 1 }}>
                <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
                  <Typography variant="subtitle1">{c.name}</Typography>
                  <Chip label="Category" size="small" variant="outlined" />
                </Box>
                {c.description && (
                  <Typography variant="body2" color="text.secondary">
                    {c.description}
                  </Typography>
                )}
              </Box>
              {canWrite && (
                <>
                  <Button size="small" variant="outlined" startIcon={<Edit />} onClick={() => openEdit(c)}>
                    Edit
                  </Button>
                  <Button
                    size="small"
                    variant="outlined"
                    color="error"
                    startIcon={<Delete />}
                    onClick={() => setConfirmDelete(c)}
                  >
                    Delete
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

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>{editing ? 'Edit category' : 'Add category'}</DialogTitle>
        <Box component="form" onSubmit={handleSave}>
          <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {formError && <Alert severity="error">{formError}</Alert>}
            <TextField
              label="Name"
              value={name}
              onChange={onEdit('name', setName)}
              required
              fullWidth
              autoFocus
              error={!!fieldErrors.name}
              helperText={fieldErrors.name}
              slotProps={{ htmlInput: { maxLength: LIMITS.categoryName } }}
            />
            <TextField
              label="Description (optional)"
              value={description}
              onChange={onEdit('description', setDescription)}
              fullWidth
              multiline
              rows={3}
              error={!!fieldErrors.description}
              helperText={fieldErrors.description}
              slotProps={{ htmlInput: { maxLength: LIMITS.categoryDescription } }}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={saving}>
              {saving ? <CircularProgress size={20} /> : 'Save'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      <Dialog open={!!confirmDelete} onClose={() => setConfirmDelete(null)} maxWidth="xs" fullWidth>
        <DialogTitle>Delete category?</DialogTitle>
        <DialogContent>
          <Typography variant="body2">
            Delete “{confirmDelete?.name}”? This fails if any book still uses this
            category — reassign or delete those books first.
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
