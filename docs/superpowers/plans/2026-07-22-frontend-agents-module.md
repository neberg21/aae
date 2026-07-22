# Frontend Agents Module Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a search + detail Agents UI under `frontend/src/modules/agents/` at `/module/agents` and `/module/agents/:id`, calling the existing `/api/agents` endpoints.

**Architecture:** Thin app shell with `BrowserRouter` and a hand-written module registry. Agents feature owns types, `fetch` API helpers, list/detail pages, routes, and CSS. Empty filters never hit the network; search uses three exact-match query params; detail loads by route `:id`.

**Tech Stack:** React 19, TypeScript, Vite 8, `react-router-dom`, Vitest, Testing Library, relative `/api/...` fetch

**Spec:** `docs/superpowers/specs/2026-07-22-frontend-agents-module-design.md`

## Global Constraints

- All feature code under `frontend/src/modules/agents/` (shell only registers routes)
- Routes: `/module/agents`, `/module/agents/:id`; `/` redirects to `/module/agents`
- Empty filters → empty state, **no** API call
- Search: `GET /api/agents/search` with only non-empty `name` / `department` / `jobTitle`
- Detail: `GET /api/agents/{id}`
- Relative `/api/...` only — no `VITE_API_BASE_URL`
- No React Query, auth, chat, create-identity, or backend changes
- Tests under `frontend/src/__tests__/`; Vitest globals off
- Windows host: CMD for shell steps; no PowerShell/bash scripts
- C# N/A — frontend only

---

## File structure

| File | Responsibility |
|------|----------------|
| `frontend/package.json` / lockfile | Add `react-router-dom` |
| `frontend/src/modules/agents/types.ts` | DTO / filter / error types |
| `frontend/src/modules/agents/api.ts` | `searchAgents`, `getAgent`, `ApiError` |
| `frontend/src/modules/agents/AgentsListPage.tsx` | Filters + results list |
| `frontend/src/modules/agents/AgentDetailPage.tsx` | Detail by `:id` |
| `frontend/src/modules/agents/routes.tsx` | Module route definitions |
| `frontend/src/modules/agents/agents.css` | Module styles |
| `frontend/src/modules/index.ts` | Registry exporting agents routes |
| `frontend/src/App.tsx` | `BrowserRouter` + registry routes + `/` redirect |
| `frontend/src/__tests__/agentsApi.test.ts` | API helper unit tests |
| `frontend/src/__tests__/AgentsListPage.test.tsx` | List page behavior |
| `frontend/src/__tests__/AgentDetailPage.test.tsx` | Detail page behavior |
| `frontend/src/__tests__/App.test.tsx` | Replace counter test with redirect/shell coverage |

---

### Task 1: Types + API client

**Files:**
- Create: `frontend/src/modules/agents/types.ts`
- Create: `frontend/src/modules/agents/api.ts`
- Create: `frontend/src/__tests__/agentsApi.test.ts`
- Modify: `frontend/package.json` (via `npm install react-router-dom`)
- Modify: `frontend/package-lock.json` (via npm)

**Interfaces:**
- Consumes: existing backend JSON camelCase (`identityId`, `name`, `department`, `jobTitle`, `systemPrompt`, `items`, …)
- Produces:
  - `export type AgentDto = { identityId: string; name: string; department: string; jobTitle: string }`
  - `export type AgentDetail = AgentDto & { systemPrompt: string }`
  - `export type AgentsPage = { items: AgentDto[]; totalCount: number; pageSize: number; pageNumber: number; totalPages: number }`
  - `export type AgentSearchFilters = { name?: string; department?: string; jobTitle?: string }`
  - `export class ApiError extends Error { readonly status: number }`
  - `export function searchAgents(filters: AgentSearchFilters): Promise<AgentsPage>`
  - `export function getAgent(id: string): Promise<AgentDetail>`

- [ ] **Step 1: Install react-router-dom**

Run from `frontend/`:

```cmd
npm install react-router-dom
```

Expected: dependency added; package ships its own types (do **not** add `@types/react-router-dom` unless TypeScript errors require it).

- [ ] **Step 2: Write failing API tests**

