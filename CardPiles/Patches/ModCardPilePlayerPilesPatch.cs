using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Appends resolved run-persistent mod piles to <see cref="Player.Piles" /> so
    ///     <see cref="MegaCrit.Sts2.Core.Models.CardModel.Pile" /> can find cards stored in them.
    ///     将已解析的 run-persistent mod pile 追加到 <see cref="Player.Piles" />，使
    ///     <see cref="MegaCrit.Sts2.Core.Models.CardModel.Pile" /> 能找到存放在其中的卡牌。
    /// </summary>
    internal sealed class ModCardPilePlayerPilesPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_player_piles_run_persistent_mod_piles";
        public static string Description => "Append run-persistent mod card piles to Player.Piles";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Player), nameof(Player.Piles), MethodType.Getter)];
        }

        public static void Postfix(Player __instance, ref IEnumerable<CardPile> __result)
        {
            var runPiles = ModCardPileStorage.GetRunPiles(__instance);
            if (runPiles.Count == 0)
                return;

            __result = __result.Concat(runPiles);
        }
    }
}
