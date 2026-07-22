# Frontend Unit / Component Testing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Vitest + Testing Library to the Vite React frontend, with one sample `App` counter test under `src/__tests__/`, npm scripts, and README notes.

**Architecture:** Extend `frontend/vite.config.ts` with a Vitest `test` block (jsdom, setup file, include glob). A shared `src/test/setup.ts` registers jest-dom matchers. Sample tests live only under `src/__tests__/`; Vitest APIs are imported explicitly (no globals).

**Tech Stack:** Vite 8, React 19, TypeScript, Vitest, jsdom, `@testing-library/react`, `@testing-library/user-event`, `@testing-library/jest-dom`

**Spec:** `docs/superpowers/specs/2026-07-22-frontend-unit-testing-design.md`

## Global Constraints

- Unit / component tests only — no E2E
- Vitest config lives in `frontend/vite.config.ts` (not a separate vitest config)
- Environment: `jsdom`
- Vitest globals: off — import `describe` / `it` / `expect` from `vitest`
- Test files only under `frontend/src/__tests__/**/*.{test,spec}.{ts,tsx}`
- Sample: one `App` counter test
- No CI wiring, coverage gates, or colocated `*.test.tsx` next to components
- Windows host: use CMD for shell steps; no PowerShell/bash scripts

---

## File structure

| File | Responsibility |
|------|----------------|
| `frontend/package.json` | Add `test` / `test:watch` scripts; lockfile updated via npm install |
| `frontend/vite.config.ts` | Existing Vite app config + Vitest `test` block; import `defineConfig` from `vitest/config` |
| `frontend/src/test/setup.ts` | Register `@testing-library/jest-dom/vitest` matchers |
| `frontend/src/__tests__/App.test.tsx` | Sample component test for counter click |
| `frontend/tsconfig.app.json` | Include `@testing-library/jest-dom` in `compilerOptions.types` for matcher typings |
| `frontend/README.md` | Short Testing section |

---

### Task 1: Vitest harness + App sample test

**Files:**
- Create: `frontend/src/test/setup.ts`
- Create: `frontend/src/__tests__/App.test.tsx`
- Modify: `frontend/vite.config.ts`
- Modify: `frontend/package.json`
- Modify: `frontend/tsconfig.app.json`
- Modify: `frontend/README.md`
- Modify: `frontend/package-lock.json` (via npm install)

**Interfaces:**
- Consumes: existing `App` default export from `frontend/src/App.tsx` (counter button text `Count is {n}`)
- Produces:
  - `npm test` → Vitest single run
  - `npm run test:watch` → Vitest watch
  - Setup side effect: jest-dom matchers available in tests (e.g. `toBeInTheDocument`)

- [ ] **Step 1: Write the sample test (before runner works)**

Create `frontend/src/__tests__/App.test.tsx`:

```tsx
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import App from '../App'

describe('App', () => {
  it('increments the counter when the button is clicked', async () => {
    const user = userEvent.setup()
    render(<App />)

    const button = screen.getByRole('button', { name: /count is 0/i })
    await user.click(button)

    expect(
      screen.getByRole('button', { name: /count is 1/i }),
    ).toBeInTheDocument()
  })
})
```

- [ ] **Step 2: Write the Vitest setup file**

Create `frontend/src/test/setup.ts`:

```ts
import '@testing-library/jest-dom/vitest'
```

- [ ] **Step 3: Extend Vite config for Vitest**

Replace the contents of `frontend/vite.config.ts` with:

```ts
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5296',
    },
  },
  test: {
    // Normalize root path casing on Windows so Vitest suite context resolves
    root: fileURLToPath(new URL('./', import.meta.url)),
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/__tests__/**/*.{test,spec}.{ts,tsx}'],
  },
})
```

Also add at the top of `vite.config.ts`:

```ts
import { fileURLToPath } from 'node:url'
```

(`root` is required on some Windows shells where the drive letter is lowercased; without it Vitest fails with `Cannot read properties of undefined (reading 'config')`.)


- [ ] **Step 4: Add jest-dom types to the app TypeScript config**

In `frontend/tsconfig.app.json`, change `"types": ["vite/client"]` to:

```json
"types": ["vite/client", "@testing-library/jest-dom"]
```

- [ ] **Step 5: Install test dependencies**

From `frontend/`, run:

```cmd
cmd /c "cd /d c:\Users\NEWA002\source\repos\aae\frontend && npm install -D vitest jsdom @testing-library/react @testing-library/jest-dom @testing-library/user-event"
```

Expected: packages added under `devDependencies`; `package-lock.json` updated.

- [ ] **Step 6: Add npm scripts**

In `frontend/package.json`, add these entries under `"scripts"` (keep existing `dev`, `build`, `lint`, `preview`):

```json
"test": "vitest run",
"test:watch": "vitest"
```

- [ ] **Step 7: Run tests once and verify pass**

```cmd
cmd /c "cd /d c:\Users\NEWA002\source\repos\aae\frontend && npm test"
```

Expected: Vitest exits 0; `App.test.tsx` passes (1 test).

- [ ] **Step 8: Document testing in the frontend README**

Append this section to `frontend/README.md`:

```markdown
## Testing

Unit and component tests use [Vitest](https://vitest.dev/) and [Testing Library](https://testing-library.com/react).

```bash
npm test
npm run test:watch
```

Put new tests under `src/__tests__/` as `*.test.tsx` or `*.spec.tsx` files.
```

- [ ] **Step 9: Commit**

```cmd
cmd /c "cd /d c:\Users\NEWA002\source\repos\aae && git add frontend/package.json frontend/package-lock.json frontend/vite.config.ts frontend/tsconfig.app.json frontend/src/test/setup.ts frontend/src/__tests__/App.test.tsx frontend/README.md && git commit -m \"feat: add Vitest and Testing Library to frontend\""
```

If the shell mangles `git commit` trailers on Windows, write the message to `%TEMP%\commitmsg.txt` via a temporary `.cmd` file and use `git commit -F`, then delete the helper.

---

## Spec coverage checklist

| Spec requirement | Task |
|------------------|------|
| Vitest in `vite.config.ts` | Task 1 Step 3 |
| jsdom | Task 1 Step 3 |
| Explicit Vitest imports (no globals) | Task 1 Step 1 |
| Testing Library + jest-dom + user-event | Task 1 Steps 1, 2, 5 |
| `src/test/setup.ts` | Task 1 Step 2 |
| Tests under `src/__tests__/` | Task 1 Step 1 |
| `npm test` / `npm run test:watch` | Task 1 Step 6 |
| Sample App counter test | Task 1 Step 1 |
| README Testing section | Task 1 Step 8 |
| No CI / coverage / E2E / colocated tests | Intentionally omitted |
