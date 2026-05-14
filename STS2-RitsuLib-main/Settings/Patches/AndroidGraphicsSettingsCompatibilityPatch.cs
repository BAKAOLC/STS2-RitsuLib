using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Settings.Patches
{
    /// <summary>
    ///     Some Android builds omit the ShaderCompatibility entry from the settings scene. Skip the vanilla wiring call
    ///     in that case so opening settings does not throw node lookup exceptions.
    /// </summary>
    public class AndroidGraphicsSettingsCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "android_graphics_settings_missing_shader_compatibility";

        public static bool IsCritical => false;

        public static string Description =>
            "Skip ConfigureAndroidGraphicsEntries when %ShaderCompatibility is absent";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NSettingsScreen), "ConfigureAndroidGraphicsEntries")];
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(NSettingsScreen __instance)
        {
            if (!OperatingSystem.IsAndroid())
                return true;

            if (__instance.GetNodeOrNull<Control>("%ShaderCompatibility") != null)
                return true;

            RitsuLibFramework.Logger.Warn(
                "[Settings][AndroidCompat] Missing %ShaderCompatibility node. Skipping ConfigureAndroidGraphicsEntries().");
            return false;
        }
    }
}
