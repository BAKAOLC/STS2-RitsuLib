using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds existing run-persistent mod piles to <see cref="Player.Piles" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">将已有的局内持久模组牌堆加入 <see cref="Player.Piles" />。</para>
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
