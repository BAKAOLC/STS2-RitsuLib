using System.Reflection;
using HarmonyLib;
using STS2RitsuLib.Interop.AutoRegistration;
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

        private static readonly Dictionary<string, Assembly> RegisteredAssembliesByModId =
            new(StringComparer.Ordinal);

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

        /// <summary>
        ///     Registers a mod assembly for the one-shot discovery pipeline. Mods should call this from their initializer
        ///     before <see cref="ModTypeDiscoveryPatch" /> runs.
        /// </summary>
        public static void RegisterModAssembly(string modId, Assembly assembly)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(assembly);

            lock (Gate)
            {
                RegisteredAssembliesByModId[modId] = assembly;
            }
        }

        internal static void EnsureBuiltInContributorsRegistered()
        {
            lock (Gate)
            {
                if (_builtInsRegistered)
                    return;
                Contributors.Add(new ModInteropTypeDiscoveryContributor());
                Contributors.Add(new AttributeAutoRegistrationTypeDiscoveryContributor());
                _builtInsRegistered = true;
            }
        }

        internal static void RunOnce(Harmony harmony)
        {
            Dictionary<string, Assembly> map;
            IModTypeDiscoveryContributor[] snapshot;
            lock (Gate)
            {
                map = new(RegisteredAssembliesByModId, StringComparer.Ordinal);
                snapshot = Contributors.ToArray();
            }

            foreach (var assembly in map.Values.Distinct())
            foreach (var modType in AssemblyTypeScanHelper.GetLoadableTypes(assembly, RitsuLibFramework.Logger))
            foreach (var contributor in snapshot)
                contributor.Contribute(harmony, map, modType);
        }
    }
}
