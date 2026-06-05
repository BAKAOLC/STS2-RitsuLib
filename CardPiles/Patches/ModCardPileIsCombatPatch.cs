using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Extends <see cref="PileTypeExtensions.IsCombatPile" /> to return <c>true</c> for
    ///     <see cref="ModCardPileScope.CombatOnly" /> mod piles. Uses a Postfix so that baselib's own Prefix (if
    ///     present) runs first; ritsulib only upgrades the result when everyone else said "no".
    ///     扩展 <see cref="PileTypeExtensions.IsCombatPile" />，使
    ///     <see cref="ModCardPileScope.CombatOnly" /> mod pile 返回 <c>true</c>。使用 Postfix 让 baselib
    ///     自己的 Prefix（如果存在）先运行；ritsulib 只在其它所有逻辑都说“no”时提升结果。
    /// </summary>
    public sealed class ModCardPileIsCombatPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_card_pile_is_combat_mod_augment";

        /// <inheritdoc />
        public static string Description =>
            "Treat CombatOnly mod card piles as combat piles for PileTypeExtensions.IsCombatPile";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PileTypeExtensions), nameof(PileTypeExtensions.IsCombatPile))];
        }

        /// <summary>
        ///     Promotes mod-pile results to <c>true</c> after vanilla / baselib ran.
        ///     在原版 / baselib 运行后，将 mod-pile 结果提升为 <c>true</c>。
        /// </summary>
        public static void Postfix(PileType pileType, ref bool __result)
        {
            if (__result)
                return;
            if (!ModCardPileRegistry.TryGetByPileType(pileType, out var definition))
                return;
            if (definition.Scope != ModCardPileScope.CombatOnly)
                return;

            __result = true;
        }
    }
}
