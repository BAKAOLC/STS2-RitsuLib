using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Resolves the visible card node for cards stored in registered mod piles.
    ///     </para>
    ///     <para xml:lang="zh-CN">解析存放在已注册模组牌堆中的卡牌所对应的可见节点。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         The prefix bypasses the vanilla switch for dynamic pile types. Invisible mod piles return
    ///         <see langword="null" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         前置补丁会为动态牌堆类型绕过原版分支。不可见的模组牌堆返回 <see langword="null" />。
    ///     </para>
    /// </remarks>
    internal sealed class ModCardPileFindOnTablePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_ncard_find_on_table_mod_route";

        public static string Description =>
            "Resolve NCard.FindOnTable for cards held in visible mod piles (ExtraHand containers)";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCard), nameof(NCard.FindOnTable))];
        }

        public static bool Prefix(CardModel card, PileType? overridePile, ref NCard? __result)
        {
#if STS2_AT_LEAST_0_110_0
            var pileType = overridePile ?? card.Pile?.Type;
#else
            var pileType = card.Pile?.Type ?? overridePile;
#endif
            if (pileType == null)
                return true;
            if (!ModCardPileRegistry.TryGetByPileType(pileType.Value, out var definition))
                return true;

            __result = definition.CardShouldBeVisible
                ? ModCardPileButtonRegistry.TryGetExtraHand(definition)?.GetCard(card)
                  ?? NCardPlayQueue.Instance?.GetCardNode(card)
                  ?? NCombatRoom.Instance?.Ui?.GetCardFromPlayContainer(card)
                : null;
            return false;
        }
    }
}
