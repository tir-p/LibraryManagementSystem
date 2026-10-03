// All backend calls live here so pages never hardcode URLs.
// Backend runs on http://localhost:5008 in dev (see backend launchSettings.json).
// Types below mirror the C# DTO records (e.g. BookDto) but in camelCase,
// because ASP.NET serializes JSON property names as camelCase by default.
export const API_BASE_URL =
  (import.meta as unknown as { env?: Record<string, string | undefined> }).env
    ?.VITE_API_URL || 'http://localhost:5008';

// Staff auth: the backend mints a JWT on POST /api/auth/login carrying the
// role claim ("Librarian" = full access, "Assistant" = read-only GETs).
// The token is stored in localStorage and sent as `Authorization: Bearer …`
// on every request by req() below.
export type AuthUser = {
  email: string;
  role: string;
  token: string;
  expiresAt: string;
};

const AUTH_KEY = 'libraryAuth';

export function getStoredAuth(): AuthUser | null {
  try {
    const raw = localStorage.getItem(AUTH_KEY);
    return raw ? (JSON.parse(raw) as AuthUser) : null;
  } catch {
    return null;
  }
}

export function setStoredAuth(user: AuthUser) {
  localStorage.setItem(AUTH_KEY, JSON.stringify(user));
}

export function clearStoredAuth() {
  localStorage.removeItem(AUTH_KEY);
  // Drop the legacy pre-auth key so old sessions can't linger.
  localStorage.removeItem('libraryUserEmail');
}

// Role names mirror backend UserRoles. Single source so a rename touches one line.
export const LIBRARIAN_ROLE = 'Librarian';

// Central place to decide write access: only Librarians mutate data.
export const isLibrarian = (user: AuthUser | null) => user?.role === LIBRARIAN_ROLE;

// Shared response checker: fetch() only rejects on network errors, NOT on
// HTTP 400/404/500. So we must inspect res.ok ourselves and throw with the
// backend's message (GlobalExceptionHandler returns { message }).
async function handleRes(res: Response) {
  if (!res.ok) {
    // 401 = missing/expired token -> drop it so the app falls back to login.
    if (res.status === 401) clearStoredAuth();
    // Backend GlobalExceptionHandler returns { message }, ProblemDetails uses title/detail.
    // 403 has an empty body (auth middleware), so give it a helpful default.
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
    throw new Error(msg);
  }
  if (res.status === 204) return null; // 204 No Content (deactivate/reactivate) has no body to parse.
  return res.json();
}

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
export type Author = {
  id: string;
  name: string;
  biography?: string | null;
  dateOfBirth?: string | null;
};

export type Category = {
  id: string;
  name: string;
  description?: string | null;
};

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

export type BookAvailability = {
  bookId: string;
  totalCopies: number;
  availableCopies: number;
};

export type BookCopy = {
  id: string;
  bookId: string;
  bookTitle?: string | null;
  barcode: string;
  shelfLocation?: string | null;
  status: string;
  isAvailable: boolean;
};

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
export const api = {
  // ---- Auth (public login; every other endpoint needs the JWT) ----
  login: (email: string, password: string): Promise<AuthUser> =>
    req('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  me: (): Promise<{ email: string; role: string }> => req('/api/auth/me'),

  // ---- Books ----
  getBooks: (): Promise<Book[]> => req('/api/books'),
  getBook: (id: string): Promise<Book> => req(`/api/books/${id}`),
  getAvailability: (): Promise<BookAvailability[]> =>
    req('/api/books/availability'),
  getCopies: (bookId: string): Promise<BookCopy[]> =>
    req(`/api/books/${bookId}/copies`),

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

  // ---- Authors ----
  getAuthors: (): Promise<Author[]> => req('/api/authors'),
  createAuthor: (body: { name: string; biography?: string; dateOfBirth?: string }): Promise<Author> =>
    req('/api/authors', { method: 'POST', body: JSON.stringify(body) }),
  updateAuthor: (id: string, body: { name: string; biography?: string }): Promise<Author> =>
    req(`/api/authors/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteAuthor: (id: string): Promise<null> =>
    req(`/api/authors/${id}`, { method: 'DELETE' }),

  // ---- Categories ----
  getCategories: (): Promise<Category[]> => req('/api/categories'),
  createCategory: (body: { name: string; description?: string }): Promise<Category> =>
    req('/api/categories', { method: 'POST', body: JSON.stringify(body) }),
  updateCategory: (id: string, body: { name: string; description?: string }): Promise<Category> =>
    req(`/api/categories/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteCategory: (id: string): Promise<null> =>
    req(`/api/categories/${id}`, { method: 'DELETE' }),

  // ---- Members ----
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

  // ---- Loans ----
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
