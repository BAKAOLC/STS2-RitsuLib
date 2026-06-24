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
    internal sealed class ModCardPileIsCombatPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_card_pile_is_combat_mod_augment";

        public static string Description =>
            "Treat CombatOnly mod card piles as combat piles for PileTypeExtensions.IsCombatPile";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PileTypeExtensions), nameof(PileTypeExtensions.IsCombatPile))];
        }

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
