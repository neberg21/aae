# Backend Module Host Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the ASP.NET Core host auto-discover `Module.*` projects via `IModule`, ship a `Module.Demo` ping proof, and align living docs to the new naming.

**Architecture:** `Service` references module projects explicitly (static container). At startup `Core` loads referenced `Module.*` assemblies, activates `IModule` implementations, registers their DI services and MVC application parts, then maps Minimal API endpoints. `Program.cs` stays free of module type names.

**Tech Stack:** .NET 10, ASP.NET Core (`Microsoft.AspNetCore.App`), xUnit, NSubstitute, `Microsoft.AspNetCore.Mvc.Testing`, existing `backend/Service.slnx`.

**Spec:** `docs/superpowers/specs/2026-07-21-backend-module-host-design.md`

## Global Constraints

- Module projects/assemblies named `Module.*` only — never `AAE.Modules.*`
- Keep host/core project names `Service` and `Core`
- No per-module lines or module type names in `Program.cs`
- Specialists may add `ProjectReference` on `Service` (+ `Service.slnx` entry); must not edit discovery in `Core` or bootstrap shape in `Program.cs`
- Remove manual `AddModule<T>` path; discovery is the only registration mechanism
- Unit test projects named `{Project}.Unit` under `backend/tests/` (e.g. `Core.Unit`, `Service.Unit`)
- Test method names: `methodName_scenario_expectedOutcome`
- Test framework: xUnit; mocking: NSubstitute (prefer substitutes over hand-rolled fakes when the SUT takes interfaces)
- C#: no primary constructors; create objects in locals before passing into methods/ctors
- Windows host: use CMD for shell steps; no PowerShell/bash scripts
- Do not implement frontend registry, middleware hooks, or runtime plugin folders

---

## File structure

| File | Responsibility |
|------|----------------|
| `backend/src/Core/IModule.cs` | Module contract (`RegisterServices`, `MapEndpoints`) |
| `backend/src/Core/IModuleCollection.cs` | Enumerable module collection + `ModuleCollection` |
| `backend/src/Core/ModuleDiscovery.cs` | Load `Module.*` refs; find/activate `IModule` types |
| `backend/src/Core/DependencyInjection.cs` | `AddCore` discovery + DI + MVC application parts |
| `backend/src/Core/ModuleEndpointRouteBuilderExtensions.cs` | `MapModules` → controllers + module endpoints |
| `backend/src/Core/Core.csproj` | Shared framework reference for ASP.NET abstractions |
| `backend/src/Module.Demo/Module.Demo.csproj` | Proof module project |
| `backend/src/Module.Demo/DemoModule.cs` | `IModule` with `/demo/ping` |
| `backend/src/Service/Service.csproj` | `ProjectReference` to `Core` + `Module.Demo` |
| `backend/src/Service/Program.cs` | Module-agnostic host bootstrap |
| `backend/src/Service/Program.Partial.cs` | `public partial class Program` for `WebApplicationFactory` |
| `backend/Service.slnx` | Include `Module.Demo` + test project |
| `backend/tests/Core.Unit/Core.Unit.csproj` | Unit tests for discovery |
| `backend/tests/Core.Unit/ModuleDiscoveryTests.cs` | Discovery activation tests |
| `backend/tests/Core.Unit/TestModules.cs` | Concrete `IModule` types for `Activator` (cannot substitute constructible types) |
| `backend/tests/Service.Unit/Service.Unit.csproj` | Host tests (`WebApplicationFactory`) |
| `backend/tests/Service.Unit/DemoPingTests.cs` | `/demo/ping` end-to-end |
| `README.md` | Naming: `Module.[Name]` |
| `docs/aae-architectutre.html` | §3.1 + Dockerfile samples aligned to real layout |
| `docs/process/organigramm.md` | Specialist path → `Module.Dnd` |
| `agents/identities/leo.md` | Module path examples |
| `agents/identities/helga.md` | Module path examples |
| `agents/identities/template_domain-supervisor.md` | Module path + Program.cs taboo wording |

---

### Task 1: Core `IModule` contract + discovery (unit-tested)