Create `frontend/src/__tests__/agentsApi.test.ts`:

```ts
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, getAgent, searchAgents } from '../modules/agents/api'

describe('agents api', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('searchAgents sends only non-empty query params', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        items: [],
        totalCount: 0,
        pageSize: 0,
        pageNumber: 1,
        totalPages: 0,
      }),
    })
    vi.stubGlobal('fetch', fetchMock)

    await searchAgents({ name: 'Leo', department: '  ', jobTitle: undefined })

    expect(fetchMock).toHaveBeenCalledTimes(1)
    const url = String(fetchMock.mock.calls[0][0])
    expect(url).toBe('/api/agents/search?name=Leo')
  })

  it('getAgent returns detail on success', async () => {
    const body = {
      identityId: 'leo',
      name: 'Leo',
      department: 'Ops',
      jobTitle: 'Orchestrator',
      systemPrompt: 'You are Leo.',
    }
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => body,
      }),
    )

    const agent = await getAgent('leo')
    expect(agent).toEqual(body)
    expect(fetch).toHaveBeenCalledWith('/api/agents/leo')
  })

  it('getAgent throws ApiError with status on failure', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: false,
        status: 404,
        statusText: 'Not Found',
      }),
    )

    await expect(getAgent('missing')).rejects.toMatchObject({
      name: 'ApiError',
      status: 404,
    })
    await expect(getAgent('missing')).rejects.toBeInstanceOf(ApiError)
  })
})
```

- [ ] **Step 3: Run tests — expect FAIL**

```cmd
cd frontend
npm test -- src/__tests__/agentsApi.test.ts
```

Expected: FAIL (module not found / exports missing).

- [ ] **Step 4: Implement types + api**

Create `frontend/src/modules/agents/types.ts`:

```ts
export type AgentDto = {
  identityId: string
  name: string
  department: string
  jobTitle: string
}

export type AgentDetail = AgentDto & {
  systemPrompt: string
}

export type AgentsPage = {
  items: AgentDto[]
  totalCount: number
  pageSize: number
  pageNumber: number
  totalPages: number
}

export type AgentSearchFilters = {
  name?: string
  department?: string
  jobTitle?: string
}
```

Create `frontend/src/modules/agents/api.ts`:

```ts
import type { AgentDetail, AgentSearchFilters, AgentsPage } from './types'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new ApiError(response.status, response.statusText || `HTTP ${response.status}`)
  }
  return (await response.json()) as T
}

export async function searchAgents(filters: AgentSearchFilters): Promise<AgentsPage> {
  const params = new URLSearchParams()
  if (filters.name?.trim()) params.set('name', filters.name.trim())
  if (filters.department?.trim()) params.set('department', filters.department.trim())
  if (filters.jobTitle?.trim()) params.set('jobTitle', filters.jobTitle.trim())

  const query = params.toString()
  const url = query ? `/api/agents/search?${query}` : '/api/agents/search'
  const response = await fetch(url)
  return readJson<AgentsPage>(response)
}

export async function getAgent(id: string): Promise<AgentDetail> {
  const response = await fetch(`/api/agents/${encodeURIComponent(id)}`)
  return readJson<AgentDetail>(response)
}
```

- [ ] **Step 5: Run tests — expect PASS**

```cmd
cd frontend
npm test -- src/__tests__/agentsApi.test.ts
```

Expected: PASS (3 tests).

- [ ] **Step 6: Commit**

```cmd
cd frontend
npm test
cd ..
git add frontend/package.json frontend/package-lock.json frontend/src/modules/agents/types.ts frontend/src/modules/agents/api.ts frontend/src/__tests__/agentsApi.test.ts
git commit -m "feat: add agents API client and types"
```

(If the environment injects a broken `git commit --trailer` under PowerShell, run the commit via a temporary `.cmd` file.)

---

### Task 2: AgentsListPage

**Files:**
- Create: `frontend/src/modules/agents/AgentsListPage.tsx`
- Create: `frontend/src/__tests__/AgentsListPage.test.tsx`

**Interfaces:**
- Consumes: `searchAgents`, `AgentDto` from Task 1
- Produces: default export `AgentsListPage` React component

