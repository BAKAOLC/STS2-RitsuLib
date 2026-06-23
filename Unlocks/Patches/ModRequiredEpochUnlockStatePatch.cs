using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     Projects obtained RitsuLib requirement epochs into <see cref="UnlockState" /> before it is serialized for
    ///     multiplayer lobby sync and run setup.
    ///     在多人大厅同步和跑局创建序列化前，将已获得的 RitsuLib 需求纪元投影进 <see cref="UnlockState" />。
    /// </summary>
    internal sealed class ModRequiredEpochUnlockStatePatch : IPatchMethod
    {
        public static string PatchId => "mod_required_epoch_unlock_state";

        public static string Description =>
            "Include obtained mod requirement epochs in generated unlock state for deterministic multiplayer filtering";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(SaveManager), nameof(SaveManager.GenerateUnlockStateFromProgress), Type.EmptyTypes)];
        }

        public static void Postfix(SaveManager __instance, ref UnlockState __result)
        {
            ArgumentNullException.ThrowIfNull(__instance);
            ArgumentNullException.ThrowIfNull(__result);

            __result = ModUnlockRegistry.IncludeObtainedRequiredEpochs(__result, __instance.Progress);
        }
    }

    /// <summary>
    ///     Normalizes the local host player's lobby unlock state when game screens pass <c>new UnlockState(Progress)</c>
    ///     directly instead of <see cref="SaveManager.GenerateUnlockStateFromProgress" />.
    ///     当游戏界面直接传入 <c>new UnlockState(Progress)</c> 而不是
    ///     <see cref="SaveManager.GenerateUnlockStateFromProgress" /> 时，规范化本地 host 玩家在大厅中的解锁状态。
    /// </summary>
    internal sealed class StartRunLobbyLocalHostUnlockStatePatch : IPatchMethod
    {
        public static string PatchId => "start_run_lobby_local_host_unlock_state";

        public static string Description =>
            "Include obtained mod requirement epochs when serializing the local host player's lobby unlock state";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(StartRunLobby), nameof(StartRunLobby.AddLocalHostPlayer),
                    [typeof(UnlockState), typeof(int)]),
            ];
        }

        public static void Prefix(ref UnlockState unlocks)
        {
            if (unlocks == null)
                return;

            unlocks = ModUnlockRegistry.IncludeObtainedRequiredEpochs(unlocks, SaveManager.Instance.Progress);
        }
    }
}
