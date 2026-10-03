import { useEffect, useMemo, useState } from 'react';
import { Box, MenuItem, Pagination, TextField, Typography } from '@mui/material';

// Reusable client-side pagination. The backend has no paging query params,
// so every page slices its already-filtered array locally.
// usePagination clamps + resets the page whenever the item list shrinks
// (e.g. after a search) so you never land on an empty page.
export function usePagination<T>(items: T[], initialPageSize = 8) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(initialPageSize);

  const total = items.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  // If filters shrink the list below the current page, jump back into range.
  useEffect(() => {
    if (page > totalPages) setPage(totalPages);
  }, [page, totalPages]);

  const paged = useMemo(() => {
    const start = (page - 1) * pageSize;
    return items.slice(start, start + pageSize);
  }, [items, page, pageSize]);

  const resetPage = () => setPage(1);

  return { page, setPage, pageSize, setPageSize, total, totalPages, paged, resetPage };
}

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
