using Core;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Core.Unit;

public sealed class ModuleDiscoveryTests
{
    [Fact]
    public void DiscoverTypes_ConcreteAndAbstractTypes_ActivatesOnlyConcrete()
    {
        var types = new[] { typeof(ValidTestModule), typeof(AbstractTestModule) };

        var modules = ModuleDiscovery.DiscoverTypes(types);

        Assert.Contains(modules, static module => module is ValidTestModule);
        Assert.DoesNotContain(modules, static module => module is AbstractTestModule);
    }

    [Fact]
    public void DiscoverTypes_ValidModule_RegistersModuleServices()
    {
        var modules = ModuleDiscovery.DiscoverTypes([typeof(ValidTestModule)]);
        var services = new ServiceCollection();

        foreach (var module in modules)
        {
            module.RegisterServices(services);
        }

        var provider = services.BuildServiceProvider();
        var marker = provider.GetRequiredService<ValidTestModuleMarker>();

        Assert.NotNull(marker);
    }

    [Fact]
    public void DiscoverTypes_TypeWithoutParameterlessCtor_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ModuleDiscovery.DiscoverTypes([typeof(BrokenTestModule)]));

        Assert.Contains(nameof(BrokenTestModule), exception.Message, StringComparison.Ordinal);
    }
}

public sealed class ModuleCollectionTests
{
    [Fact]
    public void GetEnumerator_SubstitutedModules_YieldsAllModules()
    {
        var first = Substitute.For<IModule>();
        var second = Substitute.For<IModule>();
        var modules = new[] { first, second };
        var collection = new ModuleCollection(modules);

        var enumerated = collection.ToList();

        Assert.Equal(2, enumerated.Count);
        Assert.Same(first, enumerated[0]);
        Assert.Same(second, enumerated[1]);
    }
}
