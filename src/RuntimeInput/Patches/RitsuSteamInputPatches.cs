using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.RuntimeInput.Patches
{
    internal sealed class RitsuSteamInputManifestInstallPatch : IPatchMethod
    {
        public static string PatchId => "ritsu_steam_input_manifest_install";
        public static bool IsCritical => false;
        public static string Description => "Install optional RitsuLib Steam Input action manifest";

        public static ModPatchTarget[] GetTargets()
        {
            return [PatchTarget.AsyncMethod<SteamControllerInputStrategy>(nameof(SteamControllerInputStrategy.Init))];
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            const string operation =
                "[SteamInput] Install optional manifest immediately before Steam Input initialization";
            var init = HarmonyIl.RequireMethod(
                AccessTools.Method(Type.GetType("Steamworks.SteamInput, Steamworks.NET"), "Init", [typeof(bool)]),
                operation);
            var install = HarmonyIl.RequireMethod(
                AccessTools.Method(typeof(RitsuSteamInputManifestInstaller),
                    nameof(RitsuSteamInputManifestInstaller.InstallBeforeSteamInputInit)), operation);
            var rewriter = HarmonyIlRewriter.From(instructions);
            var report = rewriter.InsertBeforeCall(operation, init, [HarmonyIl.Call(install)],
                code => code.Any(instruction => HarmonyIl.IsCallTo(instruction, install)),
                moveLabelsAndBlocksToInserted: true);
            return rewriter.InstructionsChecked(report);
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
