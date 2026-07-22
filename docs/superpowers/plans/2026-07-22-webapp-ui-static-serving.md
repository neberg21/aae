# Webapp UI Static Serving Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the ASP.NET host serve the Vite-built UI from `wwwroot` (with SPA fallback), and proxy `/api` from Vite to the local backend so relative API URLs work in both environments.

**Architecture:** Core gains `UseWebUi()` (static middleware) and `MapWebUiFallback()` (SPA fallback endpoint). `Program.cs` calls them around `UseCoreHost()` so `Use*` stays before `Map*`. Dockerfile already copies `frontend/dist` → `wwwroot`. Local Vite proxies `/api` to `http://localhost:5296`.

**Tech Stack:** .NET 10, ASP.NET Core, xUnit, `Microsoft.AspNetCore.Mvc.Testing`, Vite 8, React frontend scaffold.

**Spec:** `docs/superpowers/specs/2026-07-22-webapp-ui-static-serving-design.md`

## Global Constraints

- Single container: ASP.NET serves API + UI; Dockerfile unchanged
- Host helpers: `UseWebUi()` + `MapWebUiFallback()` in Core next to `UseCoreHost`
- SPA fallback must not claim `/api`, `/scalar`, or `/openapi` (case-insensitive); those unmatched paths stay 404
- Missing `wwwroot` (or missing `index.html`): no-op — API-only host must not fail startup
- UI API calls: relative `/api/...` only; no `VITE_API_BASE_URL`
- Vite proxy target: `http://localhost:5296`
- No CORS, auth, CDN, or HTTPS-redirection changes
- C#: no primary constructors; create objects in locals before passing into methods/ctors; prefer `var` for locals when the type is clear from the RHS
- Test method names: `MethodName_Scenario_ExpectedOutcome`
- Windows host: use CMD for shell steps; no PowerShell/bash scripts

---

## File structure

| File | Responsibility |
|------|----------------|
| `backend/src/Core/WebUiHostExtensions.cs` | `UseWebUi`, `MapWebUiFallback`, SPA path predicate |
| `backend/src/Service/Program.cs` | Call `UseWebUi` → `UseCoreHost` → `MapWebUiFallback` |
| `backend/tests/Service.Unit/WebUi/WebUiHostTests.cs` | Host tests with temp `wwwroot` |
| `backend/tests/Service.Unit/WebUi/WebUiWebApplicationFactory.cs` | `WebApplicationFactory` that installs a temp web root |
| `frontend/vite.config.ts` | Dev proxy `/api` → backend |
| `README.md` | Note that local UI needs API on port 5296 for `/api` |

---

### Task 1: Serve UI from `wwwroot` + SPA fallback (TDD)

**Files:**
- Create: `backend/src/Core/WebUiHostExtensions.cs`
- Modify: `backend/src/Service/Program.cs`
- Create: `backend/tests/Service.Unit/WebUi/WebUiWebApplicationFactory.cs`
- Create: `backend/tests/Service.Unit/WebUi/WebUiHostTests.cs`

**Interfaces:**
- Consumes: `WebApplication`, `IWebHostEnvironment.WebRootPath`, existing `UseCoreHost`
- Produces:
  - `WebApplication UseWebUi(this WebApplication app)`
  - `WebApplication MapWebUiFallback(this WebApplication app)`
  - Internal helper used only by Core: path predicate for SPA fallback

- [ ] **Step 1: Write the failing host tests + factory**

Create `backend/tests/Service.Unit/WebUi/WebUiWebApplicationFactory.cs`:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Service.Unit.WebUi;

public sealed class WebUiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _webRootPath;

    public WebUiWebApplicationFactory()
    {
        _webRootPath = Path.Combine(Path.GetTempPath(), "aae-webui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_webRootPath);
        var indexPath = Path.Combine(_webRootPath, "index.html");
        File.WriteAllText(indexPath, "<!doctype html><html><title>AAE UI</title><body>ok</body></html>");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseWebRoot(_webRootPath);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        try
        {
            Directory.Delete(_webRootPath, recursive: true);
        }
        catch (IOException)
        {
            // best-effort cleanup for temp web root
        }
        catch (UnauthorizedAccessException)
        {
            // best-effort cleanup for temp web root
        }
    }
}
```

Create `backend/tests/Service.Unit/WebUi/WebUiHostTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace Service.Unit.WebUi;

public sealed class WebUiHostTests : IClassFixture<WebUiWebApplicationFactory>
{
    private readonly WebUiWebApplicationFactory _factory;

    public WebUiHostTests(WebUiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAsync_Root_ReturnsIndexHtml()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AAE UI", body, StringComparison.Ordinal);
        AssertMediaTypeIsHtml(response.Content.Headers.ContentType);
    }

