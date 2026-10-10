/**
 * validation.ts — Client-side field validators mirroring backend caps + domain guards.
 * Junior-dev guide:
 * - Convention: each validator returns null when valid, else an error message string.
 * - LIMITS must stay in sync with backend *Configuration.cs HasMaxLength values.
 * - Forms collect FieldErrors ({field: message}) and block submit when non-empty.
 */
// Field-level validators mirroring the backend so bad input fails fast in
// the form instead of as a 400/500 from the API. Limits match the EF column
// caps (see backend Persistence/Configurations) and the domain guards
// (see backend Domain/Entities). Each returns an error message or null.
// Convention for this whole file: return null = "valid, no error". Return string = "invalid, show this text".
// Pages collect them into errs = { fieldName: message } and block save when errs is not empty.

// DB column caps — keep in sync with *Configuration.cs HasMaxLength values.
// Example: bookTitle 200 means backend column holds max 200 chars — we check the same length here first.
/** LIMITS — Max lengths per field (single source so backend cap changes touch one line). */
export const LIMITS = {
  bookTitle: 200,
  isbn: 13,
  bookDescription: 2000,
  publisher: 100,
  language: 30,
  coverImageUrl: 500,
  authorName: 100,
  biography: 2000,
  categoryName: 50,
  categoryDescription: 500,
  firstName: 50,
  lastName: 50,
  email: 200,
  phone: 20,
  barcode: 50,
  shelf: 50,
} as const;

// Backend borrow policy: Member.CanBorrow(activeCount, maxLoans = 5).
// RentalsPage checks this before calling borrow so you get a friendly message instead of a 400.
/** Max simultaneous active loans per member (mirrors backend Member.CanBorrow default). */
export const MAX_ACTIVE_LOANS = 5;
/** Max loan duration in days (catches typos; backend Loan ctor only requires >0). */
// 365 = one year cap. LoanDays 999 is probably a typo, so we stop it here.
export const MAX_LOAN_DAYS = 365;

// isBlank = true for null, undefined, "", or "   " (spaces only). !v covers null/undefined/"", !v.trim() covers spaces.
// Pages never call this directly — required()/email()/etc. use it inside.
const isBlank = (v: string | null | undefined) => !v || !v.trim();

/**
 * Check required text is non-blank.
 * @param value Raw input (null-safe).
 * @param label Field name for the message.
 * @returns Error message or null when valid.
 */
// Example: required('', 'Author') -> "Author is required.", required('King', 'Author') -> null (valid).
export function required(value: string | null | undefined, label: string): string | null {
  return isBlank(value) ? `${label} is required.` : null; // ? : is a ternary: true-part : false-part
}

/**
 * Check trimmed length fits the DB cap.
 * @param value Raw input (null-safe).
 * @param limit Max allowed trimmed length.
 * @param label Field name for the message.
 * @returns Error message or null when valid.
 */
// .trim() first so "  abc  " counts as 3, not 7. value && ... means "skip check when empty" (empty is handled by required()).
export function maxLen(value: string | null | undefined, limit: number, label: string): string | null {
  return value && value.trim().length > limit
    ? `${label} must be at most ${limit} characters.`
    : null;
}

// Required text with a DB cap (book titles, names, ...).
// ?? chains two checks: required() runs first; only if it passes (null), maxLen() runs. First error wins.
// Example: text('', 'Title', 200) -> "Title is required." (never reaches length check).
/**
 * Validate required text + DB cap (titles, names).
 * @param value Raw input.
 * @param label Field name.
 * @param limit Max length.
 * @returns Error or null.
 */
export function text(value: string | null | undefined, label: string, limit: number): string | null {
  return required(value, label) ?? maxLen(value, limit, label);
}

// Optional text with only a DB cap (publisher, biography, ...).
// Blank always passes (no required check), so empty description is fine. Only too-long fails.
/**
 * Validate optional text against a DB cap (blank always passes).
 * @param value Raw input.
 * @param limit Max length.
 * @param label Field name.
 * @returns Error or null.
 */
