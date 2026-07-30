using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies the registered shuffle-flight start position when a shuffle originates from a mod pile.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         洗牌飞行动画从模组牌堆开始时，应用已注册的飞行起始位置。
    ///     </para>
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