    [Fact]
    public async Task GetAsync_UnknownUiPath_ReturnsIndexHtml()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/team/agents");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AAE UI", body, StringComparison.Ordinal);
        AssertMediaTypeIsHtml(response.Content.Headers.ContentType);
    }

    [Fact]
    public async Task GetAsync_DemoPing_StillReturnsPongWhenWebRootPresent()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/demo/ping");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("pong", body);
    }

    [Fact]
    public async Task GetAsync_UnknownApiPath_ReturnsNotFoundNotHtml()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/does-not-exist");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("AAE UI", body, StringComparison.Ordinal);
    }

    private static void AssertMediaTypeIsHtml(MediaTypeHeaderValue? contentType)
    {
        Assert.NotNull(contentType);
        Assert.Equal("text/html", contentType.MediaType, ignoreCase: true);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```cmd
dotnet test backend\tests\Service.Unit\Service.Unit.csproj --filter "FullyQualifiedName~WebUiHostTests"
```

Expected: FAIL (e.g. `/` is not 200 HTML — static UI not wired yet).

- [ ] **Step 3: Implement `WebUiHostExtensions`**

Create `backend/src/Core/WebUiHostExtensions.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Core;

public static class WebUiHostExtensions
{
    public static WebApplication UseWebUi(this WebApplication app)
    {
        if (!HasWebRoot(app))
        {
            return app;
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();
        return app;
    }

    public static WebApplication MapWebUiFallback(this WebApplication app)
    {
        if (!HasWebRoot(app))
        {
            return app;
        }

        var webRootPath = app.Environment.WebRootPath;
        var indexPath = Path.Combine(webRootPath, "index.html");
        if (!File.Exists(indexPath))
        {
            return app;
        }

        app.MapFallback(async context =>
        {
            if (!IsSpaFallbackCandidate(context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(indexPath);
        });

        return app;
    }

    internal static bool IsSpaFallbackCandidate(PathString path)
    {
        if (path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool HasWebRoot(WebApplication app)
    {
        var webRootPath = app.Environment.WebRootPath;
        return !string.IsNullOrWhiteSpace(webRootPath) && Directory.Exists(webRootPath);
    }
}
```

- [ ] **Step 4: Wire `Program.cs`**

Replace `backend/src/Service/Program.cs` with:

```csharp
using Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCore();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseWebUi();
app.UseCoreHost();
app.MapWebUiFallback();

app.Run();
```

- [ ] **Step 5: Run Web UI tests — expect PASS**

Run:

```cmd
dotnet test backend\tests\Service.Unit\Service.Unit.csproj --filter "FullyQualifiedName~WebUiHostTests"
```

Expected: all `WebUiHostTests` PASS.

- [ ] **Step 6: Run full Service.Unit suite — expect PASS**

Run:

```cmd
dotnet test backend\tests\Service.Unit\Service.Unit.csproj
```

Expected: all tests PASS (existing API-only factory has no `wwwroot` → helpers no-op).

- [ ] **Step 7: Commit**

```cmd
git add backend\src\Core\WebUiHostExtensions.cs backend\src\Service\Program.cs backend\tests\Service.Unit\WebUi\WebUiWebApplicationFactory.cs backend\tests\Service.Unit\WebUi\WebUiHostTests.cs
git commit -m "feat: serve Vite UI from wwwroot with SPA fallback"
```

---

### Task 2: Vite `/api` proxy + README note

**Files:**
- Modify: `frontend/vite.config.ts`
- Modify: `README.md` (Getting started → Frontend)

**Interfaces:**
- Consumes: backend http profile `http://localhost:5296` from `backend/src/Service/Properties/launchSettings.json`
- Produces: Vite `server.proxy['/api']` → that URL; README mentions API must be running for proxied calls

- [ ] **Step 1: Add Vite proxy**

Replace `frontend/vite.config.ts` with:

```ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5296',
    },
  },
})
```

- [ ] **Step 2: Update README Frontend section**

In `README.md`, under `### Frontend`, after the `npm run dev` block, add:

```markdown
For `/api` calls from the Vite dev server, run the backend on `http://localhost:5296` (default `http` launch profile). The Vite config proxies `/api` to that origin so the UI can use relative `/api/...` paths.
```

Keep the existing install/dev commands unchanged.

- [ ] **Step 3: Sanity-check Vite config loads**

Run:

```cmd
cd frontend
npm run build
```

Expected: build succeeds (proxy is server-only; production build unchanged).

- [ ] **Step 4: Commit**

```cmd
git add frontend\vite.config.ts README.md
git commit -m "chore: proxy Vite /api to local ASP.NET and document it"
```

---

## Manual verification (after both tasks)

Not automated in CI for this plan; run when convenient:

```cmd
dotnet run --project backend\src\Service\Service.csproj --launch-profile http
```

In another terminal:

```cmd
cd frontend
npm run dev
```

Then:

1. Open the Vite URL — UI loads.
2. Optionally `fetch('/api/demo/ping')` from the browser console — expect `pong`.
3. Docker (optional): build `infrastructure\webapp\Dockerfile` from repo root; open `/` and `/api/demo/ping` on port 8080.

---

## Spec coverage (self-review)

| Spec requirement | Task |
|------------------|------|
| `UseWebUi` + `MapWebUiFallback` in Core | Task 1 |
| `Program.cs` order: UseWebUi → UseCoreHost → MapWebUiFallback | Task 1 |
| SPA fallback excludes `/api`, `/scalar`, `/openapi` | Task 1 (`IsSpaFallbackCandidate` + unknown API test) |
| Missing `wwwroot` no-op | Task 1 (existing Service.Unit suite) |
| Host test: `/` HTML + `/api/demo/ping` | Task 1 |
| Vite `/api` → `http://localhost:5296` | Task 2 |
| Relative `/api` convention / README | Task 2 |
| Dockerfile unchanged | (no task — explicit non-change) |
| No CORS / auth / CDN | (out of scope) |