- [ ] **Step 1: Write failing list-page tests**

Create `frontend/src/__tests__/AgentsListPage.test.tsx`:

```tsx
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AgentsListPage from '../modules/agents/AgentsListPage'
import { searchAgents } from '../modules/agents/api'

vi.mock('../modules/agents/api', () => ({
  searchAgents: vi.fn(),
}))

const searchAgentsMock = vi.mocked(searchAgents)

function renderPage() {
  return render(
    <MemoryRouter>
      <AgentsListPage />
    </MemoryRouter>,
  )
}

describe('AgentsListPage', () => {
  beforeEach(() => {
    searchAgentsMock.mockReset()
  })

  it('shows empty state and does not fetch when all filters are empty', async () => {
    const user = userEvent.setup()
    renderPage()

    await user.click(screen.getByRole('button', { name: /search/i }))

    expect(
      screen.getByText(/enter at least one filter/i),
    ).toBeInTheDocument()
    expect(searchAgentsMock).not.toHaveBeenCalled()
  })

  it('searches and renders result links to detail routes', async () => {
    const user = userEvent.setup()
    searchAgentsMock.mockResolvedValue({
      items: [
        {
          identityId: 'leo',
          name: 'Leo',
          department: 'Ops',
          jobTitle: 'Orchestrator',
        },
      ],
      totalCount: 1,
      pageSize: 1,
      pageNumber: 1,
      totalPages: 1,
    })

    renderPage()

    await user.type(screen.getByLabelText(/name/i), 'Leo')
    await user.click(screen.getByRole('button', { name: /search/i }))

    await waitFor(() => {
      expect(searchAgentsMock).toHaveBeenCalledWith({
        name: 'Leo',
        department: undefined,
        jobTitle: undefined,
      })
    })

    const link = await screen.findByRole('link', { name: /leo/i })
    expect(link).toHaveAttribute('href', '/module/agents/leo')
    expect(screen.getByText('Ops')).toBeInTheDocument()
    expect(screen.getByText('Orchestrator')).toBeInTheDocument()
  })
})
```

- [ ] **Step 2: Run tests — expect FAIL**

```cmd
cd frontend
npm test -- src/__tests__/AgentsListPage.test.tsx
```

Expected: FAIL (page module missing).

- [ ] **Step 3: Implement AgentsListPage**

Create `frontend/src/modules/agents/AgentsListPage.tsx`:

```tsx
import { FormEvent, useState } from 'react'
import { Link } from 'react-router-dom'
import { searchAgents } from './api'
import type { AgentDto } from './types'
import './agents.css'

type Status = 'idle' | 'loading' | 'success' | 'error'

export default function AgentsListPage() {
  const [name, setName] = useState('')
  const [department, setDepartment] = useState('')
  const [jobTitle, setJobTitle] = useState('')
  const [items, setItems] = useState<AgentDto[]>([])
  const [status, setStatus] = useState<Status>('idle')
  const [message, setMessage] = useState('Enter at least one filter')
  const [error, setError] = useState<string | null>(null)

  async function onSubmit(event: FormEvent) {
    event.preventDefault()

    const filters = {
      name: name.trim() || undefined,
      department: department.trim() || undefined,
      jobTitle: jobTitle.trim() || undefined,
    }

    if (!filters.name && !filters.department && !filters.jobTitle) {
      setItems([])
      setStatus('idle')
      setMessage('Enter at least one filter')
      setError(null)
      return
    }

    setStatus('loading')
    setError(null)
    try {
      const page = await searchAgents(filters)
      setItems(page.items)
      setStatus('success')
      setMessage(page.items.length === 0 ? 'No agents matched.' : '')
    } catch (err) {
      setItems([])
      setStatus('error')
      setError(err instanceof Error ? err.message : 'Search failed')
    }
  }

  return (
    <main className="agents-page">
      <h1>Agents</h1>
      <form onSubmit={onSubmit} className="agents-filters">
        <label>
          Name
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            name="name"
            autoComplete="off"
          />
        </label>
        <label>
          Department
          <input
            value={department}
            onChange={(e) => setDepartment(e.target.value)}
            name="department"
            autoComplete="off"
          />
        </label>
        <label>
          Job title
          <input
            value={jobTitle}
            onChange={(e) => setJobTitle(e.target.value)}
            name="jobTitle"
            autoComplete="off"
          />
        </label>
        <button type="submit">Search</button>
      </form>

      {status === 'loading' && <p>Loading…</p>}
      {error && <p role="alert">{error}</p>}
      {status !== 'loading' && message && <p>{message}</p>}

      {items.length > 0 && (
        <table className="agents-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Department</th>
              <th>Job title</th>
              <th>Id</th>
            </tr>
          </thead>
          <tbody>
            {items.map((agent) => (
              <tr key={agent.identityId}>
                <td>
                  <Link to={`/module/agents/${agent.identityId}`}>{agent.name}</Link>
                </td>
                <td>{agent.department}</td>
                <td>{agent.jobTitle}</td>
                <td>{agent.identityId}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  )
}
```

