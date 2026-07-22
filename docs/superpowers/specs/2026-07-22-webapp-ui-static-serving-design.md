# Webapp UI static serving — Design

**Date:** 2026-07-22  
**Status:** Pending user review  
**Deliverable:** .NET host serves the Vite-built UI from `wwwroot`; local Vite proxies `/api` to the backend

## Goal

Finish the single-container webapp so the React UI is reachable in Docker/Koyeb, and local `npm run dev` can call the same relative `/api/...` paths.

## Decisions (locked)

| Topic | Choice |
|-------|--------|
| Hosting model | Single container: ASP.NET serves API + UI (Approach A) |
| Host wiring | `UseWebUi()` + `MapWebUiFallback()` in Core; thin `Program.cs` (Approach 1) |
| Local API access | Vite proxy `/api` → `http://localhost:5296` |
| API URL convention | Relative `/api/...` only; no `VITE_API_BASE_URL` |
| SPA deep links | `MapFallbackToFile("index.html")`, excluding `/api`, `/scalar`, `/openapi` |
| Missing `wwwroot` | Skip static/fallback silently (API-only local runs) |
| CORS | Not required (same origin in prod; Vite proxy in dev) |
| Dockerfile | Unchanged — already builds frontend and copies `dist` → `wwwroot` |

## Architecture

```text
Browser ──► ASP.NET (8080 in Docker / 5296 local)
              ├── /api/*        → modules (UseCoreHost)
              ├── /scalar/*     → API docs
              ├── /openapi/*    → OpenAPI docs
              └── /*            → wwwroot + SPA fallback → index.html

Local dev:
  Browser ──► Vite (UI)
                 └── /api/*  proxy ──► http://localhost:5296
```

## Components

### 1. Web UI host extensions (Core)

Two methods alongside `UseCoreHost` (split so `Use*` middleware stays before `Map*` endpoints):

**`UseWebUi()`** — middleware only

1. If `wwwroot` does not exist, no-op.
2. Otherwise: `UseDefaultFiles` → `UseStaticFiles`.

**`MapWebUiFallback()`** — endpoint only

1. If `wwwroot` does not exist, no-op.
2. Otherwise: `MapFallbackToFile("index.html")` that does **not** match paths starting with `/api`, `/scalar`, or `/openapi` (case-insensitive).

### 2. `Program.cs`

```csharp
app.UseHttpsRedirection();
app.UseWebUi();           // static middleware (before any Map*)
app.UseCoreHost();        // /api, /scalar, /openapi endpoints
app.MapWebUiFallback();   // SPA fallback last among endpoints
```

Order rationale: ASP.NET Core should not register `Use*` after `Map*`; API endpoint maps must be registered before the SPA fallback so `/api` wins.

### 3. Vite proxy (`frontend/vite.config.ts`)

```ts
server: {
  proxy: {
    '/api': 'http://localhost:5296',
  },
},
```

### 4. Docs

README Getting started: local UI needs the API on port `5296` for `/api` calls when using the proxy.

## Data flow / error handling

- Production: UI assets and API share one origin; relative fetches hit the same host.
- Development: Vite forwards `/api` to ASP.NET; non-API routes stay on Vite.
- Unknown UI paths: fall back to `index.html` (client router-ready).
- Unknown API paths under `/api`: remain API 404s (not SPA fallback).
- No `wwwroot`: host behaves as API-only; no startup failure.

## Testing

- Existing Service unit tests continue to pass (API-only, no `wwwroot` required).
- Add a host test: with a temp `wwwroot/index.html`, `GET /` returns 200 HTML; `GET /api/demo/ping` still returns the demo response.
- Manual / Docker: open `/` sees UI; `/api/demo/ping` works; a non-file UI path returns `index.html`.

## Out of scope

- Separate UI/nginx container or CDN
- Vite proxy for `/scalar` or `/openapi`
- Auth, cookies, CORS policies
- Cache headers for `index.html` / hashed assets
- Changing `UseHttpsRedirection` for reverse-proxy edge cases

## Implementation notes

- Keep `Program.cs` free of static-file details beyond `UseWebUi()` / `MapWebUiFallback()` (module agents must not need to edit it for UI hosting).
- Place both helpers in Core next to `UseCoreHost` so the Service project stays a thin host.
