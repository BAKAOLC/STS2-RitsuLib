using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Injects bottom-row mod pile buttons (<see cref="ModCardPileUiStyle.BottomLeft" /> /
    ///     <see cref="ModCardPileUiStyle.BottomRight" />) into <see cref="NCombatPilesContainer" /> after its
    ///     vanilla <c>_Ready</c> finishes wiring up draw / discard / exhaust.
    ///     （<see cref="ModCardPileUiStyle.BottomLeft" /> / <see cref="ModCardPileUiStyle.BottomRight" />）
    ///     在原版 <c>_Ready</c> 完成 draw / discard / exhaust 接线后，将底部 row 的 mod pile 按钮
    ///     （<see cref="ModCardPileUiStyle.BottomLeft" />
    ///     <see cref="ModCardPileUiStyle.BottomRight" />）
    ///     注入 <see cref="NCombatPilesContainer" />。
    ///     （<see cref="ModCardPileUiStyle.BottomLeft" />
    ///     <see cref="ModCardPileUiStyle.BottomRight" />）
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
    ///     Forwards <see cref="NCombatPilesContainer.Initialize" /> to every injected
    ///     <see cref="NModCardPileButton" /> so each mod pile binds to the active <see cref="Player" />
    ///     alongside the vanilla draw / discard / exhaust buttons.
    ///     将 <see cref="NCombatPilesContainer.Initialize" /> 转发给每个已注入的
    ///     <see cref="NModCardPileButton" />，使每个 mod 牌堆与原版 draw / discard / exhaust 按钮一起绑定到
    ///     当前 <see cref="Player" />。
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