Create a minimal `frontend/src/modules/agents/agents.css` (enough for the import to resolve; polish in Task 4 is fine):

```css
.agents-page {
  max-width: 56rem;
  margin: 2rem auto;
  padding: 0 1rem;
  font-family: system-ui, sans-serif;
}

.agents-filters {
  display: grid;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
}

.agents-filters label {
  display: grid;
  gap: 0.25rem;
}

.agents-table {
  width: 100%;
  border-collapse: collapse;
}

.agents-table th,
.agents-table td {
  text-align: left;
  padding: 0.5rem;
  border-bottom: 1px solid #ccc;
}
```

- [ ] **Step 4: Run tests — expect PASS**

```cmd
cd frontend
npm test -- src/__tests__/AgentsListPage.test.tsx
```

Expected: PASS.

If `getByLabelText(/name/i)` matches multiple labels, tighten labels (e.g. wrap text + `htmlFor` / `id`) until the test is stable — keep accessible names.

- [ ] **Step 5: Commit**

```cmd
git add frontend/src/modules/agents/AgentsListPage.tsx frontend/src/modules/agents/agents.css frontend/src/__tests__/AgentsListPage.test.tsx
git commit -m "feat: add agents list/search page"
```

---

### Task 3: AgentDetailPage

**Files:**
- Create: `frontend/src/modules/agents/AgentDetailPage.tsx`
- Create: `frontend/src/__tests__/AgentDetailPage.test.tsx`

**Interfaces:**
- Consumes: `getAgent`, `ApiError` from Task 1; route param `id`
- Produces: default export `AgentDetailPage`

- [ ] **Step 1: Write failing detail-page tests**

Create `frontend/src/__tests__/AgentDetailPage.test.tsx`:

```tsx
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AgentDetailPage from '../modules/agents/AgentDetailPage'
import { ApiError, getAgent } from '../modules/agents/api'

vi.mock('../modules/agents/api', () => ({
  getAgent: vi.fn(),
  ApiError: class ApiError extends Error {
    status: number
    constructor(status: number, message: string) {
      super(message)
      this.name = 'ApiError'
      this.status = status
    }
  },
}))

const getAgentMock = vi.mocked(getAgent)

function renderAt(id: string) {
  return render(
    <MemoryRouter initialEntries={[`/module/agents/${id}`]}>
      <Routes>
        <Route path="/module/agents/:id" element={<AgentDetailPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('AgentDetailPage', () => {
  beforeEach(() => {
    getAgentMock.mockReset()
  })

  it('loads and shows agent fields', async () => {
    getAgentMock.mockResolvedValue({
      identityId: 'leo',
      name: 'Leo',
      department: 'Ops',
      jobTitle: 'Orchestrator',
      systemPrompt: 'You are Leo.',
    })

    renderAt('leo')

    await waitFor(() => {
      expect(getAgentMock).toHaveBeenCalledWith('leo')
    })

    expect(await screen.findByRole('heading', { name: 'Leo' })).toBeInTheDocument()
    expect(screen.getByText('Ops')).toBeInTheDocument()
    expect(screen.getByText('Orchestrator')).toBeInTheDocument()
    expect(screen.getByText('leo')).toBeInTheDocument()
    expect(screen.getByText('You are Leo.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /back to agents/i })).toHaveAttribute(
      'href',
      '/module/agents',
    )
  })

  it('shows not-found on 404', async () => {
    getAgentMock.mockRejectedValue(new ApiError(404, 'Not Found'))

    renderAt('missing')

    expect(await screen.findByText(/agent not found/i)).toBeInTheDocument()
  })
})
```

