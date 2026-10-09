/**
 * PaginationBar.test.tsx — Vitest unit tests for the usePagination hook.
 * Junior-dev guide:
 * - renderHook() mounts the hook in isolation (no page needed).
 * - act() wraps state updates so React flushes them before assertions.
 * - Each `it` covers one behavior: first slice, later slice, shrink-clamp, page-size, reset, empty list.
 */
import { describe, expect, it } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import { usePagination } from './PaginationBar';

// renderHook needs a component wrapper; the default (no wrapper) suffices
// since the hook uses no context.
// Test fixture: 20 string items ("item-0".."item-19") so page math is predictable.
const items = Array.from({ length: 20 }, (_, i) => `item-${i}`);

// Top-level suite grouping all pagination behavior tests.
describe('usePagination', () => {
  // Default case: page 1 of 20 items at 8/page = 3 pages, slice [0..8).
  it('returns the first page slice by default', () => {
    const { result } = renderHook(() => usePagination(items, 8));
    expect(result.current.page).toBe(1);
    expect(result.current.total).toBe(20);
    expect(result.current.totalPages).toBe(3);
    expect(result.current.paged).toEqual(items.slice(0, 8));
  });

  // Later pages: setPage(3) should slice items [16..24) (last partial page).
  it('slices later pages correctly', () => {
    const { result } = renderHook(() => usePagination(items, 8));
    act(() => result.current.setPage(3));
    expect(result.current.paged).toEqual(items.slice(16, 24));
  });

  // Shrink case: filtering from 20 to 5 items must clamp page 3 back to 1 (avoids empty page).
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

  // Page-size change: 20/page collapses 20 items to a single page.
  it('respects page-size changes', () => {
    const { result } = renderHook(() => usePagination(items, 8));
    act(() => result.current.setPageSize(20));
    expect(result.current.totalPages).toBe(1);
    expect(result.current.paged).toEqual(items);
  });

  // Helper check: resetPage() always jumps back to page 1.
  it('resetPage jumps back to page 1', () => {
    const { result } = renderHook(() => usePagination(items, 8));
    act(() => result.current.setPage(3));
    act(() => result.current.resetPage());
    expect(result.current.page).toBe(1);
  });

  // Edge case: empty array must not crash (total 0, 1 empty page).
  it('handles an empty list without crashing', () => {
    const { result } = renderHook(() => usePagination([], 8));
    expect(result.current.total).toBe(0);
    expect(result.current.totalPages).toBe(1);
    expect(result.current.paged).toEqual([]);
  });
});