**Files:**
- Modify: `backend/src/Core/Core.csproj`
- Modify: `backend/src/Core/IModule.cs`
- Modify: `backend/src/Core/IModuleCollection.cs` (keep `ModuleCollection` as-is unless ctor needs no change)
- Create: `backend/src/Core/ModuleDiscovery.cs`
- Modify: `backend/src/Core/DependencyInjection.cs`
- Create: `backend/src/Core/ModuleEndpointRouteBuilderExtensions.cs`
- Create: `backend/tests/Core.Unit/Core.Unit.csproj`
- Create: `backend/tests/Core.Unit/TestModules.cs`
- Create: `backend/tests/Core.Unit/ModuleDiscoveryTests.cs`
- Modify: `backend/Service.slnx`

**Interfaces:**
- Consumes: existing empty `IModule`, `IModuleCollection`, `ModuleCollection`
- Produces:
  - `IModule.RegisterServices(IServiceCollection services)`
  - `IModule.MapEndpoints(IEndpointRouteBuilder endpoints)`
  - `ModuleDiscovery.DiscoverTypes(IEnumerable<Type> moduleTypes) → IReadOnlyList<IModule>`
  - `ModuleDiscovery.Discover(IEnumerable<Assembly> assemblies) → IReadOnlyList<IModule>`
  - `ModuleDiscovery.DiscoverLoadedModules() → IReadOnlyList<IModule>`
  - `IServiceCollection.AddCore()` — discovers loaded modules, calls `RegisterServices`, registers `IModuleCollection`, calls `AddControllers` + application parts
  - `WebApplication.MapModules()` — `MapControllers()` then each module `MapEndpoints`

- [ ] **Step 1: Create the Core.Unit project and failing discovery tests**

Create `backend/tests/Core.Unit/Core.Unit.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="NSubstitute" Version="5.3.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Core\Core.csproj" />
  </ItemGroup>

</Project>
```

Create `backend/tests/Core.Unit/TestModules.cs`:

```csharp
using Core;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Unit;

public sealed class ValidTestModule : IModule
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<ValidTestModuleMarker>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/test-module/ping", () => "ok");
    }
}

public sealed class ValidTestModuleMarker
{
}

public abstract class AbstractTestModule : IModule
{
    public void RegisterServices(IServiceCollection services)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}

public sealed class BrokenTestModule : IModule
{
    public BrokenTestModule(string required)
    {
        _ = required;
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
```

Create `backend/tests/Core.Unit/ModuleDiscoveryTests.cs`:

```csharp
using Core;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Core.Unit;

public sealed class ModuleDiscoveryTests
{
    [Fact]
    public void DiscoverTypes_concreteAndAbstractTypes_activatesOnlyConcrete()
    {
        Type[] types = [typeof(ValidTestModule), typeof(AbstractTestModule)];

        IReadOnlyList<IModule> modules = ModuleDiscovery.DiscoverTypes(types);

        Assert.Contains(modules, static module => module is ValidTestModule);
        Assert.DoesNotContain(modules, static module => module is AbstractTestModule);
    }

    [Fact]
    public void DiscoverTypes_validModule_registersModuleServices()
    {
        IReadOnlyList<IModule> modules = ModuleDiscovery.DiscoverTypes([typeof(ValidTestModule)]);
        ServiceCollection services = new();

        foreach (IModule module in modules)
        {
            module.RegisterServices(services);
        }

        ServiceProvider provider = services.BuildServiceProvider();
        ValidTestModuleMarker marker = provider.GetRequiredService<ValidTestModuleMarker>();

        Assert.NotNull(marker);
    }

    [Fact]
    public void DiscoverTypes_typeWithoutParameterlessCtor_throwsInvalidOperationException()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => ModuleDiscovery.DiscoverTypes([typeof(BrokenTestModule)]));

        Assert.Contains(nameof(BrokenTestModule), exception.Message, StringComparison.Ordinal);
    }
}

public sealed class ModuleCollectionTests
{
    [Fact]
    public void GetEnumerator_substitutedModules_yieldsAllModules()
    {
        IModule first = Substitute.For<IModule>();
        IModule second = Substitute.For<IModule>();
        IModule[] modules = [first, second];
        ModuleCollection collection = new(modules);

        List<IModule> enumerated = collection.ToList();

        Assert.Equal(2, enumerated.Count);
        Assert.Same(first, enumerated[0]);
        Assert.Same(second, enumerated[1]);
    }
}
```

