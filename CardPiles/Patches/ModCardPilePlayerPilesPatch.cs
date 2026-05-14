using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Appends resolved run-persistent mod piles to <see cref="Player.Piles" /> so
    ///     <see cref="MegaCrit.Sts2.Core.Models.CardModel.Pile" /> can find cards stored in them.
    ///     将已解析的 run-persistent mod pile 追加到 <c>Player.Piles</c>，使
    ///     <c>CardModel.Pile</c> 能找到存放在其中的卡牌。
    /// </summary>
    public sealed class ModCardPilePlayerPilesPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_player_piles_run_persistent_mod_piles";

        /// <inheritdoc />
        public static string Description => "Append run-persistent mod card piles to Player.Piles";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Player), nameof(Player.Piles), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Merges the current run-persistent pile snapshot without creating new piles.
        ///     合并当前 run-persistent pile 快照，不创建新的 pile。
        /// </summary>
        public static void Postfix(Player __instance, ref IEnumerable<CardPile> __result)
        {
            var runPiles = ModCardPileStorage.GetRunPiles(__instance);
            if (runPiles.Count == 0)
                return;

            __result = __result.Concat(runPiles);
        }
        // ReSharper restore InconsistentNaming
    }
}