export function optionalText(value: string | null | undefined, limit: number, label: string): string | null {
  return maxLen(value, limit, label);
}

/**
 * Validate member email (required, DB cap, simple @-format).
 * @param value Raw email input.
 * @returns Error or null.
 */
// value! means "I checked isBlank above, so value is definitely a string here, TS trust me".
// /\S+@\S+\.\S+/ = simple check: something + @ + something + . + something. Not perfect, but catches typos.
export function email(value: string | null | undefined): string | null {
  if (isBlank(value)) return 'Email is required.';
  const v = value!.trim();
  if (v.length > LIMITS.email) return `Email must be at most ${LIMITS.email} characters.`;
  if (!/\S+@\S+\.\S+/.test(v)) return 'Please enter a valid email address.';
  return null;
}

// ISBN-10 or ISBN-13, digits only (hyphens/spaces stripped first).
// Backend column holds 13 chars and ISBNs are unique (server checks that).
// .replace(/[-\s]/g, '') removes hyphens/spaces so "978-0-13" counts as digits. /^\d+$/ = digits only.
/**
 * Validate ISBN-10 or ISBN-13 (hyphens/spaces stripped, digits-only, length 10 or 13).
 * @param value Raw ISBN input.
 * @returns Error or null.
 */
export function isbn(value: string | null | undefined): string | null {
  if (isBlank(value)) return 'ISBN is required.';
  const digits = value!.replace(/[-\s]/g, '');
  if (!/^\d+$/.test(digits)) return 'ISBN must contain digits only.';
  if (digits.length !== 10 && digits.length !== 13) {
    return 'ISBN must be 10 or 13 digits (e.g. 9780132350884).';
  }
  return null;
}

/**
 * Validate published year (whole number 1000..next year).
 * @param value Year as number or string.
 * @returns Error or null.
 */
// Number(value) turns "2008" into 2008. Number.isInteger rejects 2008.5 and NaN ("abc").
// thisYear + 1 allows next year (books announced early), but not year 3000.
export function year(value: number | string | null | undefined): string | null {
  const n = Number(value);
  const thisYear = new Date().getFullYear();
  if (!Number.isInteger(n)) return 'Year must be a whole number.';
  if (n < 1000 || n > thisYear + 1) return `Year must be between 1000 and ${thisYear + 1}.`;
  return null;
}

/**
 * Validate page count (whole number 1..10,000).
 * @param value Pages as number or string.
 * @returns Error or null.
 */
// Same Number() + isInteger pattern as year(). 10_000 underscore is just 10000 (readability).
export function pages(value: number | string | null | undefined): string | null {
  const n = Number(value);
  if (!Number.isInteger(n)) return 'Pages must be a whole number.';
  if (n < 1 || n > 10_000) return 'Pages must be between 1 and 10,000.';
  return null;
}

// Optional http(s) URL (cover images). Rejects non-URLs before they 500.
// Blank passes (cover is optional). new URL(v) throws for "not a url" — we catch and return friendly message.
// u.protocol check rejects ftp:/file: — only http/https images can display in <img>.
/**
 * Validate optional cover-image URL (blank passes, else must be http/https URL within cap).
 * @param value Raw URL input.
 * @returns Error or null.
 */
export function imageUrl(value: string | null | undefined): string | null {
  if (isBlank(value)) return null;
  const v = value!.trim();
  if (v.length > LIMITS.coverImageUrl) {
    return `Cover URL must be at most ${LIMITS.coverImageUrl} characters.`;
  }
  try {
    const u = new URL(v);
    if (u.protocol !== 'http:' && u.protocol !== 'https:') {
      return 'Cover URL must start with http:// or https://.';
    }
  } catch {
    return 'Cover URL is not a valid URL.';
  }
  return null;
}

