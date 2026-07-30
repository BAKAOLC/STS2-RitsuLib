using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds obtained RitsuLib requirement epochs to <see cref="UnlockState" /> before multiplayer lobby
    ///         synchronization and run setup serialize it.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在多人游戏大厅同步和开局设置序列化 <see cref="UnlockState" /> 前，将已获得的 RitsuLib 要求纪元加入其中。
    ///     </para>
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
    ///     <para xml:lang="en">
    ///         Adds obtained requirement epochs to the local host's lobby state when a game screen constructs
    ///         <see cref="UnlockState" /> directly instead of calling
    ///         <see cref="SaveManager.GenerateUnlockStateFromProgress" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         游戏界面直接构造 <see cref="UnlockState" /> 而未调用
    ///         <see cref="SaveManager.GenerateUnlockStateFromProgress" /> 时，将已获得的要求纪元加入本地主机的大厅状态。
    ///     </para>
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
