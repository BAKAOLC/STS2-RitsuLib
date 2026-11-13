using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;

namespace STS2RitsuLib.Patching.Core
{
    internal static class PatchInliningGuard
    {
        private static readonly Lock Gate = new();
        private static readonly Func<MethodBase, bool> DisableInlining = CreateInliningDisabler();

        internal static void Prepare(MethodBase target)
        {
            lock (Gate)
            {
                if (!DisableInlining(target))
                    throw new PlatformNotSupportedException(
                        $"The Harmony runtime cannot preserve the patch boundary for {target.DeclaringType?.FullName}.{target.Name}.");
            }
        }

        private static Func<MethodBase, bool> CreateInliningDisabler()
        {
            const string platformTypeName = "MonoMod.Core.Platforms.PlatformTriple";
            var harmonyAssembly = typeof(Harmony).Assembly;
            var platformType = harmonyAssembly.GetType(platformTypeName);
            if (platformType == null)
            {
                var loadContext = AssemblyLoadContext.GetLoadContext(harmonyAssembly) ?? AssemblyLoadContext.Default;
                var monoModAssembly = loadContext.LoadFromAssemblyName(new("MonoMod.Core"));
                platformType = monoModAssembly.GetType(platformTypeName, throwOnError: true)!;
            }

            var platform = platformType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null);
            var method = platformType.GetMethod("TryDisableInlining", [typeof(MethodBase)])
                         ?? throw new MissingMethodException(platformType.FullName, "TryDisableInlining");
            return method.CreateDelegate<Func<MethodBase, bool>>(platform);
        }
    }
}
