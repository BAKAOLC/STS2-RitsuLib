using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Interop.Patches;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Extensible pipeline invoked from <see cref="ModTypeDiscoveryPatch" /> (early localization init),
    ///     mirroring BaseLib's post-mod-init scan without hard-wiring a single feature.
    /// </summary>
    public static class ModTypeDiscoveryHub
    {
        private static readonly Lock Gate = new();
        private static readonly List<IModTypeDiscoveryContributor> Contributors = [];
        private static bool _builtInsRegistered;

        /// <summary>
        ///     Registers a contributor. Call from your mod initializer before framework patch application
        ///     if you rely on custom discovery; otherwise built-ins are registered from <see cref="RitsuLibFramework" />.
        /// </summary>
        public static void RegisterContributor(IModTypeDiscoveryContributor contributor)
        {
            ArgumentNullException.ThrowIfNull(contributor);
            lock (Gate)
            {
                Contributors.Add(contributor);
            }
        }

        internal static void EnsureBuiltInContributorsRegistered()
        {
            lock (Gate)
            {
                if (_builtInsRegistered)
                    return;
                Contributors.Add(new ModInteropTypeDiscoveryContributor());
                _builtInsRegistered = true;
            }
        }

        internal static void RunOnce(Harmony harmony)
        {
            var map = new Dictionary<string, Assembly>(StringComparer.Ordinal);
            foreach (var m in Sts2ModManagerCompat.EnumerateLoadedModsWithAssembly())
            {
                var id = m.manifest?.id;
                if (string.IsNullOrEmpty(id) || m.assembly is null)
                    continue;
                map[id] = m.assembly;
            }

            IModTypeDiscoveryContributor[] snapshot;
            lock (Gate)
            {
                snapshot = Contributors.ToArray();
            }

            foreach (var modType in ReflectionHelper.ModTypes)
            foreach (var c in snapshot)
                c.Contribute(harmony, map, modType);
        }
    }
}