// Optional phone: backend cap is 20 chars, digits plus common separators.
// Blank passes (phone optional). Regex allows leading + or digit, then digits/spaces/dashes/parens/dots.
// Example pass: "+1 (555) 123-4567". Fail: "abc!" (letters/symbols).
/**
 * Validate optional phone (blank passes; else charset + 20-cap).
 * @param value Raw phone input.
 * @returns Error or null.
 */
export function phone(value: string | null | undefined): string | null {
  if (isBlank(value)) return null;
  const v = value!.trim();
  if (v.length > LIMITS.phone) return `Phone must be at most ${LIMITS.phone} characters.`;
  if (!/^[+\d][\d\s\-().]*$/.test(v)) return 'Phone contains invalid characters.';
  return null;
}

/**
 * Validate copy barcode (required + 50-char cap).
 * @param value Raw barcode.
 * @returns Error or null.
 */
// Just reuses text(): barcode is required text with LIMITS.barcode cap. Uniqueness is still checked server-side.
export function barcode(value: string | null | undefined): string | null {
  return text(value, 'Barcode', LIMITS.barcode);
}

/**
 * Validate optional shelf location (blank passes, else 50-char cap).
 * @param value Raw shelf input.
 * @returns Error or null.
 */
// Just reuses optionalText(): empty shelf is fine (BooksPage defaults to "A1"), too-long fails.
export function shelf(value: string | null | undefined): string | null {
  return optionalText(value, LIMITS.shelf, 'Shelf location');
}

// Backend Loan ctor requires loanDays > 0; cap at a year to catch typos.
// RentalsPage calls this before borrow: loanDaysValidator(9999) -> "must be between 1 and 365".
/**
 * Validate loan duration (whole days 1..MAX_LOAN_DAYS).
 * @param value Days as number or string.
 * @returns Error or null.
 */
export function loanDays(value: number | string | null | undefined): string | null {
  const n = Number(value);
  if (!Number.isInteger(n)) return 'Loan days must be a whole number.';
  if (n < 1 || n > MAX_LOAN_DAYS) return `Loan days must be between 1 and ${MAX_LOAN_DAYS}.`;
  return null;
}

// Optional date of birth: must parse and cannot be in the future.
// Blank passes (DOB optional on create, hidden on edit). new Date("bad") -> NaN -> "not a valid date".
// d.getTime() > Date.now() means "date is after right now" -> future -> reject.
/**
 * Validate optional birth date (blank passes; else must parse and not be future).
 * @param value ISO date string.
 * @returns Error or null.
 */
export function dateOfBirth(value: string | null | undefined): string | null {
  if (isBlank(value)) return null;
  const d = new Date(value!);
  if (Number.isNaN(+d)) return 'Date of birth is not a valid date.';
  if (d.getTime() > Date.now()) return 'Date of birth cannot be in the future.';
  return null;
}

// Helper for forms: keeps a per-field error map, clearing a field's error
// as soon as the user edits it again.
// FieldErrors shape: { title: "Title is required.", year: "Year must be..." }. Empty {} = all valid.
// Pages do: put('title', text(...)) to fill it, then if (Object.keys(errs).length > 0) return (block save).
/** FieldErrors — Map of field name -> validation message shown under inputs. */
export type FieldErrors = Record<string, string>;

/**
 * Clear one field's error when the user edits it (so fixed input un-marks instantly).
 * @param set React setState for the FieldErrors map.
 * @param field Field key to remove.
 */
// Example: clearFieldError(setFieldErrors, 'title') removes errs.title but keeps other fields' errors.
// set((prev) => ...) uses latest state (functional form). {...prev} copies, delete removes one key.
// if (!prev[field]) return prev = "no error for this field, change nothing" (avoids useless re-render).
export function clearFieldError(
  set: React.Dispatch<React.SetStateAction<FieldErrors>>,
  field: string,
) {
  set((prev) => {
    if (!prev[field]) return prev;
    const next = { ...prev };
    delete next[field];
    return next;
  });
}
