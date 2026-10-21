import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

function sourceFiles(dir: string): string[] {
  return readdirSync(dir).flatMap((entry) => {
    const path = join(dir, entry);
    if (statSync(path).isDirectory()) return sourceFiles(path);
    return /\.(ts|tsx)$/.test(entry) && !/\.test\.tsx?$/.test(entry) && !path.includes(`${join('src', 'test')}`) ? [path] : [];
  });
}

describe('no hard-coded organizations, teams or role checks', () => {
  const files = sourceFiles(join(process.cwd(), 'src')).map((path) => ({ path, text: readFileSync(path, 'utf8') }));

  it('does not name any seeded organization or team in application code', () => {
    const banned = [/['"`]NNX['"`]/, /['"`]7NX['"`]/, /['"`]HR & Operations['"`]/, /['"`]S1['"`]/, /['"`]TME['"`]/, /['"`]POD['"`]/, /['"`]Capture['"`]/];
    const hits = files.flatMap(({ path, text }) => banned.filter((re) => re.test(text)).map((re) => `${path}: ${re}`));
    expect(hits).toEqual([]);
  });

  it('never branches on a role name or a user id', () => {
    const banned = [/role\s*===?\s*['"`]/i, /\.role\s*==/, /user\??\.id\s*===?\s*\d/];
    const hits = files.flatMap(({ path, text }) => banned.filter((re) => re.test(text)).map((re) => `${path}: ${re}`));
    expect(hits).toEqual([]);
  });
});
