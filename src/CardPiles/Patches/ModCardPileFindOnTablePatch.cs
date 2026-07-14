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
    ///     Extends <see cref="NCard.FindOnTable" /> so cards resident in a visible mod pile (currently only the
    ///     <see cref="ModCardPileUiStyle.ExtraHand" /> style) resolve to the live <c>NCard</c> instance managed
    ///     by <c>NModExtraHand</c>. Non-visible mod piles intentionally return <c>null</c> (matching vanilla's
    ///     Draw / Discard / Exhaust behaviour).
    ///     扩展 <see cref="NCard.FindOnTable" />，使位于可见 mod pile 中的 card（目前只有
    ///     <see cref="ModCardPileUiStyle.ExtraHand" /> 样式）解析到由 <c>NModExtraHand</c> 管理的 live
    ///     <c>NCard</c> 实例。不可见 mod pile 会有意返回 <c>null</c>（匹配原版 Draw / Discard / Exhaust 行为）。
    /// </summary>
    /// <remarks>
    ///     Implemented as a Prefix because vanilla's switch hits
    ///     <c>
    ///         _ =&gt; throw new
    ///         ArgumentOutOfRangeException()
    ///     </c>
    ///     for any pile type it doesn't know, which would otherwise abort
    ///     every runtime card lookup while a mod pile hosts the card.
    ///     它实现为 Prefix，因为原版 switch 对任何未知 pile type 会命中
    ///     <c>_ =&gt; throw new ArgumentOutOfRangeException()</c>；否则当 mod pile 持有 card 时，
    ///     每次运行时 card lookup 都会被中止。
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
            var pileType = card.Pile?.Type ?? overridePile;
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
