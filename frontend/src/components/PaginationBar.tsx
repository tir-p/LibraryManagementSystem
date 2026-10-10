/**
 * PaginationBar.tsx — Reusable client-side pagination hook + footer UI.
 * Junior-dev guide:
 * - Backend has no ?page query params, so each page slices its filtered array locally.
 * - usePagination owns page/pageSize state; PaginationBar only renders controls.
 */
import { useEffect, useMemo, useState } from 'react';
import { Box, MenuItem, Pagination, TextField, Typography } from '@mui/material';

// Reusable client-side pagination. The backend has no paging query params,
// so every page slices its already-filtered array locally.
// usePagination owns page/pageSize state; PaginationBar only renders controls.
// Example: 25 books, pageSize 8 -> 4 pages. paged = 8 items for current page.
/**
 * usePagination — Slice any array into pages with auto-clamping.
 * @param items Full (already filtered/sorted) array to paginate.
 * @param initialPageSize Rows per page on first render (default 8).
 * @returns page state, setters, totals, current slice `paged`, and resetPage().
 */
export function usePagination<T>(items: T[], initialPageSize = 8) {
  // page = 1-based current page (humans count from 1). pageSize = rows per page.
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(initialPageSize);

  const total = items.length; // total rows after filter, before paging
  // Math.ceil(25/8)=4 pages. Math.max(1, ...) keeps at least 1 page so UI never shows "page 0".
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  // If filters shrink the list below the current page, jump back into range.
  // Example: you are on page 4, then search leaves 1 page -> auto-jump to page 1 so you don't see empty.
  useEffect(() => {
    if (page > totalPages) setPage(totalPages);
  }, [page, totalPages]);

  // paged = slice for current page. (page-1)*pageSize = start index. useMemo avoids re-slicing every render.
  // Example: page 2, size 8 -> slice(8, 16).
  const paged = useMemo(() => {
    const start = (page - 1) * pageSize;
    return items.slice(start, start + pageSize);
  }, [items, page, pageSize]);

  const resetPage = () => setPage(1);

  return { page, setPage, pageSize, setPageSize, total, totalPages, paged, resetPage };
}

/**
 * PaginationBarProps — Controlled inputs for the footer row (owned by usePagination in the parent).
 */
type PaginationBarProps = {
  page: number;
  totalPages: number;
  total: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  pageSizeOptions?: number[];
};

// Footer row: "Showing X–Y of Z" + page-size dropdown + MUI Pagination.
// Renders nothing when there is only one page (avoids visual noise).
// Props are controlled from the parent: parent owns page state via usePagination, we just call onPageChange.
/**
 * PaginationBar — Footer showing "Showing X–Y of Z" + per-page dropdown + page buttons.
 * @param page Current 1-based page number.
 * @param totalPages Total pages for current pageSize.
 * @param total Total item count.
 * @param pageSize Current rows-per-page.
 * @param onPageChange Callback when user picks a new page.
 * @param onPageSizeChange Callback when user picks a new page size.
 * @param pageSizeOptions Dropdown choices (default [5,8,10,20,50]).
 */
export default function PaginationBar({
  page,
  totalPages,
  total,
  pageSize,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [5, 8, 10, 20, 50],
}: PaginationBarProps) {
  if (total === 0) return null;

  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, total);

  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 2,
        flexWrap: 'wrap',
        justifyContent: 'space-between',
        mt: 2,
      }}
    >
      <Typography variant="body2" color="text.secondary">
        Showing {from}–{to} of {total}
      </Typography>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
        <TextField
          select
          label="Per page"
          value={pageSize}
          onChange={(e) => onPageSizeChange(Number(e.target.value))}
          size="small"
          sx={{ minWidth: 110 }}
        >
          {pageSizeOptions.map((n) => (
            <MenuItem key={n} value={n}>
              {n} / page
            </MenuItem>
          ))}
        </TextField>
        {totalPages > 1 && (
          <Pagination
            count={totalPages}
            page={page}
            onChange={(_, v) => onPageChange(v)}
            color="primary"
            showFirstButton
            showLastButton
          />
        )}
      </Box>
    </Box>
  );
}
