// Compatibility wrapper: the Book type + base URL live in ./library.
// Kept so older `import { fetchBooks } from '../api/books'` still works;
// prefer `api.getBooks()` from './library' in new code.
import { API_BASE_URL, type Book } from './library';

export type { Book };
export { API_BASE_URL };

// Simple GET with its own error check (the shared api.getBooks() does the same).
export async function fetchBooks(): Promise<Book[]> {
  const res = await fetch(`${API_BASE_URL}/api/books`);

  if (!res.ok) {
    throw new Error(`Failed to load books (status ${res.status})`);
  }

  return (await res.json()) as Book[];
}
