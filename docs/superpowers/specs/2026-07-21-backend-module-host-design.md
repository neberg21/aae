# Design: Backend Module Host (Auto-Discovery)

**Date:** 2026-07-21  
**Status:** Approved for implementation planning  
**Scope:** Backend only — host + `Core` discovery + `Module.Demo` proof + docs rename

## Goal

Set up the ASP.NET Core service so new feature modules can be added without changing `Program.cs` bootstrap logic. Adding a module means: create a `Module.{Name}` project, implement `IModule`, add one `ProjectReference` on `Service`, rebuild.

## Decisions

| Topic | Choice |
|-------|--------|
| Scope | Backend only (no frontend registry) |
| Module naming | `Module.*` only — no `AAE.` / `AAE.Modules.` prefixes |
| Host / core naming | Keep existing `Service` and `Core` |
| Loading | Explicit `ProjectReference` on `Service` (static container) |
| Registration | Auto-discovery of loaded `Module.*` assemblies — no per-module lines in `Program.cs` |
| Module capabilities | DI services + MVC controllers + Minimal API endpoint mapping |
| HTTP route prefix | All module HTTP surface under `/api/{name}/...` (e.g. `/api/demo`, `/api/abc`) |
| OpenAPI | One OpenAPI document per module; document name = `IModule.Name`; served at `/openapi/{name}.json` |
| Proof module | `Module.Demo` with `Name = "demo"` and `GET /api/demo/ping` → `"pong"` |
| Middleware hooks | Out of scope |
| Runtime plugin folder | Out of scope |

## Architecture & layout

```
backend/
  Service.slnx
  src/
    Core/           # IModule contract + discovery / registration helpers
    Service/        # WebApplication host; Program.cs stays module-agnostic
    Module.Demo/    # Proof module
```

**Boundaries**

- Specialists own everything inside their `Module.*` project.
- Specialists may add a `ProjectReference` in `Service.csproj` (and the matching entry in `Service.slnx` if required for IDE/build).
- Specialists must not change discovery logic in `Core` or the bootstrap flow in `Program.cs`.

## Module contract & discovery

### `IModule` (in `Core`)

Each module exposes one (or more) concrete implementations with:

- `string Name { get; }` — short route/OpenAPI identity (e.g. `"demo"`, `"abc"`); must be URL-safe lowercase segment
- `void RegisterServices(IServiceCollection services)` — module DI registrations
- `void MapEndpoints(IEndpointRouteBuilder endpoints)` — Minimal API routes under `/api/{Name}/...`

Controllers: modules may include `[ApiController]` types. Routes must also live under `/api/{Name}/...`. The host adds each discovered module assembly as an MVC application part so controllers are found without listing them in `Program.cs`.

### HTTP & OpenAPI conventions

| Concern | Convention |
|---------|------------|
| REST / Minimal APIs | `/api/{Name}/...` only — never bare `/demo/...` |
| OpenAPI document name | Same as `IModule.Name` |
| OpenAPI URL | `/openapi/{Name}.json` (ASP.NET Core built-in multi-document OpenAPI) |
| Document contents | Only operations whose relative path starts with `api/{Name}` |

`AddCore` registers `AddOpenApi(module.Name, ...)` for each discovered module with a `ShouldInclude` filter on that path prefix. `UseCoreHost()` calls `MapOpenApi()` once for all registered documents. Do **not** register a single global default OpenAPI document in `Program.cs`.

### Discovery flow (in `Core`)

1. Enumerate loaded assemblies whose simple name matches `Module.*`.
2. Find concrete non-abstract types implementing `IModule`.
3. Activate via parameterless constructor.
4. Call `RegisterServices` for each; register per-module OpenAPI documents; keep instances for later endpoint mapping.
5. After `WebApplication` is built: `UseCoreHost()` maps OpenAPI (dev), controllers, and each module's `MapEndpoints`.

### `Program.cs` shape

Frozen host surface (never edited when adding a module):

```csharp
builder.Services.AddCore();   // discovery, DI, per-module OpenAPI registration, MVC parts
var app = builder.Build();
app.UseHttpsRedirection();
app.UseCoreHost();            // MapOpenApi + MapControllers + all IModule.MapEndpoints
app.Run();
```

`UseCoreHost()` is **host infrastructure**, not per-module wiring. Adding `Module.Foo` does **not** add any new line to `Program.cs` — only a `ProjectReference` on `Service` plus the module project.

No per-module `using`, no `AddModule<T>()`, no `MapOpenApi("foo")`, no `MapModules` for individual modules.

Remove the existing manual `AddModule<T>` registration path from `Core`; discovery is the only supported registration mechanism.

### `Module.Demo`

- Project/assembly name: `Module.Demo`
- `Name` → `"demo"`
- Maps `GET /api/demo/ping` returning `"pong"` (Minimal API)
- OpenAPI document `demo` at `/openapi/demo.json` includes that operation
- Referenced from `Service` via `ProjectReference`

## Error handling

| Case | Behavior |
|------|----------|
| No `Module.*` assemblies loaded | Host starts; empty module set; no OpenAPI documents |
| Assembly name matches `Module.*` but no `IModule` | Skip; optional debug log |
| Multiple `IModule` types in one assembly | Register all (each needs a distinct `Name`) |
| Duplicate `IModule.Name` across modules | Fail fast at startup with a clear exception |
| Missing parameterless ctor / activation failure | Fail fast at startup with a clear exception |
| Empty or invalid `Name` | Fail fast at startup with a clear exception |

## Docs updates (naming alignment)

Replace `AAE.Modules.*`, `AAE.Core`, `AAE.Web`, and similar blueprint names with the real layout (`Core`, `Service`, `Module.*`) in:

- Root `README.md` (architecture principles / backend module paths)
- `docs/aae-architectutre.html` (§3.1 tree, Program.cs sample, Dockerfile `COPY` / publish / entrypoint paths)
- `docs/process/organigramm.md` (specialist module path)
- `agents/identities/leo.md`, `helga.md`, `template_domain-supervisor.md` (module path examples and guardrails)

Historical plan/spec files under `docs/superpowers/` may keep old wording as archived decisions, or get a one-line note that naming was superseded by this spec — prefer updating only living docs agents and humans still follow (README, architecture HTML, process, identities).

## Testing

- With `Module.Demo` referenced: host discovers the module and `GET /api/demo/ping` succeeds.
- `GET /openapi/demo.json` returns 200 and describes `/api/demo/ping`.
- Optional: host without any module `ProjectReference` still builds and runs.
- Unit test projects: `{Project}.Unit` (xUnit + NSubstitute).
- Test method names: `MethodName_Scenario_ExpectedOutcome`.

## Non-goals

- Frontend `src/modules/` registry
- `IModule` middleware / pipeline hooks
- Runtime `AssemblyLoadContext` plugin folder
- Real D&D / Therapy domain logic
- Docker/Koyeb production packaging beyond documentation path fixes
- Wildcard MSBuild auto-include of all `Module.*` projects
- Single merged global OpenAPI document for all modules

## Verification

- `dotnet run --project backend\src\Service\Service.csproj` serves `/api/demo/ping` and `/openapi/demo.json`
- `Program.cs` contains no module-specific type names and no per-module Map/Add lines (only `AddCore` + `UseCoreHost`)
- New module recipe is: create `Module.{Name}` → implement `IModule` (`Name`, services, `/api/{Name}/...` endpoints) → `ProjectReference` on `Service` → rebuild
- Living docs no longer instruct agents to use `AAE.Modules.*`
