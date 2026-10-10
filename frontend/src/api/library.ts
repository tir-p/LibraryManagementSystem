/**
 * library.ts — Central API client for the Library frontend.
 * Junior-dev guide:
 * - All fetch() calls live here so pages never hardcode URLs.
 * - Mirrors backend C# DTOs in camelCase (ASP.NET serializes that way).
 * - Handles JWT auth (localStorage) and shared error parsing.
 */
// All backend calls live here so pages never hardcode URLs.
// Backend runs on http://localhost:5008 in dev (see backend launchSettings.json).
// Types below mirror the C# DTO records (e.g. BookDto) but in camelCase,
// because ASP.NET serializes JSON property names as camelCase by default.
// Pages just do: await api.getBooks() — they never call fetch() directly.
/** Base URL for the backend API. Reads VITE_API_URL env var, falls back to localhost:5008 for local dev. */
// import.meta.env.VITE_API_URL lets production point at a real server without changing code.
// || 'http://localhost:5008' means "use env value, or localhost if env is missing".
export const API_BASE_URL =
  (import.meta as unknown as { env?: Record<string, string | undefined> }).env
    ?.VITE_API_URL || 'http://localhost:5008';

// Staff auth: the backend mints a JWT on POST /api/auth/login carrying the
// role claim ("Librarian" = full access, "Assistant" = read-only GETs).
// The token is stored in localStorage and sent as `Authorization: Bearer …`
// on every request by req() below.
// Think of AuthUser as your "stamped wristband": email says who, role says what you may do, token proves it.
/**
 * AuthUser — Logged-in staff session shape returned by POST /api/auth/login.
 * @property email Staff email.
 * @property role Either "Librarian" (full write) or "Assistant" (read-only).
 * @property token JWT bearer token sent on every request.
 * @property expiresAt ISO date when the token expires.
 */
export type AuthUser = {
  email: string;
  role: string;
  token: string;
  expiresAt: string;
};

/** localStorage key where the AuthUser session is persisted. */
// localStorage = tiny key-value box in the browser that survives refresh.
// We store the whole AuthUser as a JSON string under this one key.
const AUTH_KEY = 'libraryAuth';

/**
 * Read the saved auth session from localStorage.
 * @returns The stored AuthUser, or null if missing/corrupt (safe to call on load).
 */
// Called once in App useState(() => getStoredAuth()) so refresh keeps you logged in.
// try/catch: if someone hand-edited localStorage to invalid JSON, return null instead of crashing.
export function getStoredAuth(): AuthUser | null {
  try {
    const raw = localStorage.getItem(AUTH_KEY); // raw is string or null
    // raw ? ... : null means "if nothing saved, return null". JSON.parse turns string back into object.
    return raw ? (JSON.parse(raw) as AuthUser) : null;
  } catch {
    return null;
  }
}

/**
 * Persist the auth session to localStorage after a successful login.
 * @param user AuthUser returned by the login endpoint.
 */
// JSON.stringify turns the object into a string because localStorage only holds strings.
export function setStoredAuth(user: AuthUser) {
  localStorage.setItem(AUTH_KEY, JSON.stringify(user));
}

/**
 * Clear the auth session on logout or on 401 Session-expired.
 * Also removes the legacy pre-auth email key.
 */
// After this, getStoredAuth() returns null, so App falls back to <LoginPage/>.
export function clearStoredAuth() {
  localStorage.removeItem(AUTH_KEY);
  // Drop the legacy pre-auth key so old sessions can't linger.
  localStorage.removeItem('libraryUserEmail');
}

// Role names mirror backend UserRoles. Single source so a rename touches one line.
// user?.role means "if user is null, give undefined instead of crashing". Only exact 'Librarian' passes.
/** Role string that grants write access (mirrors backend UserRoles.Librarian). */
export const LIBRARIAN_ROLE = 'Librarian';

// Central place to decide write access: only Librarians mutate data.
// Pages use it as: const canWrite = isLibrarian(user) -> {canWrite && <Button>Add</Button>}.
/**
 * Check if a user can mutate data (create/edit/delete).
 * @param user Current session or null when logged out.
 * @returns True only for the Librarian role; Assistants are read-only.
 */
export const isLibrarian = (user: AuthUser | null) => user?.role === LIBRARIAN_ROLE;

// Shared response checker: fetch() only rejects on network errors, NOT on
// HTTP 400/404/500. So we must inspect res.ok ourselves and throw with the
// backend's message (GlobalExceptionHandler returns { message }).
// res.ok = true for 200-299, false for 400/401/403/404/500.
// Every page uses try/catch around api calls, so throwing here shows the banner there.
/** Throw a friendly Error when fetch() returns non-2xx; otherwise parse JSON. Keeps error handling in one place. */
async function handleRes(res: Response) {
  if (!res.ok) {
    // 401 = missing/expired token -> drop it so the app falls back to login.
    if (res.status === 401) clearStoredAuth();
    // Backend GlobalExceptionHandler returns { message }, ProblemDetails uses title/detail.
    // 403 has an empty body (auth middleware), so give it a helpful default.
    // We try res.json() to get the real message, but keep our default if body is empty/not JSON.
    let msg =
      res.status === 401
        ? 'Session expired. Please log in again.'
        : res.status === 403
          ? 'Forbidden: your role is read-only and cannot perform this action.'
          : `Request failed (status ${res.status})`;
    try {
      const data = await res.json();
      msg = data.message || data.detail || data.title || msg;
    } catch {
      /* ignore parse error */
    }
    throw new Error(msg); // pages catch this: catch (err) { setError(err.message) }
  }
  if (res.status === 204) return null; // 204 No Content (deactivate/reactivate) has no body to parse.
  return res.json(); // 200 with JSON body: turn it into a JS object/array for the page
}

