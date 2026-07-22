# Design: Frontend Unit / Component Testing

**Date:** 2026-07-22  
**Status:** Approved for implementation planning  
**Scope:** Frontend only — Vitest + Testing Library setup, one sample test, README notes

## Goal

Add unit/component testing capabilities to the Vite + React + TypeScript frontend so developers can run fast component tests locally with a proven sample path.

## Decisions

| Topic | Choice |
|-------|--------|
| Test type | Unit / component only (no E2E) |
| Runner | Vitest, configured in existing `frontend/vite.config.ts` |
| DOM environment | jsdom |
| Component helpers | `@testing-library/react`, `@testing-library/user-event`, `@testing-library/jest-dom` |
| Vitest globals | Off — import `describe` / `it` / `expect` from `vitest` |
| Test location | `frontend/src/__tests__/**/*.{test,spec}.{ts,tsx}` |
| Sample coverage | One `App` counter test |
| CI wiring | Out of scope |
| Coverage gates | Out of scope |
| Colocated `*.test.tsx` next to components | Out of scope |

## Architecture & layout

```
frontend/
  package.json              # test / test:watch scripts
  vite.config.ts            # existing Vite config + Vitest `test` block
  src/
    test/
      setup.ts              # jest-dom matchers for Vitest
    __tests__/
      App.test.tsx          # sample component test
    App.tsx                 # existing app under test
  README.md                 # Testing section
```

**Boundaries**

- Tests import app modules from `src/` the same way the app does.
- Setup file only registers Testing Library / jest-dom matchers; no app logic.
- Vite app config (plugins, proxy) stays unchanged aside from adding the Vitest `test` section.

## Tooling & config

- DevDependencies: `vitest`, `jsdom`, `@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event`.
- `vite.config.ts`: add `test` with `environment: 'jsdom'`, `setupFiles: ['./src/test/setup.ts']`, and `include: ['src/__tests__/**/*.{test,spec}.{ts,tsx}']`.
- `src/test/setup.ts`: import `@testing-library/jest-dom/vitest`.

## Scripts

| Script | Behavior |
|--------|----------|
| `npm test` | Vitest run once (CI-friendly when wired later) |
| `npm run test:watch` | Vitest watch mode for local development |

## Sample test

`src/__tests__/App.test.tsx`:

1. Render `App`.
2. Assert initial button text is `Count is 0` (via `getByRole`).
3. Click the counter button with `userEvent`.
4. Assert button text becomes `Count is 1`.

## Documentation

Add a short **Testing** section to `frontend/README.md` covering:

- How to run `npm test` and `npm run test:watch`
- Where to put new tests (`src/__tests__/`)

## Out of scope

- Playwright / Cypress / other E2E
- Coverage thresholds or reports as a gate
- CI pipeline changes
- Colocating tests beside source files
- Enabling Vitest `globals: true`

## Success criteria

- `npm test` in `frontend/` exits 0 and runs the sample `App` test.
- New component tests can be added under `src/__tests__/` without further config changes.
- README documents the commands and folder convention.