- [ ] **Step 2: Run tests — expect FAIL**

```cmd
cd frontend
npm test -- src/__tests__/AgentDetailPage.test.tsx
```

Expected: FAIL (page module missing).

- [ ] **Step 3: Implement AgentDetailPage**

Create `frontend/src/modules/agents/AgentDetailPage.tsx`:

```tsx
import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiError, getAgent } from './api'
import type { AgentDetail } from './types'
import './agents.css'

type Status = 'loading' | 'success' | 'notfound' | 'error'

export default function AgentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [agent, setAgent] = useState<AgentDetail | null>(null)
  const [status, setStatus] = useState<Status>('loading')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!id) {
      setStatus('notfound')
      return
    }

    let cancelled = false
    setStatus('loading')
    setError(null)

    getAgent(id)
      .then((data) => {
        if (cancelled) return
        setAgent(data)
        setStatus('success')
      })
      .catch((err: unknown) => {
        if (cancelled) return
        if (err instanceof ApiError && err.status === 404) {
          setStatus('notfound')
          setAgent(null)
          return
        }
        setStatus('error')
        setError(err instanceof Error ? err.message : 'Failed to load agent')
      })

    return () => {
      cancelled = true
    }
  }, [id])

  return (
    <main className="agents-page">
      <p>
        <Link to="/module/agents">Back to agents</Link>
      </p>

      {status === 'loading' && <p>Loading…</p>}
      {status === 'notfound' && <p>Agent not found</p>}
      {status === 'error' && error && <p role="alert">{error}</p>}

      {status === 'success' && agent && (
        <>
          <h1>{agent.name}</h1>
          <dl className="agents-detail">
            <dt>Department</dt>
            <dd>{agent.department}</dd>
            <dt>Job title</dt>
            <dd>{agent.jobTitle}</dd>
            <dt>Id</dt>
            <dd>{agent.identityId}</dd>
            <dt>System prompt</dt>
            <dd>
              <pre className="agents-system-prompt">{agent.systemPrompt}</pre>
            </dd>
          </dl>
        </>
      )}
    </main>
  )
}
```

Note: the mocked `ApiError` in the test must be the same class `instanceof` checks against. Prefer importing the real `ApiError` in the test and only mocking `getAgent`:

If Step 4 fails on `instanceof ApiError`, change the mock to:

```ts
vi.mock('../modules/agents/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../modules/agents/api')>()
  return {
    ...actual,
    getAgent: vi.fn(),
  }
})
```

and keep `new ApiError(404, 'Not Found')` from the real export.

- [ ] **Step 4: Run tests — expect PASS**

```cmd
cd frontend
npm test -- src/__tests__/AgentDetailPage.test.tsx
```

Expected: PASS.

- [ ] **Step 5: Commit**

```cmd
git add frontend/src/modules/agents/AgentDetailPage.tsx frontend/src/__tests__/AgentDetailPage.test.tsx
git commit -m "feat: add agent detail page"
```

---

### Task 4: Routes, registry, App shell

**Files:**
- Create: `frontend/src/modules/agents/routes.tsx`
- Create: `frontend/src/modules/index.ts`
- Modify: `frontend/src/App.tsx` (replace Vite starter)
- Modify: `frontend/src/__tests__/App.test.tsx`
- Modify: `frontend/src/modules/agents/agents.css` (optional polish only)
- Optionally remove unused starter assets imports from old `App` (do not delete asset files unless unused and requested)

**Interfaces:**
- Consumes: `AgentsListPage`, `AgentDetailPage`
- Produces:
  - `export const agentsRoutes: RouteObject[]` from `routes.tsx`
  - `export const moduleRoutes: RouteObject[]` from `modules/index.ts` (includes `...agentsRoutes`)
  - `export function AppRoutes(): JSX.Element` (routes only, for tests)
  - default `App` wraps `AppRoutes` in `BrowserRouter`

