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
    public sealed class ModCardPileCombatPilesContainerReadyPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_combat_piles_container_ready";

        /// <inheritdoc />
        public static string Description => "Inject mod card pile buttons into NCombatPilesContainer on ready";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCombatPilesContainer), nameof(NCombatPilesContainer._Ready))];
        }

        /// <summary>
        ///     Injects mod bottom-row pile buttons after vanilla wiring completes.
        ///     在原版接线完成后注入 mod 底部 row 牌堆按钮。
        /// </summary>
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
    public sealed class ModCardPileCombatPilesContainerInitializePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_combat_piles_container_initialize";

        /// <inheritdoc />
        public static string Description => "Initialize injected mod pile buttons with the current player";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NCombatPilesContainer), nameof(NCombatPilesContainer.Initialize), [typeof(Player)]),
            ];
        }

        /// <summary>
        ///     Binds each mod pile button to the current player.
        ///     将每个 mod pile 按钮绑定到当前玩家。
        /// </summary>
        public static void Postfix(NCombatPilesContainer __instance, Player player)
        {
            foreach (var button in __instance.GetChildren().OfType<NModCardPileButton>())
                button.Initialize(player);
            ModCardPileCombatLayout.Relayout(__instance);
        }
    }
}
