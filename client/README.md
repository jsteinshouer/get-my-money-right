# Household Budget — client

React + TypeScript (Vite), styled with [Pico.css](https://picocss.com/) (classless — minimal custom styling, relies on semantic HTML).

## Development

```bash
npm install
npm run dev
```

The dev server proxies `/api` to the ASP.NET Core API on `http://localhost:5059` (see `vite.config.ts`), so run `dotnet run` from `src/Api` alongside it.

## Tests

```bash
npm run lint       # oxlint
npx tsc -b         # typecheck
npm run test:e2e   # Playwright, against a real API + Vite dev server pair
```

### End-to-end tests

`npm run test:e2e` requires the Playwright browser binary once:

```bash
npx playwright install chromium
```

On most machines, also run `sudo npx playwright install --with-deps` once so Chromium's OS-level shared libraries (`libnspr4`, `libnss3`, `libasound2`, etc.) are installed system-wide.

If you don't have root (some WSL setups don't), `npm run test:e2e` runs through `scripts/test-e2e.sh`, which detects missing shared libraries, fetches them into `~/.cache/pw-missing-libs` with `apt-get download` + `dpkg-deb -x` (no root needed), and points `LD_LIBRARY_PATH` at them automatically. This is a one-time fetch — later runs reuse the cache.

## Expanding the Oxlint configuration

If enabling type-aware lint rules, install `oxlint-tsgolint` and edit `.oxlintrc.json`:

```json
{
  "$schema": "./node_modules/oxlint/configuration_schema.json",
  "plugins": ["react", "typescript", "oxc"],
  "options": {
    "typeAware": true
  },
  "rules": {
    "react/rules-of-hooks": "error",
    "react/only-export-components": ["warn", { "allowConstantExport": true }]
  }
}
```

See the [Oxlint rules documentation](https://oxc.rs/docs/guide/usage/linter/rules) for the full list of rules and categories.
