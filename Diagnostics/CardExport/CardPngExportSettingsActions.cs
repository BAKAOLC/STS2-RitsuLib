using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Diagnostics.CardExport
{
    /// <summary>
    ///     Starts card PNG export from persisted RitsuLib settings (Mod Settings UI).
    /// </summary>
    internal static class CardPngExportSettingsActions
    {
        internal static void TryBeginFromSettings(
            IModSettingsValueBinding<string> pathBinding,
            IModSettingsValueBinding<bool> includeHoverBinding,
            IModSettingsValueBinding<bool> includeUpgradesBinding,
            IModSettingsValueBinding<double> scaleBinding,
            IModSettingsValueBinding<string> filterBinding,
            IModSettingsValueBinding<bool> includeHiddenFromLibraryBinding)
        {
            var rawPath = pathBinding.Read().Trim();
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                RitsuLibFramework.Logger.Warn(
                    "Card PNG export: choose an output folder first, or use Browse.");
                return;
            }

            if (!CardPngExporter.TryValidateExportEnvironment(out var ctxErr))
            {
                RitsuLibFramework.Logger.Warn(ctxErr);
                return;
            }

            var scale = (float)scaleBinding.Read();
            var filter = filterBinding.Read().Trim();

            var request = new CardPngExportRequest
            {
                OutputDirectory = rawPath,
                CaptureMode = includeHoverBinding.Read()
                    ? CardPngExportCaptureMode.CardWithHoverTipsPanel
                    : CardPngExportCaptureMode.CardOnly,
                IncludeUpgradedVariants = includeUpgradesBinding.Read(),
                IncludeCardsHiddenFromLibrary = includeHiddenFromLibraryBinding.Read(),
                Scale = scale,
                IdFilterSubstring = string.IsNullOrEmpty(filter) ? null : filter,
                MaxBaseCards = 0,
            };

            RitsuLibFramework.BeginCardPngExport(request);
            RitsuLibFramework.Logger.Info("Card PNG export started.");
        }
    }
}
