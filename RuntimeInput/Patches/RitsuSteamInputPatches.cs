using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.RuntimeInput.Patches
{
    internal sealed class RitsuSteamInputManifestInstallPatch : IPatchMethod
    {
        public static string PatchId => "ritsu_steam_input_manifest_install";
        public static bool IsCritical => false;
        public static string Description => "Install optional RitsuLib Steam Input action manifest";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(SteamControllerInputStrategy), nameof(SteamControllerInputStrategy.Init))];
        }

        public static void Prefix()
        {
            RitsuSteamInputManifestInstaller.InstallBeforeSteamInputInit();
        }
    }

    internal sealed class RitsuSteamInputBackendProcessPatch : IPatchMethod
    {
        public static string PatchId => "ritsu_steam_input_backend_process";
        public static bool IsCritical => false;
        public static string Description => "Poll optional RitsuLib Steam Input actions after controller input";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NControllerManager), nameof(NControllerManager._Process), [typeof(double)])];
        }

        public static void Postfix()
        {
            RitsuSteamInputBackend.Process();
        }
    }
}