/** Low-level fetch wrapper: prefixes API_BASE_URL, injects JWT + JSON headers, then delegates to handleRes. */
// path is like '/api/books'. ...init spreads method/body from the caller.
// ...(token ? { Authorization: ... } : {}) means "add header only when logged in".
// ...(init?.headers ?? {}) lets a caller add extra headers without losing ours.
async function req(path: string, init?: RequestInit) {
  const token = getStoredAuth()?.token;
  const res = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init?.headers ?? {}),
    },
  });
  return handleRes(res);
}

// DTO = Data Transfer Object: the exact JSON shape the API sends/returns.
// `?` means optional, `| null` because C# nullable strings arrive as null.
// Example: biography?: string | null means backend may send no key, a string, or null — all are ok.
/** Author DTO — mirrors backend AuthorDto (id, name, optional bio + birth date). */
export type Author = {
  id: string;
  name: string;
  biography?: string | null;
  dateOfBirth?: string | null;
};

/** Category DTO — book grouping (e.g. Fiction). Description is optional. */
export type Category = {
  id: string;
  name: string;
  description?: string | null;
};

// Denormalized = backend already includes authorName/categoryName strings,
// so the card can show them without a second fetch per book. authorId is the real link.
/** Book DTO — catalog title with denormalized authorName/categoryName for display (avoids extra lookups). */
export type Book = {
  id: string;
  title: string;
  isbn: string;
  description?: string | null;
  publisher?: string | null;
  language: string;
  publishedYear: number;
  pages: number;
  coverImageUrl?: string | null;
  authorId: string;
  authorName?: string | null;
  categoryId: string;
  categoryName?: string | null;
};

// fullName is built by backend (firstName + lastName) so cards show one string.
// isActive=false hides the member from the Rentals borrow dropdown.
/** Member DTO — library borrower with active flag (inactive members cannot borrow). */
export type Member = {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phone?: string | null;
  membershipDate: string;
  isActive: boolean;
};

// One row per title: { bookId, totalCopies: 3, availableCopies: 1 }. BooksPage turns this into copyCounts map.
/** BookAvailability DTO — aggregated stock per title from GET /api/books/availability. */
export type BookAvailability = {
  bookId: string;
  totalCopies: number;
  availableCopies: number;
};

// One physical book you can hold: barcode is the sticker, shelfLocation like "A1". isAvailable=false means on loan.
/** BookCopy DTO — one physical borrowable copy (barcode + shelf). isAvailable = not currently on loan. */
export type BookCopy = {
  id: string;
  bookId: string;
  bookTitle?: string | null;
  barcode: string;
  shelfLocation?: string | null;
  status: string;
  isAvailable: boolean;
};

// A borrow of one copy by one member. Backend sets status Overdue on read when past dueDate and not returned.
// renewalCount 0..2: backend allows max 2 renewals.
/** Loan DTO — a borrow of one copy by one member; status is Active | Overdue | Returned. */
export type Loan = {
  id: string;
  bookCopyId: string;
  barcode?: string | null;
  bookTitle?: string | null;
  memberId: string;
  memberName?: string | null;
  borrowedAt: string;
  dueDate: string;
  returnedAt?: string | null;
  status: string;
  renewalCount: number;
};

// What we send to create a book. authorId/categoryId are dropdown ids. ISBN can never change after this (backend rule).
/** Request body for POST /api/books — authorId/categoryId link the title; ISBN is set once at creation. */
export type CreateBookBody = {
  title: string;
  isbn: string;
  authorId: string;
  categoryId: string;
  publishedYear: number;
  pages: number;
  description?: string;
  publisher?: string;
  language?: string;
  coverImageUrl?: string;
};

// What we send to edit a book. Note: no authorId/categoryId/isbn here — backend UpdateBookRequest forbids changing them.
/** Request body for PUT /api/books/:id — only editable details (backend has no author/category change here). */
export type UpdateBookBody = {
  title: string;
  description?: string;
  publisher?: string;
  publishedYear: number;
  pages: number;
  language: string;
  coverImageUrl?: string;
};

// One function per endpoint. GETs just fetch; POSTs send JSON bodies.
// Each returns a Promise: use `await api.getBooks()` inside an async function.
// Example: const books: Book[] = await api.getBooks() — type after colon tells TS what shape to expect.
/**
 * api — Grouped endpoint helpers (auth, books, authors, categories, members, loans).
 * Each method returns a Promise; call with `await api.getBooks()` inside async handlers.
 */