Note: `DiscoverTypes` must activate real concrete types via `Activator` — use `TestModules.cs` for those. Use NSubstitute for interface collaborators (as in `ModuleCollectionTests`).

- [ ] **Step 2: Run tests — expect fail (types missing)**

```cmd
dotnet test backend\tests\Core.Unit\Core.Unit.csproj --nologo
```

Expected: FAIL (compile errors: `ModuleDiscovery` / `IModule` members missing).

- [ ] **Step 3: Update `Core.csproj` for ASP.NET shared framework**

Replace `backend/src/Core/Core.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Implement `IModule`, discovery, DI, and `MapModules`**

Replace `backend/src/Core/IModule.cs`:

```csharp
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

public interface IModule
{
    void RegisterServices(IServiceCollection services);

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
```

Leave `IModuleCollection` / `ModuleCollection` as they are (constructor taking `IEnumerable<IModule>`).

Create `backend/src/Core/ModuleDiscovery.cs`:

```csharp
using System.Reflection;

namespace Core;

public static class ModuleDiscovery
{
    public static IReadOnlyList<IModule> DiscoverLoadedModules()
    {
        EnsureReferencedModuleAssembliesLoaded();

        IEnumerable<Assembly> moduleAssemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(static assembly =>
            {
                string? name = assembly.GetName().Name;
                return name is not null
                       && name.StartsWith("Module.", StringComparison.Ordinal);
            });

        return Discover(moduleAssemblies);
    }

    public static IReadOnlyList<IModule> Discover(IEnumerable<Assembly> assemblies)
    {
        IEnumerable<Type> moduleTypes = assemblies
            .Distinct()
            .SelectMany(static assembly => assembly.GetTypes())
            .Where(static type =>
                typeof(IModule).IsAssignableFrom(type)
                && type is { IsInterface: false, IsAbstract: false });

        return DiscoverTypes(moduleTypes);
    }

    public static IReadOnlyList<IModule> DiscoverTypes(IEnumerable<Type> moduleTypes)
    {
        List<IModule> modules = [];

        foreach (Type moduleType in moduleTypes.Distinct())
        {
            if (!typeof(IModule).IsAssignableFrom(moduleType)
                || moduleType.IsInterface
                || moduleType.IsAbstract)
            {
                continue;
            }

            object? instance;
            try
            {
                instance = Activator.CreateInstance(moduleType);
            }
            catch (Exception exception)
            {
                string message =
                    $"Failed to activate module type '{moduleType.FullName}'. " +
                    "Modules must have a public parameterless constructor.";
                throw new InvalidOperationException(message, exception);
            }

            if (instance is not IModule module)
            {
                continue;
            }

            modules.Add(module);
        }

        return modules;
    }

    private static void EnsureReferencedModuleAssembliesLoaded()
    {
        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is null)
        {
            return;
        }

        foreach (AssemblyName referenced in entryAssembly.GetReferencedAssemblies())
        {
            string? name = referenced.Name;
            if (name is null || !name.StartsWith("Module.", StringComparison.Ordinal))
            {
                continue;
            }

            Assembly.Load(referenced);
        }
    }
}
```

Replace `backend/src/Core/DependencyInjection.cs`:

```csharp
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        IReadOnlyList<IModule> modules = ModuleDiscovery.DiscoverLoadedModules();

        foreach (IModule module in modules)
        {
            module.RegisterServices(services);
        }

        ModuleCollection moduleCollection = new(modules);
        services.AddSingleton<IModuleCollection>(moduleCollection);

        IMvcBuilder mvcBuilder = services.AddControllers();
        mvcBuilder.ConfigureApplicationPartManager(manager =>
        {
            foreach (AssemblyPart part in GetModuleParts(modules))
            {
                bool alreadyAdded = manager.ApplicationParts
                    .OfType<AssemblyPart>()
                    .Any(existing => existing.Assembly == part.Assembly);

                if (!alreadyAdded)
                {
                    manager.ApplicationParts.Add(part);
                }
            }
        });

        return services;
    }

    private static IEnumerable<AssemblyPart> GetModuleParts(IReadOnlyList<IModule> modules)
    {
        foreach (System.Reflection.Assembly assembly in modules
                     .Select(static module => module.GetType().Assembly)
                     .Distinct())
        {
            AssemblyPart part = new(assembly);
            yield return part;
        }
    }
}
```

Create `backend/src/Core/ModuleEndpointRouteBuilderExtensions.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

