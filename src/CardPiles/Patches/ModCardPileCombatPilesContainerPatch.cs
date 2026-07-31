using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Injects bottom-row mod pile buttons after <see cref="NCombatPilesContainer" /> becomes ready.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <see cref="NCombatPilesContainer" /> 就绪后注入底部区域的模组牌堆按钮。
    ///     </para>
    /// </summary>
    internal sealed class ModCardPileCombatPilesContainerReadyPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_combat_piles_container_ready";
        public static string Description => "Inject mod card pile buttons into NCombatPilesContainer on ready";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCombatPilesContainer), nameof(NCombatPilesContainer._Ready))];
        }

        public static void Postfix(NCombatPilesContainer __instance)
        {
            ModCardPileInjector.InjectCombatButtons(__instance);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Binds injected bottom-row pile buttons to the player passed to
    ///         <see cref="NCombatPilesContainer.Initialize" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将已注入的底部牌堆按钮绑定到传给 <see cref="NCombatPilesContainer.Initialize" /> 的玩家。
    ///     </para>
    /// </summary>
    internal sealed class ModCardPileCombatPilesContainerInitializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_combat_piles_container_initialize";
        public static string Description => "Initialize injected mod pile buttons with the current player";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NCombatPilesContainer), nameof(NCombatPilesContainer.Initialize), [typeof(Player)]),
            ];
        }

        public static void Postfix(NCombatPilesContainer __instance, Player player)
        {
            foreach (var button in __instance.GetChildren().OfType<NModCardPileButton>())
                button.Initialize(player);
            ModCardPileCombatLayout.Relayout(__instance);
        }
    }
}
