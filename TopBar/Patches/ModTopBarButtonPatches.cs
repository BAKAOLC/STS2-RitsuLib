using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.TopBar.Patches
{
    /// <summary>
    ///     Mounts every registered <see cref="ModTopBarButtonDefinition" /> onto <see cref="NTopBar" /> after
    ///     its vanilla <c>_Ready</c> has populated <c>%Deck</c>. Buttons are <see cref="NModCardPileButton" />
    ///     instances in "action mode" — that is, they look and animate exactly like <b>pile-backed</b>
    ///     <see cref="STS2RitsuLib.CardPiles.ModCardPileRegistry" /> top-bar buttons, but dispatch clicks
    ///     through <see cref="ModTopBarButtonSpec.OnClick" /> and draw their count label from
    ///     <see cref="ModTopBarButtonSpec.CountProvider" />. The two registries share one placement
    ///     algorithm (see <see cref="ModTopBarLayout" />) so the player-side cluster next to <c>%Deck</c>
    ///     never splits into "pile row" vs "action row".
    ///     在原版 <c>_Ready</c> 填充 <c>%Deck</c> 后，将每个已注册的 <see cref="ModTopBarButtonDefinition" /> 挂载到 <see cref="NTopBar" />。
    ///     按钮是“动作模式”的 <see cref="NModCardPileButton" />
    ///     实例；也就是说，它们外观和动画与<b>牌堆支持</b>的
    ///     <see cref="STS2RitsuLib.CardPiles.ModCardPileRegistry" /> 顶部栏按钮完全相同，但点击会通过
    ///     <see cref="ModTopBarButtonSpec.OnClick" /> 分发，并从
    ///     <see cref="ModTopBarButtonSpec.CountProvider" /> 绘制计数标签。两个注册表共享一个放置
    ///     算法（见 <see cref="ModTopBarLayout" />），因此 <c>%Deck</c> 旁的玩家侧集群
    ///     永远不会拆成“牌堆行”和“动作行”。
    ///     永远不会拆成“牌堆行”和“动作行”。
    /// </summary>
    public sealed class ModTopBarActionButtonReadyPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_top_bar_ready_action_inject";

        /// <inheritdoc />
        public static string Description => "Inject mod action buttons into NTopBar";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NTopBar), nameof(NTopBar._Ready))];
        }

        /// <summary>
        ///     Wires generic mod top-bar buttons alongside the vanilla deck/map/pause nodes and any
        ///     pile-backed <see cref="STS2RitsuLib.CardPiles.ModCardPileUiStyle.TopBarDeck" /> buttons.
        ///     将通用 mod 顶部栏按钮接入原版 deck / map / pause 节点以及任何
        ///     牌堆支持的 <see cref="STS2RitsuLib.CardPiles.ModCardPileUiStyle.TopBarDeck" /> 按钮旁。
        ///     牌堆支持的 <c>STS2RitsuLib.CardPiles.ModCardPileUiStyle.TopBarDeck</c> 按钮。
        /// </summary>
        public static void Postfix(NTopBar __instance)
        {
            var definitions = ModTopBarButtonRegistry.GetDefinitionsSnapshot();
            if (definitions.Length == 0)
                return;

            // The right-side cluster (%Deck / %Map / %Pause / Options …) lives inside the
            // `RightAlignedStuff` container, not directly on NTopBar. `ModTopBarLayout.Place` handles
            // that re-parenting for us so buttons end up as siblings of %Deck.
            foreach (var definition in definitions)
            {
                var button = NModCardPileButton.CreateAction(definition);
                __instance.AddChildSafely(button);
                ModTopBarLayout.Place(__instance, button, definition.Offset);
            }
        }
    }

    /// <summary>
    ///     Binds every injected action-mode <see cref="NModCardPileButton" /> to the local
    ///     <see cref="Player" /> on <see cref="NTopBar.Initialize" />, mirroring
    ///     <c>ModCardPileTopBarInitializePatch</c>.
    ///     将每个注入的动作模式 <see cref="NModCardPileButton" /> 绑定到本地
    ///     <see cref="Player" />，发生在 <see cref="NTopBar.Initialize" /> 时，对应
    ///     <c>ModCardPileTopBarInitializePatch</c>。
    /// </summary>
    public sealed class ModTopBarActionButtonInitializePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_top_bar_initialize_action_bind";

        /// <inheritdoc />
        public static string Description =>
            "Bind mod action buttons to the local player on NTopBar.Initialize";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NTopBar), nameof(NTopBar.Initialize), [typeof(IRunState)]),
            ];
        }

        /// <summary>
        ///     Binds each injected action button to the local <see cref="Player" />.
        ///     将每个注入的动作按钮绑定到本地 <see cref="Player" />。
        ///     将每个注入的动作按钮绑定到本地 <c>Player</c>。
        /// </summary>
        public static void Postfix(NTopBar __instance, IRunState runState)
        {
            var player = LocalContext.GetMe(runState);
            if (player == null)
                return;
            // Action buttons live inside `RightAlignedStuff` (Deck's parent), not on NTopBar directly.
            var container = ModTopBarLayout.GetRightAlignedContainer(__instance);
            if (container == null)
                return;
            foreach (var button in container.GetChildren().OfType<NModCardPileButton>())
                if (button.IsActionMode)
                    button.Initialize(player);
        }
    }
}