public static class ModuleEndpointRouteBuilderExtensions
{
    public static WebApplication MapModules(this WebApplication app)
    {
        app.MapControllers();

        IModuleCollection modules = app.Services.GetRequiredService<IModuleCollection>();
        foreach (IModule module in modules)
        {
            module.MapEndpoints(app);
        }

        return app;
    }
}
```

- [ ] **Step 5: Add Core.Unit to the solution and run unit tests**

Update `backend/Service.slnx` to:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/Core/Core.csproj" />
    <Project Path="src/Service/Service.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/Core.Unit/Core.Unit.csproj" />
  </Folder>
</Solution>
```

```cmd
dotnet test backend\tests\Core.Unit\Core.Unit.csproj --nologo
```

Expected: PASS (all four facts).

- [ ] **Step 6: Commit**

```cmd
git add backend\src\Core\Core.csproj backend\src\Core\IModule.cs backend\src\Core\ModuleDiscovery.cs backend\src\Core\DependencyInjection.cs backend\src\Core\ModuleEndpointRouteBuilderExtensions.cs backend\tests\Core.Unit\Core.Unit.csproj backend\tests\Core.Unit\TestModules.cs backend\tests\Core.Unit\ModuleDiscoveryTests.cs backend\Service.slnx
```

```cmd
(
echo feat: add Module.* discovery contract in Core
echo.
echo Introduce IModule registration/endpoints, assembly discovery, and unit tests.
) > %TEMP%\commitmsg.txt
git commit -F %TEMP%\commitmsg.txt
del %TEMP%\commitmsg.txt
```

---

### Task 2: `Module.Demo` + host wiring + Service.Unit test

**Files:**
- Create: `backend/src/Module.Demo/Module.Demo.csproj`
- Create: `backend/src/Module.Demo/DemoModule.cs`
- Modify: `backend/src/Service/Service.csproj`
- Modify: `backend/src/Service/Program.cs`
- Create: `backend/src/Service/Program.Partial.cs`
- Create: `backend/tests/Service.Unit/Service.Unit.csproj`
- Create: `backend/tests/Service.Unit/DemoPingTests.cs`
- Modify: `backend/Service.slnx`

**Interfaces:**
- Consumes: `AddCore()`, `MapModules()`, `IModule`
- Produces: `Module.Demo.DemoModule` mapping `GET /demo/ping` → `"pong"`; host serves it without naming `DemoModule` in `Program.cs`

- [ ] **Step 1: Add failing Service.Unit test project**

Create `backend/tests/Service.Unit/Service.Unit.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.9" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="NSubstitute" Version="5.3.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Service\Service.csproj" />
  </ItemGroup>

</Project>
```

Create `backend/tests/Service.Unit/DemoPingTests.cs`:

```csharp
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Service.Unit;

public sealed class DemoPingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DemoPingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAsync_demoPingPath_returnsPong()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/demo/ping");
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("pong", body);
    }
}
```

Create `backend/src/Service/Program.Partial.cs` (needed for `WebApplicationFactory<Program>`):

```csharp
public partial class Program
{
}
```

- [ ] **Step 2: Run integration test — expect fail**

```cmd
dotnet test backend\tests\Service.Unit\Service.Unit.csproj --nologo
```

Expected: FAIL (404 or missing `/demo/ping` — `Module.Demo` not wired yet). If the project fails to compile because `Program` is inaccessible, ensure `Program.Partial.cs` exists first, then re-run; failure mode should then be assertion/404, not compile.

- [ ] **Step 3: Create `Module.Demo` and wire Service**

Create `backend/src/Module.Demo/Module.Demo.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Core\Core.csproj" />
  </ItemGroup>

</Project>
```

Create `backend/src/Module.Demo/DemoModule.cs`:

```csharp
using Core;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Module.Demo;

public sealed class DemoModule : IModule
{
    public void RegisterServices(IServiceCollection services)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/demo/ping", () => "pong");
    }
}
```

Replace `backend/src/Service/Service.csproj` ItemGroup project references with:

