using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Data
{
    internal static class RitsuLibSettingsStore
    {
        private static readonly ModDataStore Store = ModDataStore.For(Const.ModId);

        private static readonly Lock InitLock = new();
        private static bool _initialized;

        internal static void Initialize()
        {
            lock (InitLock)
            {
                if (_initialized)
                    return;

                using (RitsuLibFramework.BeginModDataRegistration(Const.ModId, false))
                {
                    Store.Register<RitsuLibSettings>(
                        Const.SettingsKey,
                        Const.SettingsFileName,
                        SaveScope.Global,
                        () => new(),
                        true
                    );
                }

                NormalizeSchemaVersionIfNeeded();

                _initialized = true;
                LogConfigSnapshot();
            }
        }

        private static void LogConfigSnapshot()
        {
            var s = GetSettings();
            var master = s.DebugCompatibilityMode;
            RitsuLibFramework.Logger.Info(
                $"[Config] Debug compatibility master is {(master ? "enabled" : "disabled")}. " +
                $"Sub-flags (only when master on): LocTable={s.DebugCompatLocTable}, UnlockEpoch={s.DebugCompatUnlockEpoch}, AncientArchitect={s.DebugCompatAncientArchitect}. " +
                $"Config file: {ProfileManager.GetFilePath(Const.SettingsFileName, SaveScope.Global, 0, Const.ModId)}");
        }

        /// <summary>
        ///     Master debug-compatibility switch. When false, no RitsuLib soft-fail shims run.
        /// </summary>
        internal static bool IsDebugCompatibilityMasterEnabled()
        {
            Initialize();
            return GetSettings().DebugCompatibilityMode;
        }

        /// <summary>
        ///     <c>LocTable</c> missing-key placeholders + warnings.
        /// </summary>
        internal static bool IsLocTableCompatEnabled()
        {
            Initialize();
            var s = GetSettings();
            return s is { DebugCompatibilityMode: true, DebugCompatLocTable: true };
        }

        /// <summary>
        ///     Skip invalid epoch grants with warnings instead of throwing.
        /// </summary>
        internal static bool IsUnlockEpochCompatEnabled()
        {
            Initialize();
            var s = GetSettings();
            return s is { DebugCompatibilityMode: true, DebugCompatUnlockEpoch: true };
        }

        /// <summary>
        ///     <c>THE_ARCHITECT</c> empty dialogue stub for registry characters.
        /// </summary>
        internal static bool IsAncientArchitectCompatEnabled()
        {
            Initialize();
            var s = GetSettings();
            return s is { DebugCompatibilityMode: true, DebugCompatAncientArchitect: true };
        }

        private static RitsuLibSettings GetSettings()
        {
            return Store.Get<RitsuLibSettings>(Const.SettingsKey);
        }

        /// <summary>
        ///     Harmony patch dump UI / lifecycle reads paths and flags without exposing the store surface publicly.
        /// </summary>
        internal static (string OutputPath, bool DumpOnFirstMainMenu) GetHarmonyPatchDumpOptions()
        {
            Initialize();
            var s = GetSettings();
            return (s.HarmonyPatchDumpOutputPath, s.HarmonyPatchDumpOnFirstMainMenu);
        }

        private static void NormalizeSchemaVersionIfNeeded()
        {
            var settings = GetSettings();
            if (settings.SchemaVersion >= RitsuLibSettings.CurrentSchemaVersion)
                return;

            Store.Modify<RitsuLibSettings>(Const.SettingsKey, model =>
            {
                if (model.SchemaVersion < 2)
                {
                    model.DebugCompatLocTable = true;
                    model.DebugCompatUnlockEpoch = true;
                    model.DebugCompatAncientArchitect = true;
                }

                // Schema 3 adds Harmony patch dump options; new properties default via type initializers / JSON.

                model.SchemaVersion = RitsuLibSettings.CurrentSchemaVersion;
            });
            Store.Save(Const.SettingsKey);
        }
    }
}
