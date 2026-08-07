using STS2RitsuLib.CardPiles;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Declaratively registers a mod card pile through <see cref="ModCardPileRegistry" />. Apply it to a
    ///         concrete type in the mod assembly; that type may implement <see cref="IModCardPileHandler" /> to
    ///         handle opening the pile.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过 <see cref="ModCardPileRegistry" /> 声明式注册模组牌堆。请将此特性用于模组程序集中的
    ///         具体类型；该类型可实现 <see cref="IModCardPileHandler" /> 以处理牌堆打开操作。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Options mirror <see cref="ModCardPileSpec" />. The hover-tip title, description, and empty-pile
    ///         message use <see cref="ModCardPileSpec.HoverTipLocTable" /> keys <c>"{id}.title"</c>,
    ///         <c>"{id}.description"</c>, and <c>"{id}.empty"</c>, where <c>id</c> is the qualified pile ID.
    ///         Add those translations to <c>static_hover_tips.json</c>.
    ///     </para>
    ///     <para xml:lang="en">
    ///         <see cref="AnchorKind" /> selects the placement mode. The offset and custom-coordinate properties
    ///         supply its numeric values. <see cref="ModCardPileAnchorKind.StyleDefault" /> uses automatic
    ///         same-style placement.
    ///     </para>
    ///     <para xml:lang="en">
    ///         If the annotated type implements <see cref="IModCardPileHandler" />, RitsuLib creates one instance
    ///         through its public parameterless constructor and assigns <see cref="IModCardPileHandler.OnOpen" />
    ///         to <see cref="ModCardPileSpec.OnOpen" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         各选项与 <see cref="ModCardPileSpec" /> 对应。悬停提示标题、描述和空牌堆消息分别使用
    ///         <see cref="ModCardPileSpec.HoverTipLocTable" /> 表中的 <c>"{id}.title"</c>、
    ///         <c>"{id}.description"</c> 和 <c>"{id}.empty"</c>；其中 <c>id</c> 是限定后的牌堆 ID。
    ///         请在 <c>static_hover_tips.json</c> 中添加这些翻译。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="AnchorKind" /> 选择放置模式，偏移与自定义坐标属性提供对应数值。
    ///         <see cref="ModCardPileAnchorKind.StyleDefault" /> 会自动排列同样式牌堆。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         如果标注类型实现 <see cref="IModCardPileHandler" />，RitsuLib 会通过其公共无参构造函数创建
    ///         一个实例，并将 <see cref="IModCardPileHandler.OnOpen" /> 赋给
    ///         <see cref="ModCardPileSpec.OnOpen" />。
    ///     </para>
    /// </remarks>
    /// <param name="localPileStem">
    ///     <para xml:lang="en">Local pile stem within the owning mod's namespace.</para>
    ///     <para xml:lang="zh-CN">归属模组命名空间内的本地牌堆名称。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOwnedCardPileAttribute(string localPileStem) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Local pile stem within the owning mod's namespace.</para>
        ///     <para xml:lang="zh-CN">归属模组命名空间内的本地牌堆名称。</para>
        /// </summary>
        public string LocalPileStem { get; } = localPileStem;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Lifetime scope. Defaults to <see cref="ModCardPileScope.CombatOnly" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         生命周期范围。默认为 <see cref="ModCardPileScope.CombatOnly" />。
        ///     </para>
        /// </summary>
        public ModCardPileScope Scope { get; set; } = ModCardPileScope.CombatOnly;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Presentation style. Defaults to <see cref="ModCardPileUiStyle.Headless" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         展示样式。默认为 <see cref="ModCardPileUiStyle.Headless" />。
        ///     </para>
        /// </summary>
        public ModCardPileUiStyle Style { get; set; } = ModCardPileUiStyle.Headless;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Placement mode. Defaults to <see cref="ModCardPileAnchorKind.StyleDefault" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         放置模式。默认为 <see cref="ModCardPileAnchorKind.StyleDefault" />。
        ///     </para>
        /// </summary>
        public ModCardPileAnchorKind AnchorKind { get; set; } = ModCardPileAnchorKind.StyleDefault;

        /// <summary>
        ///     <para xml:lang="en">Additional X offset from the resolved anchor position, in pixels.</para>
        ///     <para xml:lang="zh-CN">相对解析后锚点位置增加的 X 轴像素偏移。</para>
        /// </summary>
        public float AnchorOffsetX { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Additional Y offset from the resolved anchor position, in pixels.</para>
        ///     <para xml:lang="zh-CN">相对解析后锚点位置增加的 Y 轴像素偏移。</para>
        /// </summary>
        public float AnchorOffsetY { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         X coordinate in the mount parent's local space where the point selected by
        ///         <see cref="AnchorCustomPivotX" /> and <see cref="AnchorCustomPivotY" /> is placed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <see cref="ModCardPileAnchorKind.Custom" /> 时，所选基准点在挂载父节点局部坐标系中的
        ///         X 坐标；基准点由 <see cref="AnchorCustomPivotX" /> 和 <see cref="AnchorCustomPivotY" /> 指定。
        ///     </para>
        /// </summary>
        public float AnchorCustomX { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Y coordinate in the mount parent's local space where the selected pivot point is placed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <see cref="ModCardPileAnchorKind.Custom" /> 时，所选基准点在挂载父节点局部坐标系中的 Y 坐标。
        ///     </para>
        /// </summary>
        public float AnchorCustomY { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Horizontal pivot fraction, normally from <c>0</c> to <c>1</c>, for
        ///         <see cref="ModCardPileAnchorKind.Custom" />. The default <c>0</c> uses the left edge.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <see cref="ModCardPileAnchorKind.Custom" /> 使用的水平枢轴比例，通常为 <c>0</c> 到
        ///         <c>1</c>；默认值 <c>0</c> 使用左边缘。
        ///     </para>
        /// </summary>
        public float AnchorCustomPivotX { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Vertical pivot fraction, normally from <c>0</c> to <c>1</c>, for
        ///         <see cref="ModCardPileAnchorKind.Custom" />. The default <c>0</c> uses the top edge.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <see cref="ModCardPileAnchorKind.Custom" /> 使用的垂直枢轴比例，通常为 <c>0</c> 到
        ///         <c>1</c>；默认值 <c>0</c> 使用上边缘。
        ///     </para>
        /// </summary>
        public float AnchorCustomPivotY { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Godot resource path for the pile icon (for example, <c>res://my_mod/icons/my_pile.png</c>).
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         牌堆图标的 Godot 资源路径（例如 <c>res://my_mod/icons/my_pile.png</c>）。
        ///     </para>
        /// </summary>
        public string? IconPath { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional input action IDs forwarded to <c>NCardPileScreen.ShowScreen</c>. Each array element
        ///         is one ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         转发给 <c>NCardPileScreen.ShowScreen</c> 的可选输入操作 ID；每个数组元素表示一个 ID。
        ///     </para>
        /// </summary>
        public string[]? Hotkeys { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Only meaningful for <see cref="ModCardPileUiStyle.ExtraHand" />: when
        ///         <see langword="true" />, cards in the pile are rendered as <c>NCard</c> nodes inside its container.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         仅对 <see cref="ModCardPileUiStyle.ExtraHand" /> 有意义：为 <see langword="true" /> 时，
        ///         牌堆中的卡牌会在容器内渲染为 <c>NCard</c> 节点。
        ///     </para>
        /// </summary>
        public bool CardShouldBeVisible { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Built-in extra-hand arrangement direction. Defaults to the vanilla hand layout.</para>
        ///     <para xml:lang="zh-CN">额外手牌的内置排列方向。默认为原版手牌布局。</para>
        /// </summary>
        public ModExtraHandLayoutDirection ExtraHandDirection { get; set; }
            = ModExtraHandLayoutDirection.VanillaHand;

        /// <summary>
        ///     <para xml:lang="en">Horizontal or vertical extra-hand spacing in pixels. Defaults to <c>110</c>.</para>
        ///     <para xml:lang="zh-CN">额外手牌的水平或垂直像素间距。默认为 <c>110</c>。</para>
        /// </summary>
        public float ExtraHandSpacing { get; set; } = 110f;

        /// <summary>
        ///     <para xml:lang="en">Normal extra-hand card scale. Defaults to <c>0.65</c>.</para>
        ///     <para xml:lang="zh-CN">额外手牌中卡牌的常态缩放比例。默认为 <c>0.65</c>。</para>
        /// </summary>
        public float ExtraHandCardScale { get; set; } = 0.65f;

        /// <summary>
        ///     <para xml:lang="en">Focused extra-hand card scale. Defaults to <c>1</c>.</para>
        ///     <para xml:lang="zh-CN">额外手牌中卡牌获得焦点时的缩放比例。默认为 <c>1</c>。</para>
        /// </summary>
        public float ExtraHandHoverScale { get; set; } = 1f;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Whether extra-hand cards use vanilla playable-glow rules. Defaults to <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         额外手牌卡牌是否使用原版可打出发光规则。默认为 <see langword="true" />。
        ///     </para>
        /// </summary>
        public bool ExtraHandShowPlayableGlow { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Whether the extra-hand pile has manual-card-play capability through the vanilla pipeline. This
        ///         also determines each new container's initial runtime availability. Defaults to
        ///         <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         额外手牌牌堆是否具备通过原版流程手动出牌的能力；该值也决定每个新容器的运行时可用性
        ///         初始值。默认为 <see langword="true" />。
        ///     </para>
        /// </summary>
        public bool ExtraHandAllowCardPlay { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         X component of the screen-space offset applied after automatic hover-tip placement.
        ///     </para>
        ///     <para xml:lang="zh-CN">自动放置悬停提示后应用的屏幕空间 X 轴偏移。</para>
        /// </summary>
        public float HoverTipOffsetX { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Y component of the screen-space offset applied after automatic hover-tip placement.
        ///     </para>
        ///     <para xml:lang="zh-CN">自动放置悬停提示后应用的屏幕空间 Y 轴偏移。</para>
        /// </summary>
        public float HoverTipOffsetY { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Hover-tip placement relative to the pile button.</para>
        ///     <para xml:lang="zh-CN">悬停提示相对于牌堆按钮的放置位置。</para>
        /// </summary>
        public ModCardPileHoverTipPlacement HoverTipPlacement { get; set; }
    }
}
