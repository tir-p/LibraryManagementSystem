/**
 * vite.config.ts — Vite + Vitest configuration for the frontend.
 * Junior-dev guide:
 * - plugins: [react()] enables JSX/TSX + Fast Refresh.
 * - test.environment jsdom gives hook tests (renderHook) a DOM.
 * - test.include picks up any src/**.test.ts(x) / spec files.
 */
import react from '@vitejs/plugin-react'
// vitest/config re-exports vite's defineConfig plus the `test` section types.
import { defineConfig } from 'vitest/config'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    // jsdom: hook tests (renderHook) need a DOM; pure util tests run fine here too.
    environment: 'jsdom',
    include: ['src/**/*.{test,spec}.{ts,tsx}'],
  },
})