export const api = {
  // ---- Auth (public login; every other endpoint needs the JWT) ----
  // login is the only call without a token: it mints the token. Body must be JSON.stringify({email, password}).
  // me proves the token still works (used rarely, e.g. session check).
  login: (email: string, password: string): Promise<AuthUser> =>
    req('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  me: (): Promise<{ email: string; role: string }> => req('/api/auth/me'),

  // ---- Books: catalog + stock + copies ----
  // getBooks = full catalog for BooksPage/Dashboard. getBook = one full record (has description).
  // getAvailability = stock per title in ONE call. getCopies = barcodes for one title (detail dialog + Rentals).
  getBooks: (): Promise<Book[]> => req('/api/books'),
  getBook: (id: string): Promise<Book> => req(`/api/books/${id}`),
  getAvailability: (): Promise<BookAvailability[]> =>
    req('/api/books/availability'),
  getCopies: (bookId: string): Promise<BookCopy[]> =>
    req(`/api/books/${bookId}/copies`),

  // createBook = POST with authorId/categoryId. updateBook = PUT of editable fields only (no ISBN/author change).
  // deleteBook may fail if copies/loans exist — page shows backend message.
  // addCopy = POST one barcode to one title (shelf defaults to A1 in BooksPage if empty).
  createBook: (body: CreateBookBody): Promise<Book> =>
    req('/api/books', { method: 'POST', body: JSON.stringify(body) }),

  updateBook: (id: string, body: UpdateBookBody): Promise<Book> =>
    req(`/api/books/${id}`, { method: 'PUT', body: JSON.stringify(body) }),

  deleteBook: (id: string): Promise<null> =>
    req(`/api/books/${id}`, { method: 'DELETE' }),

  addCopy: (bookId: string, barcode: string, shelfLocation?: string): Promise<BookCopy> =>
    req(`/api/books/${bookId}/copies`, {
      method: 'POST',
      body: JSON.stringify({ barcode, shelfLocation }),
    }),

  // ---- Authors: full CRUD. Delete fails if books still reference the author (backend FK) — page shows message. ----
  getAuthors: (): Promise<Author[]> => req('/api/authors'),
  createAuthor: (body: { name: string; biography?: string; dateOfBirth?: string }): Promise<Author> =>
    req('/api/authors', { method: 'POST', body: JSON.stringify(body) }),
  updateAuthor: (id: string, body: { name: string; biography?: string }): Promise<Author> =>
    req(`/api/authors/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteAuthor: (id: string): Promise<null> =>
    req(`/api/authors/${id}`, { method: 'DELETE' }),

  // ---- Categories: same CRUD shape as authors. Name is unique server-side (duplicate -> 400). ----
  getCategories: (): Promise<Category[]> => req('/api/categories'),
  createCategory: (body: { name: string; description?: string }): Promise<Category> =>
    req('/api/categories', { method: 'POST', body: JSON.stringify(body) }),
  updateCategory: (id: string, body: { name: string; description?: string }): Promise<Category> =>
    req(`/api/categories/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteCategory: (id: string): Promise<null> =>
    req(`/api/categories/${id}`, { method: 'DELETE' }),

  // ---- Members: email is set once on create (updateMember has no email field — backend forbids changing it). ----
  // deactivate/reactivate return 204 No Content (null), not a Member — that's why handleRes has the 204 case.
  getMembers: (): Promise<Member[]> => req('/api/members'),
  getMember: (id: string): Promise<Member> => req(`/api/members/${id}`),
  createMember: (body: { firstName: string; lastName: string; email: string; phone?: string }): Promise<Member> =>
    req('/api/members', { method: 'POST', body: JSON.stringify(body) }),
  updateMember: (id: string, body: { firstName: string; lastName: string; phone?: string }): Promise<Member> =>
    req(`/api/members/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deactivateMember: (id: string): Promise<null> =>
    req(`/api/members/${id}/deactivate`, { method: 'POST' }),
  reactivateMember: (id: string): Promise<null> =>
    req(`/api/members/${id}/reactivate`, { method: 'POST' }),

  // ---- Loans: you borrow a COPY (barcode id), not a title. Backend enforces max 5 active + max 2 renewals. ----
  // borrow defaults to 14 days. returnLoan flips to Returned. renewLoan extends due date (Active only).
  getLoans: (): Promise<Loan[]> => req('/api/loans'),
  getLoan: (id: string): Promise<Loan> => req(`/api/loans/${id}`),
  borrow: (bookCopyId: string, memberId: string, loanDays = 14): Promise<Loan> =>
    req('/api/loans/borrow', {
      method: 'POST',
      body: JSON.stringify({ bookCopyId, memberId, loanDays }),
    }),
  returnLoan: (loanId: string): Promise<Loan> =>
    req(`/api/loans/${loanId}/return`, { method: 'POST' }),
  renewLoan: (loanId: string): Promise<Loan> =>
    req(`/api/loans/${loanId}/renew`, { method: 'POST' }),
};
