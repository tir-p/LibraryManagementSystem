import { describe, expect, it } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import { usePagination } from './PaginationBar';

// renderHook needs a component wrapper; the default (no wrapper) suffices
// since the hook uses no context.
const items = Array.from({ length: 20 }, (_, i) => `item-${i}`);

describe('usePagination', () => {
  it('returns the first page slice by default', () => {
    const { result } = renderHook(() => usePagination(items, 8));
    expect(result.current.page).toBe(1);
    expect(result.current.total).toBe(20);
    expect(result.current.totalPages).toBe(3);
    expect(result.current.paged).toEqual(items.slice(0, 8));
  });

  it('slices later pages correctly', () => {
    const { result } = renderHook(() => usePagination(items, 8));
    act(() => result.current.setPage(3));
    expect(result.current.paged).toEqual(items.slice(16, 24));
  });

  it('clamps the page when the list shrinks below it', () => {
    // Start on page 3 of 20 items, then shrink to 5 items (1 page).
    const { result, rerender } = renderHook(
      ({ list }: { list: string[] }) => usePagination(list, 8),
      { initialProps: { list: items } },
    );
    act(() => result.current.setPage(3));
    expect(result.current.page).toBe(3);

    rerender({ list: items.slice(0, 5) });
    expect(result.current.totalPages).toBe(1);
    expect(result.current.page).toBe(1);
    expect(result.current.paged).toEqual(items.slice(0, 5));
  });

  it('respects page-size changes', () => {
    const { result } = renderHook(() => usePagination(items, 8));
    act(() => result.current.setPageSize(20));
    expect(result.current.totalPages).toBe(1);
    expect(result.current.paged).toEqual(items);
  });

  it('resetPage jumps back to page 1', () => {
    const { result } = renderHook(() => usePagination(items, 8));
    act(() => result.current.setPage(3));
    act(() => result.current.resetPage());
    expect(result.current.page).toBe(1);
  });

  it('handles an empty list without crashing', () => {
    const { result } = renderHook(() => usePagination([], 8));
    expect(result.current.total).toBe(0);
    expect(result.current.totalPages).toBe(1);
    expect(result.current.paged).toEqual([]);
  });
});
