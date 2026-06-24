using System.Numerics;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     Declarative spec for a mod-owned top-bar button. Mirrors <c>ModCardPileSpec</c>'s structure but is
    ///     fully decoupled from card piles — the only contract is "show a button next to the vanilla deck
    ///     button, hover-tip via <c>static_hover_tips</c>, click runs a callback".
    ///     mod 拥有的顶部栏按钮的声明式 spec。结构对应 <c>ModCardPileSpec</c>，但
    ///     与牌堆完全解耦；唯一契约是“在原版牌组
    ///     按钮旁显示一个按钮，通过 <c>static_hover_tips</c> 提供悬停提示，点击时运行回调”。
    /// </summary>
    /// <remarks>
    ///     Localization follows the same <c>static_hover_tips.{id}.title</c> / <c>.description</c>
    ///     convention as mod card piles, where <c>id</c> is the qualified button id.
    ///     本地化遵循与 mod 牌堆相同的 <c>static_hover_tips.{id}.title</c> / <c>.description</c>
    ///     约定，其中 <c>id</c> 是限定后的按钮 id。
    /// </remarks>
    public sealed record ModTopBarButtonSpec
    {
        /// <summary>
        ///     Vanilla loc table that the button's hover-tip resolves against.
        ///     按钮悬停提示所解析的原版本地化表。
        /// </summary>
        public const string HoverTipLocTable = ModTopBarButtonLocConstants.HoverTipLocTable;

        /// <summary>
        ///     Godot resource path for the button icon (for example <c>res://my_mod/icon.png</c>).
        ///     按钮图标的 Godot 资源路径（例如 <c>res://my_mod/icon.png</c>）。
        /// </summary>
        public string? IconPath { get; init; }

        /// <summary>
        ///     Sort order within a single mod's top-bar buttons; lower values render closer to the vanilla
        ///     deck button.
        ///     单个 mod 顶部栏按钮内的排序顺序；值越低，渲染位置越靠近原版
        ///     牌组按钮。
        /// </summary>
        public int Order { get; init; }

        /// <summary>
        ///     Extra pixel offset applied on top of the auto-stacked slot position. Use this for
        ///     fine-tuning when the default horizontal stacking layout doesn't match your icon.
        ///     应用在自动堆叠槽位置之上的额外像素偏移。用于
        ///     默认水平堆叠布局与图标不匹配时的微调。
        /// </summary>
        public Vector2 Offset { get; init; }

        /// <summary>
        ///     Required click handler. Receives a <see cref="ModTopBarButtonContext" /> that exposes
        ///     <see cref="ModTopBarButtonContext.OpenCapstoneScreen" /> / related helpers.
        ///     必需点击处理器。接收 <see cref="ModTopBarButtonContext" />，它会暴露
        ///     <see cref="ModTopBarButtonContext.OpenCapstoneScreen" /> / 相关辅助方法。
        /// </summary>
        public Action<ModTopBarButtonContext>? OnClick { get; init; }

        /// <summary>
        ///     Optional predicate that decides whether the button is visible for the current player. When
        ///     null the button is always visible (once the top bar exists). Evaluated on
        ///     <see cref="Godot.Node._Process" /> to keep the visibility state in sync with combat/run state
        ///     changes.
        ///     可选谓词，用于决定按钮对当前玩家是否可见。当
        ///     为 null 时，按钮始终可见（顶部栏存在后）。在
        ///     <see cref="Godot.Node._Process" /> 上求值，以让可见性状态与战斗/跑局状态
        ///     变化保持同步。
        /// </summary>
        public Func<ModTopBarButtonContext, bool>? VisibleWhen { get; init; }

        /// <summary>
        ///     Optional predicate used by the button to decide when it should render in its "screen open"
        ///     rocking state (mirroring vanilla <c>NTopBarDeckButton</c> / <c>NTopBarMapButton</c>). Typical
        ///     usage is
        ///     <c>ctx =&gt; ModScreenService.CurrentCapstoneScreen is MyScreen</c>. When null the button never
        ///     enters the open state — pick this for one-shot "fire and forget" click handlers.
        ///     可选谓词，供按钮判断何时应渲染为“屏幕打开”
        ///     摇动状态（对应原版 <c>NTopBarDeckButton</c> / <c>NTopBarMapButton</c>）。典型
        ///     用法为
        ///     <c>ctx =&gt; ModScreenService.CurrentCapstoneScreen is MyScreen</c>。为 null 时按钮永远不会
        ///     进入打开状态；一次性“触发后即忘”的点击处理器应选择此项。
        /// </summary>
        public Func<ModTopBarButtonContext, bool>? IsOpenWhen { get; init; }

        /// <summary>
        ///     Optional provider used to populate the count badge under the button's icon. Called on
        ///     <see cref="Godot.Node._Process" /> — keep it cheap. When null, the count label is hidden
        ///     entirely, which is the right choice for fire-and-forget action buttons (menus, toggles).
        ///     When set, the button reuses the vanilla card-pile "number jumped up" bump animation so the
        ///     feedback feels identical to the player's deck / draw / discard counts.
        ///     可选提供器，用于填充按钮图标下方的计数徽章。在
        ///     <see cref="Godot.Node._Process" /> 上调用，应保持轻量。为 null 时，计数标签会被
        ///     完全隐藏，这适合 fire-and-forget 动作按钮（菜单、切换）。
        ///     设置后，按钮会复用原版牌堆“数字跳起”的弹性动画，使
        ///     反馈与玩家的牌组 / 抽牌堆 / 弃牌堆计数一致。
        /// </summary>
        public Func<ModTopBarButtonContext, int>? CountProvider { get; init; }
    }

    /// <summary>
    ///     Shared constants for the top-bar-button localization convention.
    ///     顶部栏按钮本地化约定的共享常量。
    /// </summary>
    internal static class ModTopBarButtonLocConstants
    {
        public const string HoverTipLocTable = "static_hover_tips";
    }
}
