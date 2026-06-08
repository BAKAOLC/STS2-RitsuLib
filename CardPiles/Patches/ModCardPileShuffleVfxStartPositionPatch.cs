using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Overrides the source/start position for shuffle fly visuals when the source pile is a mod pile and
    ///     that pile registered a <see cref="ModCardPileSpec.FlightStartPositionResolver" />.
    ///     当源牌堆是 mod 牌堆，且该牌堆注册了
    ///     <see cref="ModCardPileSpec.FlightStartPositionResolver" /> 时，覆盖 shuffle 飞行动画的源/起点位置。
    /// </summary>
    internal sealed class ModCardPileShuffleVfxStartPositionPatch : IPatchMethod
    {
        private static readonly FieldInfo? StartPositionField =
            AccessTools.Field(typeof(NCardFlyShuffleVfx), "_startPos");

        public static string PatchId => "ritsulib_mod_pile_shuffle_vfx_start_position";

        public static string Description =>
            "Allow mod card piles to customize shuffle-fly source positions";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardFlyShuffleVfx), nameof(NCardFlyShuffleVfx.Create))];
        }

        public static void Postfix(CardPile startPile, CardPile targetPile, ref NCardFlyShuffleVfx? __result)
        {
            if (__result == null || StartPositionField == null)
                return;
            if (!ModCardPileRegistry.TryGetByPileType(startPile.Type, out var definition))
                return;

            var resolved = ModCardPileLayout.GetShuffleStartPosition(definition, startPile, targetPile);
            StartPositionField.SetValue(__result, resolved);
        }
    }
}
