/**
 * validation.test.ts — Vitest coverage for field validators + borrow policy + dueLabel.
 * Junior-dev guide:
 * - Each describe groups one validator (text, email, isbn, year/pages, imageUrl, phone/barcode/shelf, loanDays, dateOfBirth).
 * - Convention: validators return null when valid, else an error message string.
 * - Last suites pin backend parity (MAX_ACTIVE_LOANS=5) and dueLabel formatting.
 */
import { describe, expect, it } from 'vitest';
import {
  LIMITS,
  MAX_ACTIVE_LOANS,
  barcode,
  dateOfBirth,
  email,
  imageUrl,
  isbn,
  loanDays,
  pages,
  phone,
  shelf,
  text,
  year,
} from './validation';
import { dueLabel as loanDueLabel } from './libraryUtils';

// Suite: required-text helper (blank rejected, over-cap rejected, exact-cap accepted).
describe('text validator (required + DB cap)', () => {
  it('rejects blank values', () => {
    expect(text('', 'Title', 200)).toBe('Title is required.');
    expect(text('   ', 'Title', 200)).toBe('Title is required.');
    expect(text(null, 'Title', 200)).toBe('Title is required.');
  });

  it('rejects over-long values', () => {
    expect(text('a'.repeat(201), 'Title', 200)).toBe(
      'Title must be at most 200 characters.',
    );
  });

  it('accepts values at exactly the cap', () => {
    expect(text('a'.repeat(200), 'Title', 200)).toBeNull();
  });
});

// Suite: email format + required + DB cap (mirrors backend member email rules).
describe('email validator', () => {
  it('rejects blank and malformed addresses', () => {
    expect(email('')).toBe('Email is required.');
    expect(email('not-an-email')).toBe('Please enter a valid email address.');
    expect(email('a@b')).toBe('Please enter a valid email address.');
  });

  it('accepts a normal address and trims whitespace', () => {
    expect(email('  librarian@library.com  ')).toBeNull();
  });

  it('rejects addresses over the DB cap', () => {
    expect(email(`${'a'.repeat(LIMITS.email)}@x.com`)).toBe(
      `Email must be at most ${LIMITS.email} characters.`,
    );
  });
});

// Suite: ISBN-10/13 digits-only check (hyphens stripped before validation).
describe('isbn validator', () => {
  it('rejects blank values', () => {
    expect(isbn('')).toBe('ISBN is required.');
  });

  it('accepts 13-digit ISBNs', () => {
    expect(isbn('9780132350884')).toBeNull();
  });

  it('accepts 10-digit ISBNs', () => {
    expect(isbn('0132350884')).toBeNull();
  });

  it('rejects hyphens, letters, and wrong lengths', () => {
    expect(isbn('978-0132350884')).toBeNull(); // stripped before checking
    expect(isbn('978013235088')).toBe('ISBN must be 10 or 13 digits (e.g. 9780132350884).');
    expect(isbn('978013235088X')).toBe('ISBN must contain digits only.');
  });
});

// Suite: numeric guards — year must be whole 1000..next-year, pages 1..10,000 whole.
describe('year / pages validators', () => {
  it('rejects non-integers and out-of-range years', () => {
    expect(year('abc')).toBe('Year must be a whole number.');
    expect(year(999)).toMatch(/between 1000 and/);
    expect(year(new Date().getFullYear() + 2)).toMatch(/between 1000 and/);
  });

  it('accepts the current year', () => {
    expect(year(new Date().getFullYear())).toBeNull();
  });

  it('rejects zero/negative pages and huge values', () => {
    expect(pages(0)).toBe('Pages must be between 1 and 10,000.');
    expect(pages(-5)).toBe('Pages must be between 1 and 10,000.');
    expect(pages(10_001)).toBe('Pages must be between 1 and 10,000.');
    expect(pages(2.5)).toBe('Pages must be a whole number.');
    expect(pages(320)).toBeNull();
  });
});

// Suite: optional cover-image URL (empty allowed, else must be http/https URL).
describe('imageUrl validator', () => {
  it('allows empty (optional field)', () => {
    expect(imageUrl('')).toBeNull();
    expect(imageUrl(null)).toBeNull();
  });

  it('accepts http(s) URLs', () => {
    expect(imageUrl('https://example.com/cover.jpg')).toBeNull();
  });

  it('rejects non-URLs and non-http schemes', () => {
    expect(imageUrl('not a url')).toBe('Cover URL is not a valid URL.');
    expect(imageUrl('ftp://example.com/x.jpg')).toBe(
      'Cover URL must start with http:// or https://.',
    );
  });
});

// Suite: phone (optional, charset + 20-cap), barcode (required, 50-cap), shelf (optional).
describe('phone / barcode / shelf validators', () => {
  it('allows empty optional phone and shelf', () => {
    expect(phone('')).toBeNull();
    expect(shelf('')).toBeNull();
  });

  it('accepts common phone formats', () => {
    expect(phone('+1 (555) 123-4567')).toBeNull();
  });

  it('rejects bad phone characters and over-long values', () => {
    expect(phone('abc!')).toBe('Phone contains invalid characters.');
    expect(phone('1'.repeat(21))).toBe('Phone must be at most 20 characters.');
  });

  it('requires barcodes within the DB cap', () => {
    expect(barcode('')).toBe('Barcode is required.');
    expect(barcode('b'.repeat(51))).toBe('Barcode must be at most 50 characters.');
    expect(barcode('BC-0001')).toBeNull();
  });
});

// Suite: loanDays 1..365 whole numbers (default rental is 14 days).
describe('loanDays validator', () => {
  it('rejects zero, negatives, fractions, and absurd values', () => {
    expect(loanDays(0)).toMatch(/between 1 and/);
    expect(loanDays(-3)).toMatch(/between 1 and/);
    expect(loanDays(1.5)).toBe('Loan days must be a whole number.');
    expect(loanDays(366)).toMatch(/between 1 and/);
  });

  it('accepts the 14-day default', () => {
    expect(loanDays(14)).toBeNull();
  });
});

// Suite: optional birth date (empty/past allowed, future/garbage rejected).
describe('dateOfBirth validator', () => {
  it('allows empty and past dates', () => {
    expect(dateOfBirth('')).toBeNull();
    expect(dateOfBirth('1990-05-01')).toBeNull();
  });

  it('rejects future dates and garbage', () => {
    expect(dateOfBirth('2999-01-01')).toBe('Date of birth cannot be in the future.');
    expect(dateOfBirth('not-a-date')).toBe('Date of birth is not a valid date.');
  });
});

// Suite: borrow-policy constant must stay in sync with backend Member.CanBorrow cap (5).
describe('borrow policy constants', () => {
  it('matches the backend Member.CanBorrow default cap of 5', () => {
    expect(MAX_ACTIVE_LOANS).toBe(5);
  });
});

// Suite: dueLabel display helper (Returned label vs "N days overdue").
describe('dueLabel (libraryUtils)', () => {
  it('labels returned loans by return date', () => {
    expect(loanDueLabel('2026-01-01', 'Returned', '2026-01-05')).toContain('Returned');
  });

  it('flags overdue loans with a day count', () => {
    const tenDaysAgo = new Date(Date.now() - 10 * 86_400_000).toISOString();
    expect(loanDueLabel(tenDaysAgo, 'Overdue')).toBe('10 days overdue');
  });
});
