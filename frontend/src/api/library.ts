// All backend calls live here so pages never hardcode URLs.
// Backend runs on http://localhost:5008 in dev (see backend launchSettings.json).
// Types below mirror the C# DTO records (e.g. BookDto) but in camelCase,
// because ASP.NET serializes JSON property names as camelCase by default.
export const API_BASE_URL = 'http://localhost:5008';

// Shared response checker: fetch() only rejects on network errors, NOT on
// HTTP 400/404/500. So we must inspect res.ok ourselves and throw with the
// backend's message (GlobalExceptionHandler returns { message }).
async function handleRes(res: Response) {
  if (!res.ok) {
    // Backend GlobalExceptionHandler returns { message }, ProblemDetails uses title/detail.
    let msg = `Request failed (status ${res.status})`;
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

// DTO = Data Transfer Object: the exact JSON shape the API sends/returns.
// `?` means optional, `| null` because C# nullable strings arrive as null.
export type Author = { id: string; name: string };
export type Category = { id: string; name: string };
export type Book = {
  id: string;
  title: string;
  isbn: string;
  language: string;
  publishedYear: number;
  pages: number;
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

// One function per endpoint. GETs just fetch; POSTs send JSON bodies.
// Each returns a Promise: use `await api.getBooks()` inside an async function.
export const api = {
  getBooks: () => fetch(`${API_BASE_URL}/api/books`).then(handleRes),
  getAuthors: () => fetch(`${API_BASE_URL}/api/authors`).then(handleRes),
  getCategories: () => fetch(`${API_BASE_URL}/api/categories`).then(handleRes),
  getMembers: () => fetch(`${API_BASE_URL}/api/members`).then(handleRes),
  getLoans: () => fetch(`${API_BASE_URL}/api/loans`).then(handleRes),
  getCopies: (bookId: string) =>
    fetch(`${API_BASE_URL}/api/books/${bookId}/copies`).then(handleRes),
  getAvailability: () =>
    fetch(`${API_BASE_URL}/api/books/availability`).then(handleRes),

  createBook: (body: {
    title: string;
    isbn: string;
    authorId: string;
    categoryId: string;
    publishedYear: number;
    pages: number;
    description?: string;
  }) =>
    fetch(`${API_BASE_URL}/api/books`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    }).then(handleRes),

  addCopy: (bookId: string, barcode: string, shelfLocation?: string) =>
    fetch(`${API_BASE_URL}/api/books/${bookId}/copies`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ barcode, shelfLocation }),
    }).then(handleRes),

  borrow: (bookCopyId: string, memberId: string, loanDays = 14) =>
    fetch(`${API_BASE_URL}/api/loans/borrow`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ bookCopyId, memberId, loanDays }),
    }).then(handleRes),

  returnLoan: (loanId: string) =>
    fetch(`${API_BASE_URL}/api/loans/${loanId}/return`, { method: 'POST' }).then(handleRes),

  renewLoan: (loanId: string) =>
    fetch(`${API_BASE_URL}/api/loans/${loanId}/renew`, { method: 'POST' }).then(handleRes),

  createMember: (body: { firstName: string; lastName: string; email: string; phone?: string }) =>
    fetch(`${API_BASE_URL}/api/members`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    }).then(handleRes),

  deactivateMember: (id: string) =>
    fetch(`${API_BASE_URL}/api/members/${id}/deactivate`, { method: 'POST' }).then(handleRes),

  reactivateMember: (id: string) =>
    fetch(`${API_BASE_URL}/api/members/${id}/reactivate`, { method: 'POST' }).then(handleRes),
};
