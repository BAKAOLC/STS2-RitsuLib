using STS2RitsuLib.CardPiles;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     Declaratively registers a mod card pile (see <see cref="ModCardPileRegistry" />). Place on any
    ///     concrete class inside your mod assembly; the type itself acts as the registration carrier and
    ///     can optionally implement <see cref="IModCardPileHandler" /> to customise button-click behaviour.
    ///     声明式注册 mod 牌堆（参见 <see cref="ModCardPileRegistry" />）。将其放在你的
    ///     mod 程序集中的任意具体类上；该类型本身作为注册载体，
    ///     并且可以选择实现 <see cref="IModCardPileHandler" /> 来自定义按钮点击行为。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Field semantics mirror <see cref="ModCardPileSpec" />. Localization follows the vanilla
    ///         pile convention — hover-tip title / description and the empty-pile thought bubble are all
    ///         resolved against <see cref="ModCardPileSpec.HoverTipLocTable" /> using the keys
    ///         <c>"{id}.title"</c>, <c>"{id}.description"</c> and <c>"{id}.empty"</c> where <c>id</c> is the
    ///         qualified pile id. Because
    ///         mods can only extend existing loc tables (not create new ones) the table itself is not
    ///         configurable; author your translations in <c>static_hover_tips.json</c>.
    ///     </para>
    ///     <para>
    ///         Anchor values split across <see cref="AnchorKind" /> plus optional
    ///         <see cref="AnchorOffsetX" /> / <see cref="AnchorOffsetY" /> /
    ///         <see cref="AnchorCustomX" /> / <see cref="AnchorCustomY" />; when <see cref="AnchorKind" />
    ///         is left at <see cref="ModCardPileAnchorKind.StyleDefault" /> the pile auto-stacks per the
    ///         "explicit anchor + auto-stack fallback" rule.
    ///         <see cref="AnchorOffsetY" /> / <see cref="AnchorCustomX" /> / <see cref="AnchorCustomY" />；
    ///     </para>
    ///     <para>
    ///         If the annotated type implements <see cref="IModCardPileHandler" />, ritsulib creates a
    ///         single instance (parameterless constructor required) and wires its
    ///         <see cref="IModCardPileHandler.OnOpen" /> method into
    ///         <see cref="ModCardPileSpec.OnOpen" />.
    ///         <see cref="ModCardPileSpec.OnOpen" />。
    ///     </para>
    ///     <para>
    ///         字段语义对应 <see cref="ModCardPileSpec" />。本地化遵循原版
    ///         牌堆约定：悬停提示标题/描述以及空牌堆气泡都会
    ///         基于 <see cref="ModCardPileSpec.HoverTipLocTable" /> 解析，使用 key
    ///         <c>"{id}.title"</c>、<c>"{id}.description"</c> 和 <c>"{id}.empty"</c>，其中 <c>id</c> 是
    ///         限定牌堆 id。由于
    ///         mod 只能扩展现有本地化表（不能创建新表），表本身不可
    ///         配置；请在 <c>static_hover_tips.json</c> 中编写翻译。
    ///     </para>
    ///     <para>
    ///         锚点值拆分为 <see cref="AnchorKind" /> 加可选的
    ///         <see cref="AnchorOffsetX" />
    ///         <see cref="AnchorOffsetY" />
    ///         <see cref="AnchorCustomX" />
    ///         <see cref="AnchorCustomY" />；当 <see cref="AnchorKind" />
    ///         保持为 <see cref="ModCardPileAnchorKind.StyleDefault" /> 时，牌堆会按
    ///         “显式锚点 + 自动堆叠回退”规则自动堆叠。
    ///         <see cref="AnchorOffsetY" />
    ///         <see cref="AnchorCustomX" />
    ///         <see cref="AnchorCustomY" />；
    ///     </para>
    ///     <para>
    ///         如果带注解的类型实现 <see cref="IModCardPileHandler" />，ritsulib 会创建
    ///         单个实例（需要无参构造函数），并将其
    ///         <see cref="IModCardPileHandler.OnOpen" /> 方法接入
    ///         <see cref="ModCardPileSpec.OnOpen" />。
    ///         <see cref="ModCardPileSpec.OnOpen" />。
    ///     </para>
    /// </remarks>
    /// <param name="localPileStem">
    ///     Local, mod-scoped pile stem (matches <c>RegisterOwned(localStem, ...)</c>).
    ///     mod 局部范围内的牌堆词干（匹配 <c>RegisterOwned(localStem, ...)</c>）。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOwnedCardPileAttribute(string localPileStem) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Local, mod-scoped pile stem.
        ///     mod 局部范围内的牌堆词干。
        /// </summary>
        public string LocalPileStem { get; } = localPileStem;

        /// <summary>
        ///     Lifetime scope (defaults to <see cref="ModCardPileScope.CombatOnly" />).
        ///     生命周期作用域（默认 <see cref="ModCardPileScope.CombatOnly" />）。
        /// </summary>
        public ModCardPileScope Scope { get; set; } = ModCardPileScope.CombatOnly;

        /// <summary>
        ///     UI chrome family (defaults to <see cref="ModCardPileUiStyle.Headless" />).
        ///     UI chrome 类型族（默认 <see cref="ModCardPileUiStyle.Headless" />）。
        /// </summary>
        public ModCardPileUiStyle Style { get; set; } = ModCardPileUiStyle.Headless;

        /// <summary>
        ///     Anchor slot hint (defaults to <see cref="ModCardPileAnchorKind.StyleDefault" />).
        ///     锚点槽位提示（默认 <see cref="ModCardPileAnchorKind.StyleDefault" />）。
        /// </summary>
        public ModCardPileAnchorKind AnchorKind { get; set; } = ModCardPileAnchorKind.StyleDefault;

        /// <summary>
        ///     Extra X pixels added on top of the resolved anchor position.
        ///     在解析出的 anchor 位置上额外增加的 X 像素。
        /// </summary>
        public float AnchorOffsetX { get; set; }

        /// <summary>
        ///     Extra Y pixels added on top of the resolved anchor position.
        ///     在解析出的 anchor 位置上额外增加的 Y 像素。
        /// </summary>
        public float AnchorOffsetY { get; set; }

        /// <summary>
        ///     X authoring coordinate in the mount parent's local space when <see cref="AnchorKind" /> is
        ///     <see cref="ModCardPileAnchorKind.Custom" /> (paired with <see cref="AnchorCustomPivotX" /> /
        ///     <see cref="AnchorCustomPivotY" /> as chrome landmark fractions — see <see cref="ModCardPileAnchor" />).
        ///     <see cref="ModCardPileAnchor" />）。
        ///     当 <see cref="AnchorKind" /> 为 <see cref="ModCardPileAnchorKind.Custom" /> 时，mount parent 本地空间中的
        ///     X 编写坐标（与作为 chrome 标志比例的 <see cref="AnchorCustomPivotX" /> /
        ///     <see cref="AnchorCustomPivotY" /> 配对；参见 <see cref="ModCardPileAnchor" />）。
        ///     <see cref="ModCardPileAnchor" />）。
        /// </summary>
        public float AnchorCustomX { get; set; }

        /// <summary>
        ///     Y authoring coordinate when <see cref="AnchorKind" /> is <see cref="ModCardPileAnchorKind.Custom" />.
        ///     当 <see cref="AnchorKind" /> 为 <see cref="ModCardPileAnchorKind.Custom" /> 时的 Y 编写坐标。
        /// </summary>
        public float AnchorCustomY { get; set; }

        /// <summary>
        ///     X-axis fraction (typically 0..1) on nominal chrome horizontal extent when
        ///     <see cref="AnchorKind" /> is <see cref="ModCardPileAnchorKind.Custom" />; default <c>0</c>
        ///     means upper-left anchored.
        ///     当 <see cref="AnchorKind" /> 为 <see cref="ModCardPileAnchorKind.Custom" /> 时，名义 chrome 水平范围上的
        ///     X 轴比例（通常为 0..1）；默认 <c>0</c>
        ///     表示左上角锚定。
        /// </summary>
        public float AnchorCustomPivotX { get; set; }

        /// <summary>
        ///     Y chrome landmark fraction for <see cref="ModCardPileAnchorKind.Custom" />; default <c>0</c>
        ///     with <see cref="AnchorCustomPivotX" /> behaves like upper-left authoring.
        ///     <see cref="ModCardPileAnchorKind.Custom" /> 使用的 Y chrome 标志比例；默认 <c>0</c>
        ///     与 <see cref="AnchorCustomPivotX" /> 组合时表现为左上角编写。
        /// </summary>
        public float AnchorCustomPivotY { get; set; }

        /// <summary>
        ///     Godot resource path for the pile icon (e.g. <c>res://my_mod/icons/my_pile.png</c>).
        ///     牌堆图标的 Godot ResourcePath（例如 <c>res://my_mod/icons/my_pile.png</c>）。
        /// </summary>
        public string? IconPath { get; set; }

        /// <summary>
        ///     Optional hotkey ids forwarded to <c>NCardPileScreen.ShowScreen</c>. Separate ids with commas
        ///     at the call site (<c>Hotkeys = new[] { "combat_pile_deck" }</c>).
        ///     （<c>Hotkeys = new[] { "combat_pile_deck" }</c>）。
        ///     转发给 <c>NCardPileScreen.ShowScreen</c> 的可选 hotkey id。在调用处用逗号分隔多个 id
        /// </summary>
        public string[]? Hotkeys { get; set; }

        /// <summary>
        ///     Only meaningful for <see cref="ModCardPileUiStyle.ExtraHand" />: when true, cards added to
        ///     the pile are rendered as <c>NCard</c> nodes inside the pile container.
        ///     仅对 <see cref="ModCardPileUiStyle.ExtraHand" /> 有意义：为 true 时，加入
        ///     牌堆的卡牌会在牌堆容器内渲染为 <c>NCard</c> 节点。
        /// </summary>
        public bool CardShouldBeVisible { get; set; }

        /// <summary>
        ///     Built-in extra-hand arrangement direction. Defaults to the vanilla hand layout.
        ///     额外手牌的内置排列方向。默认为原版手牌布局。
        /// </summary>
        public ModExtraHandLayoutDirection ExtraHandDirection { get; set; }
            = ModExtraHandLayoutDirection.VanillaHand;

        /// <summary>
        ///     Horizontal/vertical extra-hand spacing in pixels. Defaults to <c>110</c>.
        ///     水平/垂直额外手牌的像素间距。默认为 <c>110</c>。
        /// </summary>
        public float ExtraHandSpacing { get; set; } = 110f;

        /// <summary>
        ///     Horizontal/vertical extra-hand normal card scale. Defaults to <c>0.65</c>.
        ///     水平/垂直额外手牌的常态卡牌缩放。默认为 <c>0.65</c>。
        /// </summary>
        public float ExtraHandCardScale { get; set; } = 0.65f;

        /// <summary>
        ///     Horizontal/vertical extra-hand focused card scale. Defaults to <c>1</c>.
        ///     水平/垂直额外手牌的焦点卡牌缩放。默认为 <c>1</c>。
        /// </summary>
        public float ExtraHandHoverScale { get; set; } = 1f;

        /// <summary>
        ///     Whether extra-hand cards use vanilla playable glow rules. Defaults to true.
        ///     额外手牌卡牌是否使用原版可打出发光规则。默认为 true。
        /// </summary>
        public bool ExtraHandShowPlayableGlow { get; set; } = true;

        /// <summary>
        ///     Whether extra-hand cards can be manually played through the vanilla pipeline. Defaults to true.
        ///     额外手牌卡牌是否可通过原版流程手动打出。默认为 true。
        /// </summary>
        public bool ExtraHandAllowCardPlay { get; set; } = true;

        /// <summary>
        ///     Added to the hover tip position after automatic placement (see <see cref="ModCardPileSpec.HoverTipScreenOffset" />
        ///     ).
        ///     自动放置后追加到悬停提示位置的偏移（参见 <see cref="ModCardPileSpec.HoverTipScreenOffset" />
        ///     ）。
        /// </summary>
        public float HoverTipOffsetX { get; set; }

        /// <summary>
        ///     Added to the hover tip position after automatic placement (see <see cref="ModCardPileSpec.HoverTipScreenOffset" />
        ///     ).
        ///     自动放置后追加到悬停提示位置的偏移（参见 <see cref="ModCardPileSpec.HoverTipScreenOffset" />
        ///     ）。
        /// </summary>
        public float HoverTipOffsetY { get; set; }

        /// <summary>
        ///     Hover tip anchor relative to the pile button (see <see cref="ModCardPileSpec.HoverTipPlacement" />).
        ///     悬停提示相对于牌堆按钮的锚点（参见 <see cref="ModCardPileSpec.HoverTipPlacement" />）。
        /// </summary>
        public ModCardPileHoverTipPlacement HoverTipPlacement { get; set; }
    }
}
