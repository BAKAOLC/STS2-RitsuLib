using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Intercepts <see cref="PileTypeExtensions.GetTargetPosition" /> for mod piles, returning the
    ///     coordinate of the pile's registered UI host (button or extra-hand container). Must run as a Prefix
    ///     because vanilla's switch would otherwise throw <see cref="ArgumentOutOfRangeException" /> for minted
    ///     values.
    ///     <see cref="ArgumentOutOfRangeException" />。
    ///     为 mod pile 拦截 <see cref="PileTypeExtensions.GetTargetPosition" />，返回 pile 已注册 UI host
    ///     （button 或 extra-hand container）的坐标。它必须作为 Prefix 运行，因为原版 switch 对 minted
    ///     值会抛出 <see cref="ArgumentOutOfRangeException" />。
    ///     <see cref="ArgumentOutOfRangeException" />。
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
