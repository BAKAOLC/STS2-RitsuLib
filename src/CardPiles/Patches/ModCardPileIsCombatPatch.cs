using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Makes <see cref="PileTypeExtensions.IsCombatPile" /> recognize registered
    ///         <see cref="ModCardPileScope.CombatOnly" /> piles.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使 <see cref="PileTypeExtensions.IsCombatPile" /> 识别已注册的
    ///         <see cref="ModCardPileScope.CombatOnly" /> 牌堆。
    ///     </para>
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
