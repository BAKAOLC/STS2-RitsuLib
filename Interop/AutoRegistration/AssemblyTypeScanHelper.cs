using System.Reflection;
using MegaCrit.Sts2.Core.Logging;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    internal static class AssemblyTypeScanHelper
    {
        public static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly, Logger logger)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentNullException.ThrowIfNull(logger);

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions.Where(static e => e != null))
                    logger.Warn(
                        $"[AutoRegister] Loader exception while scanning {assembly.FullName}: {loaderException!.Message}");

                return ex.Types.Where(static t => t != null).Cast<Type>().ToArray();
            }
        }
    }
}
