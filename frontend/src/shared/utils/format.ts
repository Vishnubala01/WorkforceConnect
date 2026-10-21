// Organization accent colours are assigned from a fixed set by id, so a newly created organization
// gets a stable colour without the frontend knowing its name.
const ACCENTS = ['#2F5D8A', '#2E7D6B', '#7A4F9A', '#B5642B', '#44546A', '#A33B55'] as const;

export function accentFor(id: number): string {
  return ACCENTS[Math.abs(id) % ACCENTS.length] ?? ACCENTS[0];
}

export function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return '?';
  if (parts.length === 1) return (parts[0] ?? '?').slice(0, 2).toUpperCase();
  return `${parts[0]?.[0] ?? ''}${parts[parts.length - 1]?.[0] ?? ''}`.toUpperCase();
}

export function plural(count: number, singular: string, pluralForm = `${singular}s`): string {
  return `${count} ${count === 1 ? singular : pluralForm}`;
}

export function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Something went wrong. Try again.';
}
