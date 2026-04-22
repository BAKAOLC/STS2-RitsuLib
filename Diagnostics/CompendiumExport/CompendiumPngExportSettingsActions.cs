using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Diagnostics.CardExport;
using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Diagnostics.CompendiumExport
{
    internal static class CompendiumPngExportSettingsActions
    {
        internal static void TryBeginRelicDetailFromSettings(
            ModSettingsValueBinding<RitsuLibSettings, string> pathBinding,
            ModSettingsValueBinding<RitsuLibSettings, double> scaleBinding,
            ModSettingsValueBinding<RitsuLibSettings, string> filterBinding,
            ModSettingsValueBinding<RitsuLibSettings, bool> includeHoverBinding)
        {
            if (!TryValidatePathAndEnv(pathBinding, out var path))
                return;
            var filter = filterBinding.Read().Trim();
            RitsuLibFramework.BeginCompendiumDetailPngExport(new()
            {
                OutputDirectory = path,
                Scale = scaleBinding.Read(),
                IdFilterSubstring = string.IsNullOrEmpty(filter) ? null : filter,
                Relics = true,
                Potions = false,
                IncludeRelicHoverTips = includeHoverBinding.Read(),
            });
            RitsuLibFramework.Logger.Info("Relic detail PNG export started.");
        }

        internal static void TryBeginPotionDetailFromSettings(
            ModSettingsValueBinding<RitsuLibSettings, string> pathBinding,
            ModSettingsValueBinding<RitsuLibSettings, double> scaleBinding,
            ModSettingsValueBinding<RitsuLibSettings, string> filterBinding)
        {
            if (!TryValidatePathAndEnv(pathBinding, out var path))
                return;
            var filter = filterBinding.Read().Trim();
            RitsuLibFramework.BeginCompendiumDetailPngExport(new()
            {
                OutputDirectory = path,
                Scale = scaleBinding.Read(),
                IdFilterSubstring = string.IsNullOrEmpty(filter) ? null : filter,
                Relics = false,
                Potions = true,
                IncludeRelicHoverTips = false,
            });
            RitsuLibFramework.Logger.Info("Potion detail PNG export started.");
        }

        private static bool TryValidatePathAndEnv(
            ModSettingsValueBinding<RitsuLibSettings, string> pathBinding, out string path)
        {
            path = pathBinding.Read().Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                RitsuLibFramework.Logger.Warn("Detail PNG export: choose an output folder first, or use Browse.");
                return false;
            }

            if (CardPngExporter.TryValidateExportEnvironment(out var err)) return true;
            RitsuLibFramework.Logger.Warn(err);
            return false;
        }
    }
}
