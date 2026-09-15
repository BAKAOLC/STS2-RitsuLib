using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Logging;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    internal static class AssemblyTypeScanHelper
    {
        private static readonly ConditionalWeakTable<Assembly, TypeCache> Cache = new();

        public static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly, Logger logger)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentNullException.ThrowIfNull(logger);

            if (assembly.IsDynamic)
                return Scan(assembly, logger, out _);
            var cache = Cache.GetValue(assembly, static _ => new());
            lock (cache.Gate)
            {
                if (cache.Types != null)
                    return cache.Types;
                var types = Scan(assembly, logger, out var complete);
                if (complete)
                    cache.Types = types;
                return types;
            }
        }

        private static IReadOnlyList<Type> Scan(Assembly assembly, Logger logger, out bool complete)
        {
            try
            {
                var types = Array.AsReadOnly(assembly.GetTypes());
                complete = true;
                return types;
            }
            catch (ReflectionTypeLoadException ex)
            {
                complete = false;
                foreach (var loaderException in ex.LoaderExceptions.Where(static e => e != null))
                    logger.Warn(
                        $"[AutoRegister] Loader exception while scanning {assembly.FullName}: {loaderException!.Message}");

                return [.. ex.Types.Where(static t => t != null).Cast<Type>()];
            }
        }

        private sealed class TypeCache
        {
            internal Lock Gate { get; } = new();
            internal IReadOnlyList<Type>? Types { get; set; }
        }
    }
}
