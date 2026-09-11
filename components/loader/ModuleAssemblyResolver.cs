using System.Reflection;
using System.Runtime.Loader;

namespace STS2RitsuLib.Loader
{
    internal sealed class ModuleAssemblyResolver(AssemblyLoadContext context, BundleLayout layout)
    {
        private readonly Dictionary<string, BundleModule> _modules =
            layout.Modules.ToDictionary(module => module.Name, StringComparer.OrdinalIgnoreCase);

        private readonly Lock _sync = new();

        internal void Install()
        {
            context.Resolving += Resolve;
        }

        internal Assembly Load(string name)
        {
            lock (_sync)
            {
                var module = _modules[name];
                var existing = context.Assemblies.SingleOrDefault(assembly =>
                    string.Equals(assembly.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                    return context.LoadFromAssemblyPath(module.Path);
                if (!string.Equals(existing.GetName().FullName, module.Identity.FullName, StringComparison.Ordinal))
                    throw new FileLoadException($"A conflicting assembly is already loaded: {name}");
                if (name.StartsWith("STS2-RitsuLib", StringComparison.Ordinal) &&
                    !string.Equals(Path.GetFullPath(existing.Location), module.Path,
                        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    throw new FileLoadException(
                        $"RitsuLib was already loaded from another installation: {existing.Location}");
                return existing;
            }
        }

        private Assembly? Resolve(AssemblyLoadContext _, AssemblyName requested)
        {
            lock (_sync)
            {
                if (requested.Name == null || !_modules.TryGetValue(requested.Name, out var module))
                    return null;
                if (requested.Version != null && requested.Version > module.Identity.Version)
                    throw new FileLoadException(
                        $"The installed RitsuLib module is older than the requested reference: {requested}");
                return Load(module.Name);
            }
        }
    }
}
