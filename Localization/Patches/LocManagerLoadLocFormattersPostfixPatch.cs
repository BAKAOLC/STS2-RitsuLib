using MegaCrit.Sts2.Core.Localization;
using SmartFormat;
using STS2RitsuLib.Localization.SmartFormat;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Localization.Patches
{
    /// <summary>
    ///     Injects mod-registered SmartFormat extensions after the game creates its localization formatter.
    /// </summary>
    public sealed class LocManagerLoadLocFormattersPostfixPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "loc_manager_load_loc_formatters_register_mod_extensions";

        /// <inheritdoc />
        public static string Description =>
            "Inject mod-registered SmartFormat IFormatter / ISource into LocManager";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(LocManager), "LoadLocFormatters")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Adds all registered mod SmartFormat extensions to the freshly created localization formatter.
        /// </summary>
        public static void Postfix(SmartFormatter ____smartFormatter)
        {
            try
            {
                var formatter = ____smartFormatter ?? Smart.Default;
                if (formatter == null)
                {
                    RitsuLibFramework.Logger.Warn(
                        "[SmartFormat] LocManager.LoadLocFormatters completed but no SmartFormatter instance was available.");
                    return;
                }

                SmartFormatExtensionInjector.InjectAll(formatter);
                ModSmartFormatExtensionRegistry.NotifyInitialized(formatter);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error(
                    $"[SmartFormat] Failed to inject registered localization extensions: {ex.Message}");
            }
        }
    }
}