- [ ] **Step 1: Write failing App redirect test**

Replace `frontend/src/__tests__/App.test.tsx` with:

```tsx
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../App'

vi.mock('../modules/agents/api', () => ({
  searchAgents: vi.fn(),
  getAgent: vi.fn(),
  ApiError: class ApiError extends Error {
    status: number
    constructor(status: number, message: string) {
      super(message)
      this.name = 'ApiError'
      this.status = status
    }
  },
}))

describe('AppRoutes', () => {
  it('redirects / to /module/agents', async () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <AppRoutes />
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'Agents' })).toBeInTheDocument()
    expect(screen.getByText(/enter at least one filter/i)).toBeInTheDocument()
  })
})
```

- [ ] **Step 2: Run tests — expect FAIL**

```cmd
cd frontend
npm test -- src/__tests__/App.test.tsx
```

Expected: FAIL (`AppRoutes` not exported / old App counter UI).

- [ ] **Step 3: Implement routes + registry + App**

Create `frontend/src/modules/agents/routes.tsx`:

```tsx
import type { RouteObject } from 'react-router-dom'
import AgentDetailPage from './AgentDetailPage'
import AgentsListPage from './AgentsListPage'

export const agentsRoutes: RouteObject[] = [
  { path: '/module/agents', element: <AgentsListPage /> },
  { path: '/module/agents/:id', element: <AgentDetailPage /> },
]
```

Create `frontend/src/modules/index.ts`:

```ts
import type { RouteObject } from 'react-router-dom'
import { agentsRoutes } from './agents/routes'

export const moduleRoutes: RouteObject[] = [...agentsRoutes]
```

Replace `frontend/src/App.tsx` with:

```tsx
import { BrowserRouter, Navigate, useRoutes } from 'react-router-dom'
import { moduleRoutes } from './modules'

export function AppRoutes() {
  const element = useRoutes([
    { path: '/', element: <Navigate to="/module/agents" replace /> },
    ...moduleRoutes,
  ])
  return element
}

export default function App() {
  return (
    <BrowserRouter>
      <AppRoutes />
    </BrowserRouter>
  )
}
```

Leave `main.tsx` importing default `App` unchanged.

- [ ] **Step 4: Run full test suite — expect PASS**

```cmd
cd frontend
npm test
```

Expected: all tests PASS (api, list, detail, App).

- [ ] **Step 5: Manual smoke (optional but recommended)**

1. Start backend on `5296`.
2. `cd frontend` → `npm run dev`.
3. Open `/` → lands on Agents empty state.
4. Search `name=Leo` (or a seeded agent) → row → detail shows system prompt.
5. Open `/module/agents/does-not-exist` → not found.

- [ ] **Step 6: Commit**

```cmd
git add frontend/src/modules/agents/routes.tsx frontend/src/modules/index.ts frontend/src/App.tsx frontend/src/__tests__/App.test.tsx frontend/src/modules/agents/agents.css
git commit -m "feat: wire agents module routes into app shell"
```

---

## Spec coverage checklist

| Spec requirement | Task |
|------------------|------|
| Code under `frontend/src/modules/agents/` | 1–4 |
| Registry `modules/index.ts` | 4 |
| Routes `/module/agents`, `/module/agents/:id` | 4 |
| `/` → `/module/agents` | 4 |
| Three filters + Search submit | 2 |
| Empty filters → no fetch | 2 |
| `searchAgents` / `getAgent` relative URLs | 1 |
| Detail fields + system prompt | 3 |
| Inline errors / 404 not-found | 2–3 |
| Vitest tests for list, detail, App | 2–4 |
| `react-router-dom` dependency | 1 |
| No backend / React Query / chat | respected (out of scope) |

## Plan self-review notes

- No TBD placeholders; commit steps use Windows-friendly CMD.
- `ApiError` `instanceof` across vi.mock boundaries called out with `importOriginal` fallback.
- Types align across tasks: `AgentDto`, `AgentDetail`, `AgentsPage`, `AgentSearchFilters`.
- `GET /api/agents` (list-all) intentionally unused per spec.
