using System.Reflection;
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
            var platformType = typeof(Harmony).Assembly.GetType("MonoMod.Core.Platforms.PlatformTriple", true)!;
            var platform = platformType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null);
            var method = platformType.GetMethod("TryDisableInlining", [typeof(MethodBase)])
                         ?? throw new MissingMethodException(platformType.FullName, "TryDisableInlining");
            return method.CreateDelegate<Func<MethodBase, bool>>(platform);
        }
    }
}
