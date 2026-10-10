/**
 * libraryUtils.ts — Pure display helpers shared by pages (no fetch, no state).
 * Junior-dev guide:
 * - dueLabel/overdueDays format loan dates; truncate shortens card text; exportCsv downloads tables.
 * - Pure = same inputs always give same outputs, safe to call during render.
 */
// Shared display helpers: due-date labels + CSV export. Pure functions so
// any page can use them without extra fetches or state.
// Pure = same inputs always give same output, safe to call during render (no useState/useEffect inside).

// Human label for a loan's due date: "Due in 3 days" / "Due today" /
// "2 days overdue". Returned loans just say when they came back.
// Math: (now - dueDate) / 86_400_000 turns milliseconds into days (1000*60*60*24).
// days > 0 = past due, 0 = today, negative = still time left (we flip sign with -days).
/**
 * Build a human label for a loan's due date ("Due in 3 days" / "Due today" / "2 days overdue").
 * @param dueDate ISO due-date string from the Loan DTO.
 * @param status Loan status (Active | Overdue | Returned).
 * @param returnedAt Optional return timestamp for Returned loans.
 * @returns Friendly label for cards and dashboard.
 */
export function dueLabel(dueDate: string, status: string, returnedAt?: string | null): string {
  // Returned loans ignore due math and just show return date. toLocaleDateString() formats for your locale (e.g. 10/8/2026).
  if (status === 'Returned') {
    return returnedAt
      ? `Returned ${new Date(returnedAt).toLocaleDateString()}`
      : 'Returned';
  }
  // Math.floor rounds down to whole days. days === 1 ? '' : 's' fixes plural ("1 day" vs "2 days").
  const days = Math.floor((Date.now() - new Date(dueDate).getTime()) / 86_400_000);
  if (days > 0) return `${days} day${days === 1 ? '' : 's'} overdue`;
  if (days === 0) return 'Due today';
  const n = -days;
  return `Due in ${n} day${n === 1 ? '' : 's'}`;
}

// Whole days past the due date (0 when not overdue or already returned).
// Math.max(0, ...) clamps negatives to 0: "due in 3 days" counts as 0 overdue, not -3.
// RentalsPage "most overdue" sort uses this number to order rows.
/**
 * Count whole days past the due date (0 when not overdue or already returned).
 * @param dueDate ISO due-date string.
 * @param status Loan status; Returned always yields 0.
 * @returns Non-negative overdue day count (used for "most overdue" sort).
 */
export function overdueDays(dueDate: string, status: string): number {
  if (status === 'Returned') return 0;
  return Math.max(0, Math.floor((Date.now() - new Date(dueDate).getTime()) / 86_400_000));
}

// Shorten long text for cards; detail dialogs show the full string.
// Example: truncate("hello world", 5) -> "hello…". Short text returns unchanged.
// .trimEnd() removes trailing space before … so "hello …" never happens. !text covers null/undefined/"".
/**
 * Shorten long text with an ellipsis for card previews.
 * @param text Input string (null-safe).
 * @param max Max chars before truncation (default 140).
 * @returns Original text if short, else sliced + "…".
 */
export function truncate(text: string | null | undefined, max = 140): string {
  if (!text) return '';
  return text.length > max ? `${text.slice(0, max).trimEnd()}…` : text;
}

// Generic client-side CSV download from already-loaded rows.
// Columns with commas/quotes are RFC-4180 quoted so Excel opens it cleanly.
// Trick: build a Blob (file in memory) -> fake <a download> link -> click it -> browser saves file. No backend needed.
// esc wraps every cell in quotes and doubles inner quotes: He said "hi" -> "He said ""hi""". v ?? '' turns null into "".
/**
 * Download already-loaded rows as a CSV file (client-side Blob + anchor click).
 * @param filename Download name (e.g. "books.csv").
 * @param headers Column titles for the first row.
 * @param rows 2D array of cell values (quoted per RFC-4180 so commas are safe).
 */
export function exportCsv(filename: string, headers: string[], rows: (string | number | null | undefined)[][]) {
  const esc = (v: string | number | null | undefined) =>
    `"${String(v ?? '').replace(/"/g, '""')}"`; // String(...) handles numbers, ?? '' handles null, replace doubles quotes
  // Join cells with commas, rows with \r\n (Excel expects CRLF). First row = headers.
  const csv =
    [headers.map(esc).join(','), ...rows.map((r) => r.map(esc).join(','))].join('\r\n');
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' }); // file in memory
  const url = URL.createObjectURL(blob); // temporary URL like blob:http://... pointing at that memory
  const a = document.createElement('a'); // invisible download link
  a.href = url;
  a.download = filename; // e.g. "books.csv" — tells browser to save, not navigate
  document.body.appendChild(a); // must be in DOM for click() to work in some browsers
  a.click(); // start download
  a.remove(); // clean up link
  URL.revokeObjectURL(url); // free the memory
}
