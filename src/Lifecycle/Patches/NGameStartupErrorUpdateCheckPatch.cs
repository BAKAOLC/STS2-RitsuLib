using MegaCrit.Sts2.Core.Nodes;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Updates;

namespace STS2RitsuLib.Lifecycle.Patches
{
    internal sealed class NGameStartupErrorUpdateCheckPatch : IPatchMethod
    {
        public static string PatchId => "ngame_startup_error_update_check";

        public static string Description =>
            "Start automatic update checks when the game enters its startup error dialog";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NGame), "GameStartupError")];
        }

        public static void Prefix()
        {
            AutomaticUpdateCheckScheduler.StartForGameStartupError();
        }
    }
}
