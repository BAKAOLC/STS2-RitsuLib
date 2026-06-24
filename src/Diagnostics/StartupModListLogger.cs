using System.Text;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Diagnostics
{
    internal static class StartupModListLogger
    {
        private const string Prefix = "[ModList]";
        private static readonly Lock SyncRoot = new();
        private static bool _initialized;
        private static bool _logged;

        internal static void Initialize()
        {
            lock (SyncRoot)
            {
                if (_initialized)
                    return;

                _initialized = true;
            }

            ModManager.OnModDetected += OnModDetected;
            TryLogAfterInitialModLoading();
            RitsuLibFramework.SubscribeLifecycleOnce<MainMenuReadyEvent>(_ => TryLogStartupModList());
        }

        private static void OnModDetected(Mod _)
        {
            TryLogAfterInitialModLoading();
        }

        private static void TryLogAfterInitialModLoading()
        {
            IReadOnlyList<Sts2ModInventoryEntry> registeredMods;
            try
            {
                registeredMods = Sts2ModManagerCompat.BuildModInventoryEntries();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"{Prefix} Failed to inspect mod loading state: {ex.Message}");
                return;
            }

            if (registeredMods.Count == 0 ||
                registeredMods.Any(static mod => string.Equals(mod.State, "None", StringComparison.Ordinal)))
                return;

            TryLogStartupModList();
        }

        private static void TryLogStartupModList()
        {
            lock (SyncRoot)
            {
                if (_logged)
                    return;

                _logged = true;
            }

            ModManager.OnModDetected -= OnModDetected;

            try
            {
                var loadedMods = Sts2ModManagerCompat.BuildLoadedModInventoryEntries();
                var registeredMods = Sts2ModManagerCompat.BuildModInventoryEntries();
                var hostVersion = Sts2HostVersion.ReleaseLabel
                                  ?? Sts2HostVersion.Numeric?.ToString()
                                  ?? "unknown";

                var text = new StringBuilder()
                    .AppendLine()
                    .AppendLine("=== RitsuLib Mod Debug Information ===")
                    .AppendLine($"Host Version: {hostVersion}")
                    .AppendLine($"RitsuLib {RitsuLibFramework.BuildVersionLogText()}");

                if (RuntimeFrameworkVersionSummary.TryBuildBaseLibDisplayLine(out var baseLibLine))
                    text.AppendLine(baseLibLine);

                text
                    .AppendLine($"Loaded Mods: {loadedMods.Count}")
                    .AppendLine($"Registered Mods: {registeredMods.Count}")
                    .AppendLine("Mod List:");

                if (loadedMods.Count == 0)
                {
                    text.AppendLine("  <none>");
                    RitsuLibFramework.Logger.Info(text.ToString());
                    return;
                }

                foreach (var mod in loadedMods)
                    text.AppendLine($"  * {FormatModName(mod)} ({FormatVersion(mod)})");

                RitsuLibFramework.Logger.Info(text.ToString());
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"{Prefix} Failed to print startup mod list: {ex.Message}");
            }
        }

        private static string FormatModName(Sts2ModInventoryEntry mod)
        {
            var name = string.IsNullOrWhiteSpace(mod.Name) ? mod.Id : mod.Name.Trim();
            return string.Equals(name, mod.Id, StringComparison.Ordinal)
                ? name
                : $"{name} [{mod.Id}]";
        }

        private static string FormatVersion(Sts2ModInventoryEntry mod)
        {
            return string.IsNullOrWhiteSpace(mod.Version) ? "unknown version" : mod.Version.Trim();
        }
    }
}
