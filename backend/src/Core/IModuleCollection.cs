using System.Collections;

namespace Core;

public interface IModuleCollection : IEnumerable<IModule>;

public class ModuleCollection : IModuleCollection
{
    private readonly List<IModule> _modules = [];

    public ModuleCollection(IEnumerable<IModule> modules)
    {
        _modules.AddRange(modules);
    }

    public IEnumerator<IModule> GetEnumerator() => _modules.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}