using MegaCrit.Sts2.Core.Localization;
using SmartFormat;
using STS2RitsuLib.Localization.SmartFormat;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Localization.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Injects mod-registered SmartFormat extensions after the game creates its localization
    ///         formatter.
    ///     </para>
    ///     <para xml:lang="zh-CN">在游戏创建本地化格式化器后，注入模组注册的 SmartFormat 扩展。</para>
    /// </summary>
    internal sealed class LocManagerLoadLocFormattersPostfixPatch : IPatchMethod
    {
        public static string PatchId => "loc_manager_load_loc_formatters_register_mod_extensions";

        public static string Description =>
            "Inject mod-registered SmartFormat IFormatter / ISource into LocManager";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(LocManager), "LoadLocFormatters")];
        }

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
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[SmartFormat] Failed to inject registered localization extensions: {ex.Message}");
            }
        }
    }
}
