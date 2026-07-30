using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Resolves <see cref="PileTypeExtensions.GetTargetPosition" /> for registered mod card piles.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为已注册模组卡牌牌堆解析 <see cref="PileTypeExtensions.GetTargetPosition" />。
    ///     </para>
    /// </summary>
    internal sealed class ModCardPileGetTargetPositionPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_pile_get_target_position_mod_route";

        public static string Description =>
            "Provide NCard fly-in targets for mod card piles before the vanilla switch throws";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PileTypeExtensions), nameof(PileTypeExtensions.GetTargetPosition))];
        }

        public static bool Prefix(PileType pileType, NCard? node, ref Vector2 __result)
        {
            if (!ModCardPileRegistry.TryGetByPileType(pileType, out var definition))
                return true;

            __result = ModCardPileLayout.GetTargetPosition(definition, node);
            return false;
        }
    }
}
