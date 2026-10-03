// Shared display helpers: due-date labels + CSV export. Pure functions so
// any page can use them without extra fetches or state.

// Human label for a loan's due date: "Due in 3 days" / "Due today" /
// "2 days overdue". Returned loans just say when they came back.
export function dueLabel(dueDate: string, status: string, returnedAt?: string | null): string {
  if (status === 'Returned') {
    return returnedAt
      ? `Returned ${new Date(returnedAt).toLocaleDateString()}`
      : 'Returned';
  }
  const days = Math.floor((Date.now() - new Date(dueDate).getTime()) / 86_400_000);
  if (days > 0) return `${days} day${days === 1 ? '' : 's'} overdue`;
  if (days === 0) return 'Due today';
  const n = -days;
  return `Due in ${n} day${n === 1 ? '' : 's'}`;
}

// Whole days past the due date (0 when not overdue or already returned).
export function overdueDays(dueDate: string, status: string): number {
  if (status === 'Returned') return 0;
  return Math.max(0, Math.floor((Date.now() - new Date(dueDate).getTime()) / 86_400_000));
}

// Shorten long text for cards; detail dialogs show the full string.
export function truncate(text: string | null | undefined, max = 140): string {
  if (!text) return '';
  return text.length > max ? `${text.slice(0, max).trimEnd()}…` : text;
}

// Generic client-side CSV download from already-loaded rows.
// Columns with commas/quotes are RFC-4180 quoted so Excel opens it cleanly.
export function exportCsv(filename: string, headers: string[], rows: (string | number | null | undefined)[][]) {
  const esc = (v: string | number | null | undefined) =>
    `"${String(v ?? '').replace(/"/g, '""')}"`;
  const csv =
    [headers.map(esc).join(','), ...rows.map((r) => r.map(esc).join(','))].join('\r\n');
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}