```xml
  <ItemGroup>
    <ProjectReference Include="..\Core\Core.csproj" />
    <ProjectReference Include="..\Module.Demo\Module.Demo.csproj" />
  </ItemGroup>
```

Replace `backend/src/Service/Program.cs` with:

```csharp
using Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCore();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapModules();

app.Run();
```

Update `backend/Service.slnx` to:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/Core/Core.csproj" />
    <Project Path="src/Module.Demo/Module.Demo.csproj" />
    <Project Path="src/Service/Service.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/Core.Unit/Core.Unit.csproj" />
    <Project Path="tests/Service.Unit/Service.Unit.csproj" />
  </Folder>
</Solution>
```

- [ ] **Step 4: Run all backend tests — expect pass**

```cmd
dotnet test backend\Service.slnx --nologo
```

Expected: all tests PASS.

- [ ] **Step 5: Manual smoke check**

```cmd
dotnet run --project backend\src\Service\Service.csproj --no-launch-profile --urls http://127.0.0.1:5088
```

In a second terminal:

```cmd
curl -s http://127.0.0.1:5088/demo/ping
```

Expected: `pong`. Stop the server afterward (Ctrl+C).

Verify `Program.cs` contains no `Module.Demo` / `DemoModule` identifiers.

- [ ] **Step 6: Commit**

```cmd
git add backend\src\Module.Demo\Module.Demo.csproj backend\src\Module.Demo\DemoModule.cs backend\src\Service\Service.csproj backend\src\Service\Program.cs backend\src\Service\Program.Partial.cs backend\tests\Service.Unit\Service.Unit.csproj backend\tests\Service.Unit\DemoPingTests.cs backend\Service.slnx
```

```cmd
(
echo feat: wire Module.Demo ping via auto-discovery
echo.
echo Add proof module, host MapModules bootstrap, and integration test for /demo/ping.
) > %TEMP%\commitmsg.txt
git commit -F %TEMP%\commitmsg.txt
del %TEMP%\commitmsg.txt
```

---

### Task 3: Align living docs to `Module.*` naming

**Files:**
- Modify: `README.md`
- Modify: `docs/aae-architectutre.html`
- Modify: `docs/process/organigramm.md`
- Modify: `agents/identities/leo.md`
- Modify: `agents/identities/helga.md`
- Modify: `agents/identities/template_domain-supervisor.md`

**Interfaces:**
- Consumes: naming decisions from the spec
- Produces: living docs that instruct `Module.[Name]` / `backend/src/Module.*` — not `AAE.Modules.*`

- [ ] **Step 1: Update root README architecture principles**

In `README.md`, replace the architecture principles bullet list with:

```markdown
- **Static container / dynamic module integration** — agents add feature modules without rewriting core bootstrap.
- Backend modules: `Module.[Name]` under `backend/src/` (e.g. `Module.Demo`); specialists may add a `ProjectReference` on `Service` but must not modify `Program.cs` or `Core` discovery.
- Frontend modules: `frontend/src/modules/[name]` (target); global shell stays thin and registry-driven.
- Orchestration and sync: n8n as event bus; Flowise for LLM/agent flows; workflow definitions versioned in-repo where possible.
```

Also update the Status section if it still says module paths are only a target pattern for backend — backend modules now exist (`Module.Demo`). Keep frontend as target pattern.

- [ ] **Step 2: Update architecture HTML backend samples**

In `docs/aae-architectutre.html`:

1. If a table cell still shows `AAE.WebApplication` as the .NET app name (around the overview table), change it to `backend` / `Service`.

2. Replace the §3.1 directory tree `<pre><code>...` block with:

```html
        <pre><code>backend/
├── Service.slnx
└── src/
    ├── Core/                  <span class="tok-cm"># IModule contract + discovery</span>
    ├── Module.Demo/           <span class="tok-cm"># Proof / example feature module</span>
    ├── Module.Dnd/            <span class="tok-cm"># Example domain module (when added)</span>
    └── Service/               <span class="tok-cm"># Host entrypoint (Program.cs)</span>
        ├── Program.cs
        └── appsettings.json</code></pre>
```

3. Replace the Program.cs sample so it matches auto-discovery (no per-module DI lines), preserving the HTML token spans style used in neighboring samples:

```html
        <pre><code><span class="tok-kw">using</span> Core;

