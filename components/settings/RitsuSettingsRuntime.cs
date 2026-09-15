using System.Reflection;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Settings
{
    internal interface IRitsuSettingsHost
    {
        bool TryOpenSettings(out RitsuModSettingsSubmenu submenu, out string error);
        IEnumerable<Mod> EnumerateModsForManifestLookup();
        bool TryGetBestModPresentationInfo(string modId, out RitsuModPresentationInfo? info);
        bool TryGetBestModPresentationInfoForAssembly(Assembly assembly, out RitsuModPresentationInfo? info);
        string NormalizePublicStem(string value);
        void EnsureFrameworkPagesRegistered();
        void RefreshDynamicPages();
    }

    internal static class RitsuSettingsRuntime
    {
        private static IRitsuSettingsHost? _host;

        private static IRitsuSettingsHost Host => Volatile.Read(ref _host) ??
                                                  throw new InvalidOperationException(
                                                      "The RitsuLib runtime has not attached its settings services.");

        internal static void Attach(IRitsuSettingsHost host)
        {
            ArgumentNullException.ThrowIfNull(host);
            var existing = Interlocked.CompareExchange(ref _host, host, null);
            if (existing != null && !ReferenceEquals(existing, host))
                throw new InvalidOperationException("RitsuLib settings services already belong to another runtime.");
        }

        internal static bool TryOpenSettings(out RitsuModSettingsSubmenu submenu, out string error)
        {
            return Host.TryOpenSettings(out submenu, out error);
        }

        internal static IEnumerable<Mod> EnumerateModsForManifestLookup()
        {
            return Host.EnumerateModsForManifestLookup();
        }

        internal static bool TryGetBestModPresentationInfo(string modId, out RitsuModPresentationInfo? info)
        {
            return Host.TryGetBestModPresentationInfo(modId, out info);
        }

        internal static bool
            TryGetBestModPresentationInfoForAssembly(Assembly assembly, out RitsuModPresentationInfo? info)
        {
            return Host.TryGetBestModPresentationInfoForAssembly(assembly, out info);
        }

        internal static string NormalizePublicStem(string value)
        {
            return Host.NormalizePublicStem(value);
        }

        internal static void EnsureFrameworkPagesRegistered()
        {
            Host.EnsureFrameworkPagesRegistered();
        }

        internal static void RefreshDynamicPages()
        {
            Host.RefreshDynamicPages();
        }
    }
}
