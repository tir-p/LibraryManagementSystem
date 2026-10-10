/**
 * AuthorsPage.tsx — CRUD UI for authors (search list + add/edit dialog + delete confirm).
 * Junior-dev guide:
 * - Props: canWrite=false hides mutation buttons for the Assistant role.
 * - State: authors list, loading/error flags, search query, dialog + form fields.
 * - useEffect([]) loads once on mount; useMemo filters by query; usePagination slices pages.
 */
import { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
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
import { api, type Author } from '../api/library';
import PaginationBar, { usePagination } from '../components/PaginationBar';
import {
  LIMITS,
  clearFieldError,
  dateOfBirth as dobValidator,
  optionalText,
  text,
  type FieldErrors,
} from '../utils/validation';

// Authors: full CRUD against /api/authors. Deleting an author that still
// has books fails on the backend (FK) — the error banner shows the message.
/**
 * AuthorsPage — Lists, searches, creates, edits, and deletes authors.
 * @param canWrite True for Librarians (shows Add/Edit/Delete); false for read-only Assistants.
 */
export default function AuthorsPage({ canWrite }: { canWrite: boolean }) {
  // Server list + page status: authors array, spinner flag, banner error text.
  const [authors, setAuthors] = useState<Author[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [query, setQuery] = useState('');

  // Dialog + form state: open flag, editing target (null = create), per-field inputs, save spinner + errors.
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Author | null>(null);
  const [name, setName] = useState('');
  const [biography, setBiography] = useState('');
  const [dateOfBirth, setDateOfBirth] = useState('');
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  // This is a helper that makes a TextField "controlled".
  // value={name} shows the state, onChange saves every keystroke back into state.
  // It also clears that field's red error as soon as you start fixing it.
  const onEdit =
    (field: string, setter: (v: string) => void) => (e: React.ChangeEvent<HTMLInputElement>) => {
      setter(e.target.value);
      clearFieldError(setFieldErrors, field);
    };

  // Which author are we sure we want to delete? null = no dialog open.
  // We store the whole object so the dialog can show its name.
  const [confirmDelete, setConfirmDelete] = useState<Author | null>(null);
  // Spinner just for the Delete button, so you can't double-click delete.
  const [deleting, setDeleting] = useState(false);

  /** API call: fetch all authors, showing spinner and error banner as needed. Reused by mount + Refresh + save/delete. */
  const load = async () => {
    setLoading(true);
    setError('');
    try {
      setAuthors(await api.getAuthors()); // GET /api/authors
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load authors.');
    } finally {
      setLoading(false); // always stop spinner
    }
  };

  // useEffect with [] = run once on mount (fetch-on-load pattern).
  // Empty [] means "no dependencies, so never re-run". This just loads the list when you open the page.
  useEffect(() => {
    load();
  }, []);

  // useMemo = "recompute only when [authors, query] change, otherwise reuse old result".
  // Without it, filtering would run on every render, even when typing in the dialog.
  // This is a derived list: we don't store it in useState, we calculate it from authors + query.
  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return authors; //If q is an empty string, return the complete list of authors without filtering.
    return authors.filter((a) => a.name.toLowerCase().includes(q));
  }, [authors, query]);

  // Take the filtered list and cut out just the current page (8 items).
  // paged = what we actually render below with .map().
  const {
    page,
    setPage,
    pageSize,
    setPageSize,
    total,
    totalPages,
    paged,
  } = usePagination(filtered, 8);

  // If you search while on page 3, the result might only have 1 page.
  // So jump back to page 1 whenever the search text changes, or you would see an empty page.
  useEffect(() => {
    setPage(1);
  }, [query, setPage]);

  // Event handler: open blank dialog for creating a new author.
  // editing = null means "we are creating, not editing".
  const openCreate = () => {
    setEditing(null);
    setName('');
    setBiography('');
    setDateOfBirth('');
    setFormError('');
    setFieldErrors({});
    setOpen(true);
  };

  // Fill the dialog with this author's current values, then open it.
  // editing = that author means "we are editing, not creating".
  const openEdit = (a: Author) => {
    setEditing(a);
    setName(a.name);
    setBiography(a.biography ?? '');
    setDateOfBirth(a.dateOfBirth ? a.dateOfBirth.slice(0, 10) : '');
    setFormError('');
    setFieldErrors({});
    setOpen(true);
  };

  // Runs when you press Save in the dialog.
  // e.preventDefault() stops the browser from reloading the whole page on form submit.
  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError('');
    // errs collects one message per bad field. If it stays empty, the form is valid.
    const errs: FieldErrors = {};
    // put() helper: only adds to errs when msg is not null (so valid fields add nothing).
    const put = (field: string, msg: string | null) => {
      if (msg) errs[field] = msg;
    };
    put('name', text(name, 'Name', LIMITS.authorName));
    put('biography', optionalText(biography, LIMITS.biography, 'Biography'));
    // Date of birth is only asked on create. On edit the field is hidden, so skip its check.
    if (!editing) put('dateOfBirth', dobValidator(dateOfBirth));
    setFieldErrors(errs);
    // If even one field failed, stop here and show red text under that field. No API call.
    if (Object.keys(errs).length > 0) return;
    setSaving(true); // show spinner on Save button and block double-clicks
    try {
      const body = {
        name: name.trim(),
        biography: biography.trim() || undefined,
        // Backend CreateAuthorRequest accepts DateOnly? — ISO date string works.
        // UpdateAuthorRequest has no date field, so date is only sent on create.
        ...(editing ? {} : dateOfBirth ? { dateOfBirth } : {}),
      };
      if (editing) await api.updateAuthor(editing.id, { name: body.name, biography: body.biography });
      else await api.createAuthor(body);
      setOpen(false); // close dialog first so UI feels fast
      // Re-fetch the full list so the new/edited row appears (backend gave it the real id).
      await load();
    } catch (err) {
      // Server errors (like duplicate name) land here and show in the red banner inside the dialog.
      setFormError(err instanceof Error ? err.message : 'Save failed.');
    } finally {
      setSaving(false); // always stop spinner, success or fail
    }
  };

  // Runs when you press Delete in the confirm dialog.
  // Guard: if dialog was closed already, there is nothing to delete.
  const handleDelete = async () => {
    if (!confirmDelete) return;
    setDeleting(true);
    try {
      await api.deleteAuthor(confirmDelete.id);
      setConfirmDelete(null); // close dialog
      await load(); // refresh list so deleted row disappears
    } catch (err) {
      // Example: author still has books -> backend refuses, we show its message in the page banner.
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
          Authors ({filtered.length}/{authors.length})
        </Typography>
        <Button variant="outlined" size="small" onClick={load} startIcon={<Refresh />}>
          Refresh
        </Button>
        {canWrite && (
          <Button variant="contained" size="small" onClick={openCreate} startIcon={<Add />}>
            Add Author
          </Button>
        )}
      </Box>

      {!canWrite && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          You are signed in as Assistant (read-only). Sign in as a Librarian to manage authors.
        </Alert>
      )}

      {/* Search box is "controlled": value always equals query state, onChange saves typing into query. */}
      <TextField
        label="Search authors"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        fullWidth
        size="small"
        sx={{ mb: 2 }}
        placeholder="Type a name…"
      />

      {/* Loading spinner: show only while load() is running. */}
      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      )}
      {/* Error banner: show only when not loading and error text is not empty. */}
      {!loading && error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {/* Empty state: list loaded fine but filter removed everything (or list is truly empty). */}
      {!loading && !error && filtered.length === 0 && (
        <Alert severity="info">No authors found. Click “Add Author” to create one.</Alert>
      )}

      {/* Main list: .map() turns each author into one Card. */}
      {/* key={a.id} helps React tell cards apart when the list changes (always needed in lists). */}
      {/* paged = only the 8 items for the current page, not the whole filtered list. */}
      {!loading &&
        !error &&
        paged.map((a) => (
          <Card key={a.id} variant="outlined" sx={{ mb: 1 }}>
            <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
              <Box sx={{ flexGrow: 1 }}>
                <Typography variant="subtitle1">{a.name}</Typography>
                {a.biography && (
                  <Typography variant="body2" color="text.secondary">
                    {a.biography}
                  </Typography>
                )}
                {a.dateOfBirth && (
                  <Typography variant="caption" color="text.secondary">
                    Born {new Date(a.dateOfBirth).toLocaleDateString()}
                  </Typography>
                )}
              </Box>
        {/* canWrite comes from App: true for Librarian, false for Assistant. */}
        {/* {canWrite && (...)} means "show this button only for Librarians, otherwise show nothing". */}
        {canWrite && (
                <>
                  <Button size="small" variant="outlined" startIcon={<Edit />} onClick={() => openEdit(a)}>
                    Edit
                  </Button>
                  <Button
                    size="small"
                    variant="outlined"
                    color="error"
                    startIcon={<Delete />}
                    onClick={() => setConfirmDelete(a)}
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

      {/* Add/Edit dialog: open=true shows it. Title and date field change with editing (null = create). */}
      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>{editing ? 'Edit author' : 'Add author'}</DialogTitle>
        {/* onSubmit={handleSave} runs our save function when you press Enter or click Save. */}
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
              slotProps={{ htmlInput: { maxLength: LIMITS.authorName } }}
            />
            <TextField
              label="Biography (optional)"
              value={biography}
              onChange={onEdit('biography', setBiography)}
              fullWidth
              multiline
              rows={3}
              error={!!fieldErrors.biography}
              helperText={fieldErrors.biography}
              slotProps={{ htmlInput: { maxLength: LIMITS.biography } }}
            />
            {!editing && (
              <TextField
                label="Date of birth (optional)"
                type="date"
                value={dateOfBirth}
                onChange={onEdit('dateOfBirth', setDateOfBirth)}
                fullWidth
                error={!!fieldErrors.dateOfBirth}
                helperText={fieldErrors.dateOfBirth}
                slotProps={{ inputLabel: { shrink: true } }}
              />
            )}
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={saving}>
              {saving ? <CircularProgress size={20} /> : 'Save'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      {/* Delete confirm: open={!!confirmDelete} turns the author object into true/false. null = closed. */}
      <Dialog open={!!confirmDelete} onClose={() => setConfirmDelete(null)} maxWidth="xs" fullWidth>
        <DialogTitle>Delete author?</DialogTitle>
        <DialogContent>
          <Typography variant="body2">
            Delete “{confirmDelete?.name}”? This fails if any book still references this
            author — reassign or delete those books first.
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
