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
| Proof module | `Module.Demo` with `GET /demo/ping` → `"pong"` |
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

- `void RegisterServices(IServiceCollection services)` — module DI registrations
- `void MapEndpoints(IEndpointRouteBuilder endpoints)` — Minimal API routes

Controllers: modules may include `[ApiController]` types. The host adds each discovered module assembly as an MVC application part so controllers are found without listing them in `Program.cs`.

### Discovery flow (in `Core`)

1. Enumerate loaded assemblies whose simple name matches `Module.*`.
2. Find concrete non-abstract types implementing `IModule`.
3. Activate via parameterless constructor.
4. Call `RegisterServices` for each; keep the instances for later endpoint mapping.
5. After `WebApplication` is built: `MapControllers()` and call `MapEndpoints` on each module.

### `Program.cs` shape

Conceptually: register controllers / OpenAPI / `AddCore()` (or equivalent that triggers discovery + service registration) → build → middleware → map controllers + module endpoints → `Run()`.

No per-module `using` and no `AddModule<T>()` calls in `Program.cs`.

Remove the existing manual `AddModule<T>` registration path from `Core`; discovery is the only supported registration mechanism.

### `Module.Demo`

- Project/assembly name: `Module.Demo`
- Implements `IModule`
- Maps `GET /demo/ping` returning `"pong"` (Minimal API)
- Referenced from `Service` via `ProjectReference`

## Error handling

| Case | Behavior |
|------|----------|
| No `Module.*` assemblies loaded | Host starts; empty module set |
| Assembly name matches `Module.*` but no `IModule` | Skip; optional debug log |
| Multiple `IModule` types in one assembly | Register all |
| Missing parameterless ctor / activation failure | Fail fast at startup with a clear exception |

## Docs updates (naming alignment)

Replace `AAE.Modules.*`, `AAE.Core`, `AAE.Web`, and similar blueprint names with the real layout (`Core`, `Service`, `Module.*`) in:

- Root `README.md` (architecture principles / backend module paths)
- `docs/aae-architectutre.html` (§3.1 tree, Program.cs sample, Dockerfile `COPY` / publish / entrypoint paths)
- `docs/process/organigramm.md` (specialist module path)
- `agents/identities/leo.md`, `helga.md`, `template_domain-supervisor.md` (module path examples and guardrails)

Historical plan/spec files under `docs/superpowers/` may keep old wording as archived decisions, or get a one-line note that naming was superseded by this spec — prefer updating only living docs agents and humans still follow (README, architecture HTML, process, identities).

## Testing

- With `Module.Demo` referenced: host discovers the module and `GET /demo/ping` succeeds.
- Optional: host without any module `ProjectReference` still builds and runs.
- Unit test projects: `{Project}.Unit` (xUnit + NSubstitute).
- Test method names: `methodName_scenario_expectedOutcome`.

## Non-goals

- Frontend `src/modules/` registry
- `IModule` middleware / pipeline hooks
- Runtime `AssemblyLoadContext` plugin folder
- Real D&D / Therapy domain logic
- Docker/Koyeb production packaging beyond documentation path fixes
- Wildcard MSBuild auto-include of all `Module.*` projects

## Verification

- `dotnet run --project backend\src\Service\Service.csproj` serves `/demo/ping`
- `Program.cs` contains no module-specific type names
- New module recipe is: create `Module.{Name}` → implement `IModule` → `ProjectReference` on `Service` → rebuild
- Living docs no longer instruct agents to use `AAE.Modules.*`