<span class="tok-kw">var</span> builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCore(); <span class="tok-cm">// discovers Module.* assemblies via ProjectReference</span>

<span class="tok-kw">var</span> app = builder.Build();

<span class="tok-kw">if</span> (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapModules(); <span class="tok-cm">// MapControllers + each IModule.MapEndpoints</span>

app.Run();</code></pre>
```

4. Replace Dockerfile backend stage paths to the real layout (keep frontend stage as-is). Use:

```dockerfile
COPY Service.slnx ./
COPY src/Core/*.csproj ./src/Core/
COPY src/Service/*.csproj ./src/Service/
COPY src/Module.Demo/*.csproj ./src/Module.Demo/
RUN dotnet restore Service.slnx

COPY . ./
RUN dotnet publish src/Service/Service.csproj -c Release -o /app/publish --no-restore
```

And entrypoint:

```dockerfile
ENTRYPOINT ["dotnet", "Service.dll"]
```

Apply these inside the existing HTML `<pre><code>` Dockerfile panel (escape `<` as `&lt;` if you add any generics; paths above need no generics). Update surrounding `COPY AAE.WebApplication/...` lines accordingly so no `AAE.Modules.*` / `AAE.Web` remain in that panel.

- [ ] **Step 3: Update organigramm + agent identities**

`docs/process/organigramm.md` — change the backend specialist line to:

```markdown
   ├──► [ D&D Backend Spezialist ] (Arbeitet nur in Module.Dnd)
```

`agents/identities/leo.md`:

- In Kern-Philosophie: replace `` `AAE.Modules.[Name]` im Backend `` with `` `Module.[Name]` im Backend (`backend/src/Module.[Name]`) ``
- In JSON example `module_scope`: use `'Module.Dnd'` instead of `'AAE.Modules.Dnd'`

`agents/identities/helga.md`:

- Isolation rule example: `` `Module.[Name]` `` instead of `` `AAE.Modules.[Name]` ``

`agents/identities/template_domain-supervisor.md`:

- Backend path: `` `backend/src/Module.{{Domain_Name}}/` ``
- Taboo line: `` `backend/src/Service/Program.cs` `` instead of `` `AAE.Web/Program.cs` ``

- [ ] **Step 4: Grep living docs for leftover `AAE.Modules`**

```cmd
rg "AAE\.Modules|AAE\.WebApplication|AAE\.Web/|AAE\.Core" README.md docs\aae-architectutre.html docs\process agents\identities
```

Expected: no matches in those living paths. (Historical files under `docs/superpowers/` may still mention old names — leave them.)

- [ ] **Step 5: Commit**

```cmd
git add README.md docs\aae-architectutre.html docs\process\organigramm.md agents\identities\leo.md agents\identities\helga.md agents\identities\template_domain-supervisor.md
```

```cmd
(
echo docs: rename module paths to Module.* in living docs
echo.
echo Align README, architecture HTML, organigramm, and agent identities with the host design.
) > %TEMP%\commitmsg.txt
git commit -F %TEMP%\commitmsg.txt
del %TEMP%\commitmsg.txt
```

---

## Plan self-review

| Spec requirement | Task |
|------------------|------|
| `Module.*` naming, keep `Core`/`Service` | Tasks 2–3 |
| Explicit `ProjectReference` loading | Task 2 |
| Auto-discovery, no per-module `Program.cs` lines | Tasks 1–2 |
| DI + controllers (application parts) + Minimal APIs | Task 1 (`AddCore` / `MapModules`) + Task 2 (`DemoModule`) |
| Remove `AddModule<T>` | Task 1 |
| `Module.Demo` `/demo/ping` | Task 2 |
| Fail fast on bad ctor; empty modules OK | Task 1 tests + discovery behavior |
| Living docs rename | Task 3 |
| Integration verification | Task 2 steps 4–5 |

No placeholders left. Method names consistent: `DiscoverTypes` / `Discover` / `DiscoverLoadedModules` / `AddCore` / `MapModules`. Unit tests call `DiscoverTypes` so `BrokenTestModule` does not poison assembly-wide discovery. Test projects are `{Project}.Unit`; test methods use `methodName_scenario_expectedOutcome`; mocking uses NSubstitute.
