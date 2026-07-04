using Godot;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Describes a mod card pile at registration time. Everything but the mod id and local stem is optional;
    ///     sensible defaults match the vanilla Draw / Discard / Exhaust button behaviour.
    ///     描述注册时的 mod 卡牌牌堆。除 mod id 和 local stem 外均为可选；
    ///     合理默认值会匹配原版 Draw / Discard / Exhaust 按钮行为。
    /// </summary>
    /// <remarks>
    ///     Localization follows the vanilla pile convention — the hover-tip title / description and the
    ///     "open empty pile" thought bubble are always resolved against the built-in
    ///     <c>static_hover_tips</c> loc table, using the keys <c>"{id}.title"</c>,
    ///     <c>"{id}.description"</c> and <c>"{id}.empty"</c> where <c>id</c> is the registered pile id. Mods cannot create
    ///     additional loc
    ///     tables, so all entries are expected to live in <c>static_hover_tips.json</c> merged through
    ///     the normal mod-localization pipeline.
    ///     本地化遵循原版牌堆约定：hover-tip title / description 以及“打开空牌堆”的 thought bubble
    ///     始终基于内置 <c>static_hover_tips</c> loc table 解析，使用 <c>"{id}.title"</c>、
    ///     <c>"{id}.description"</c> 和 <c>"{id}.empty"</c> key，其中 <c>id</c> 是已注册牌堆 id。
    ///     mod 不能创建额外 loc table，因此所有 entry 都应放在 <c>static_hover_tips.json</c> 中，
    ///     并通过常规 mod-localization 管线合并。
    /// </remarks>
    public sealed record ModCardPileSpec
    {
        /// <summary>
        ///     Vanilla loc table used for every mod-card-pile hover tip. Mods can only *extend* this table
        ///     (not create new tables), so the pile subsystem always resolves into it.
        ///     每个 mod-card-pile hover tip 使用的原版 loc table。mod 只能扩展此表（不能创建新表），
        ///     因此牌堆子系统总是解析到此表。
        /// </summary>
        public const string HoverTipLocTable = "static_hover_tips";

        /// <summary>
        ///     Builds a spec with defaults suitable for a combat-only bottom-left pile that auto-stacks
        ///     toward the screen center (same row as the draw pile).
        ///     构建带默认值的 spec，适用于仅战斗中存在、位于左下、向屏幕中心自动堆叠的牌堆
        ///     （与抽牌堆同一行）。
        /// </summary>
        public ModCardPileSpec()
        {
        }

        /// <summary>
        ///     Lifetime scope of the pile. Defaults to <see cref="ModCardPileScope.CombatOnly" />.
        ///     牌堆的生命周期作用域。默认 <see cref="ModCardPileScope.CombatOnly" />。
        /// </summary>
        public ModCardPileScope Scope { get; init; } = ModCardPileScope.CombatOnly;

        /// <summary>
        ///     Visual style family; drives which UI chrome is attached in combat. Defaults to
        ///     <see cref="ModCardPileUiStyle.Headless" /> (no UI button).
        ///     视觉样式族；决定战斗中附加哪种 UI chrome。默认 <see cref="ModCardPileUiStyle.Headless" />
        ///     （无 UI 按钮）。
        /// </summary>
        public ModCardPileUiStyle Style { get; init; } = ModCardPileUiStyle.Headless;

        /// <summary>
        ///     Slot hint paired with <see cref="Style" />. When left at <see cref="ModCardPileAnchor.Default" />
        ///     the pile auto-stacks after other same-style piles in registration order.
        ///     与 <see cref="Style" /> 配对的 slot hint。保持为 <see cref="ModCardPileAnchor.Default" /> 时，
        ///     牌堆会按注册顺序堆叠在其它同样式牌堆之后。
        /// </summary>
        public ModCardPileAnchor Anchor { get; init; } = ModCardPileAnchor.Default;

        /// <summary>
        ///     Godot resource path for the pile's button icon (for example <c>res://art/my_pile.png</c>). When
        ///     null or missing the placeholder texture is used.
        ///     牌堆按钮图标的 Godot resource 路径（例如 <c>res://art/my_pile.png</c>）。为 null 或缺失时使用占位贴图。
        /// </summary>
        public string? IconPath { get; init; }

        /// <summary>
        ///     Optional controller / keyboard hotkey ids that open the pile's view screen.
        ///     打开牌堆 view screen 的可选手柄 / 键盘 hotkey id。
        /// </summary>
        public string[]? Hotkeys { get; init; }

        /// <summary>
        ///     When true, cards added to the pile are displayed as <c>NCard</c> nodes inside the pile's UI
        ///     container (only meaningful for <see cref="ModCardPileUiStyle.ExtraHand" />).
        ///     为 true 时，加入牌堆的卡牌会在牌堆的 UI 容器中显示为 <c>NCard</c> 节点
        ///     （仅对 <see cref="ModCardPileUiStyle.ExtraHand" /> 有意义）。
        /// </summary>
        public bool CardShouldBeVisible { get; init; }

        /// <summary>
        ///     Extra screen-space pixels added to the hover tip's resolved <see cref="Godot.Control.GlobalPosition" />.
        ///     Defaults to <see cref="Vector2.Zero" />. Most useful with <see cref="ModCardPileAnchorKind.Custom" />
        ///     when the automatic placement needs a small nudge.
        ///     添加到 hover tip 解析后 <see cref="Godot.Control.GlobalPosition" /> 的额外屏幕空间像素。
        ///     默认 <see cref="Vector2.Zero" />。当 <see cref="ModCardPileAnchorKind.Custom" /> 的自动放置
        ///     需要微调时最有用。
        /// </summary>
        public Vector2 HoverTipScreenOffset { get; init; }

        /// <summary>
        ///     How the hover tip is anchored relative to the pile button. Defaults to
        ///     <see cref="ModCardPileHoverTipPlacement.Auto" />.
        ///     hover tip 相对于牌堆按钮的锚定方式。默认 <see cref="ModCardPileHoverTipPlacement.Auto" />。
        /// </summary>
        public ModCardPileHoverTipPlacement HoverTipPlacement { get; init; } = ModCardPileHoverTipPlacement.Auto;

        /// <summary>
        ///     When non-null, evaluated periodically on the pile button's <c>_Process</c> tick (same pattern as
        ///     <see cref="TopBar.ModTopBarButtonSpec.VisibleWhen" />). If the delegate returns false the button is
        ///     hidden, ignores mouse input, and any active hover tip is removed. When null the button is always
        ///     shown (subject to normal parent visibility). Attribute-driven registration cannot supply a
        ///     delegate; use <see cref="ModCardPileRegistry.Register" /> from code when you need this.
        ///     <see cref="ModCardPileRegistry.Register" />。
        ///     非 null 时，会在牌堆按钮的 <c>_Process</c> tick 上定期求值（与
        ///     <see cref="TopBar.ModTopBarButtonSpec.VisibleWhen" /> 模式相同）。如果 delegate 返回 false，按钮会
        ///     隐藏、忽略鼠标输入，并移除任何活动悬停提示。为 null 时按钮始终
        ///     显示（仍受普通父节点可见性影响）。attribute-driven 注册不能提供
        ///     delegate；需要此功能时请从代码调用 <see cref="ModCardPileRegistry.Register" />。
        ///     <see cref="ModCardPileRegistry.Register" />。
        /// </summary>
        public Func<ModCardPileVisibilityContext, bool>? VisibleWhen { get; init; }

        /// <summary>
        ///     Optional default-screen capabilities. Null preserves legacy vanilla <c>NCardPileScreen</c>
        ///     behavior: no card inspection, no upgrade-preview tickbox, and no sort bar.
        ///     默认 screen 的可选能力。null 会保留旧的原版 <c>NCardPileScreen</c> 行为：无卡牌检查、
        ///     无升级预览开关、无排序栏。
        /// </summary>
        public ModCardPileViewSpec? View { get; init; }

        /// <summary>
        ///     Optional callback invoked when the pile's UI button is released. When null (the default) the
        ///     button falls back to <c>NCardPileScreen.ShowScreen</c> — the same behaviour as vanilla Draw /
        ///     Discard / Exhaust buttons. Supply a delegate to plug in a custom
        ///     <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Capstones.ICapstoneScreen" />, inspect the pile, or
        ///     do nothing at all.
        ///     牌堆的 UI 按钮释放时调用的可选回调。为 null（默认）时，按钮会回退到
        ///     <c>NCardPileScreen.ShowScreen</c>，即与原版 Draw / Discard / Exhaust 按钮相同的行为。
        ///     提供 delegate 可接入自定义 <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Capstones.ICapstoneScreen" />、
        ///     检查牌堆，或完全不做任何事。
        /// </summary>
        /// <remarks>
        ///     The context exposes helpers — <see cref="ModCardPileOpenContext.ShowDefaultPileScreen" /> runs
        ///     the default behaviour, while <see cref="ModCardPileOpenContext.OpenCapstoneScreen" /> mounts a
        ///     custom screen through <c>NCapstoneContainer</c>. The callback is *not* invoked when the pile is
        ///     empty; in that case the empty-pile thought bubble is shown and re-clicking an already-open
        ///     default pile screen continues to toggle it closed before the callback runs.
        ///     context 暴露 helper：<see cref="ModCardPileOpenContext.ShowDefaultPileScreen" /> 运行默认行为，
        ///     <see cref="ModCardPileOpenContext.OpenCapstoneScreen" /> 通过 <c>NCapstoneContainer</c> 挂载自定义画面。
        ///     牌堆为空时不会调用此回调；此时会显示空牌堆 thought bubble，并且再次点击已打开的默认牌堆 screen
        ///     会继续先将其关闭。
        /// </remarks>
        public Action<ModCardPileOpenContext>? OnOpen { get; init; }

        /// <summary>
        ///     Optional resolver called for each card fly-in to this pile, allowing mods to provide a dynamic
        ///     target position for the tail/trail endpoint. Return null to use the default layout position.
        ///     每次卡牌飞入此牌堆时调用的可选 resolver，允许 mod 为 tail/trail endpoint 提供动态目标位置。
        ///     返回 null 表示使用默认布局位置。
        /// </summary>
        public Func<ModCardPileFlightTargetContext, Vector2?>? FlightTargetPositionResolver { get; init; }

        /// <summary>
        ///     Optional resolver called when a shuffle-style fly visual starts from this pile, allowing mods to
        ///     provide a dynamic source/start position. Return null to use the default layout position.
        ///     shuffle 风格飞行动画从此牌堆开始时调用的可选解析器，允许 mod 提供动态源/起点位置。
        ///     返回 null 表示使用默认布局位置。
        /// </summary>
        public Func<ModCardPileFlightStartContext, Vector2?>? FlightStartPositionResolver { get; init; }
    }
}
