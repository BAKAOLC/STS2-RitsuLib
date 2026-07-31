using Godot;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Identifies the layout slot used by a mod card-pile control or its position fallback.
    ///     </para>
    ///     <para xml:lang="zh-CN">标识模组卡牌牌堆控件或其位置回退所使用的布局槽位。</para>
    /// </summary>
    public enum ModCardPileAnchorKind
    {
        /// <summary>
        ///     <para xml:lang="en">Uses the default slot for the selected UI style.</para>
        ///     <para xml:lang="zh-CN">使用所选界面样式的默认槽位。</para>
        /// </summary>
        StyleDefault = 0,

        /// <summary>
        ///     <para xml:lang="en">Places the control in the row extending right from the draw-pile button.</para>
        ///     <para xml:lang="zh-CN">将控件放在从抽牌堆按钮向右延伸的行中。</para>
        /// </summary>
        BottomLeftPrimary = 1,

        /// <summary>
        ///     <para xml:lang="en">Places the control in the row extending right from the discard-pile button.</para>
        ///     <para xml:lang="zh-CN">将控件放在从弃牌堆按钮向右延伸的行中。</para>
        /// </summary>
        BottomLeftSecondary = 2,

        /// <summary>
        ///     <para xml:lang="en">Places the control in the row extending left from the exhaust-pile button.</para>
        ///     <para xml:lang="zh-CN">将控件放在从消耗牌堆按钮向左延伸的行中。</para>
        /// </summary>
        BottomRightPrimary = 3,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Places the control after the primary bottom-right slots while reserving the first primary slot.
        ///     </para>
        ///     <para xml:lang="zh-CN">将控件放在右下主槽位之后，并始终保留第一个主槽位。</para>
        /// </summary>
        BottomRightSecondary = 4,

        /// <summary>
        ///     <para xml:lang="en">Places the control immediately after the base-game deck button.</para>
        ///     <para xml:lang="zh-CN">将控件放在原版牌组按钮之后。</para>
        /// </summary>
        TopBarAfterDeck = 5,

        /// <summary>
        ///     <para xml:lang="en">Places the control before the top bar's modifier group.</para>
        ///     <para xml:lang="zh-CN">将控件放在顶部栏的修改器组之前。</para>
        /// </summary>
        TopBarBeforeModifiers = 6,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Centers an <see cref="ModCardPileUiStyle.ExtraHand" /> container above the base-game hand.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModCardPileUiStyle.ExtraHand" /> 容器居中放在原版手牌上方。
        ///     </para>
        /// </summary>
        ExtraHandAbove = 7,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Centers an <see cref="ModCardPileUiStyle.ExtraHand" /> container below the base-game hand.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModCardPileUiStyle.ExtraHand" /> 容器居中放在原版手牌下方。
        ///     </para>
        /// </summary>
        ExtraHandBelow = 8,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Uses <see cref="ModCardPileAnchor.CustomPosition" /> and
        ///         <see cref="ModCardPileAnchor.CustomAuthoringPivot" /> to place the control explicitly.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <see cref="ModCardPileAnchor.CustomPosition" /> 和
        ///         <see cref="ModCardPileAnchor.CustomAuthoringPivot" /> 明确放置控件。
        ///     </para>
        /// </summary>
        Custom = 9,
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes a card-pile control's layout slot, offset, and optional custom authoring point.
    ///     </para>
    ///     <para xml:lang="zh-CN">描述卡牌牌堆控件的布局槽位、偏移和可选自定义定位点。</para>
    /// </summary>
    /// <param name="Kind">
    ///     <para xml:lang="en">The layout slot used by the card-pile control.</para>
    ///     <para xml:lang="zh-CN">卡牌牌堆控件使用的布局槽位。</para>
    /// </param>
    /// <param name="Offset">
    ///     <para xml:lang="en">
    ///         An additional offset in the parent control's local coordinate system.
    ///     </para>
    ///     <para xml:lang="zh-CN">父控件局部坐标系中的额外偏移。</para>
    /// </param>
    /// <param name="CustomPosition">
    ///     <para xml:lang="en">
    ///         For <see cref="ModCardPileAnchorKind.Custom" />, the authoring point in the parent control's local
    ///         coordinate system; ignored for other anchor kinds.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使用 <see cref="ModCardPileAnchorKind.Custom" /> 时，父控件局部坐标系中的定位点；
    ///         其他锚点类型会忽略此值。
    ///     </para>
    /// </param>
    /// <param name="CustomAuthoringPivot">
    ///     <para xml:lang="en">
    ///         For <see cref="ModCardPileAnchorKind.Custom" />, the component-wise fraction of the nominal control
    ///         size placed at <paramref name="CustomPosition" />. Common values are <c>(0, 0)</c> for the upper-left
    ///         corner, <c>(0.5, 0.5)</c> for the center, and <c>(1, 1)</c> for the lower-right corner.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使用 <see cref="ModCardPileAnchorKind.Custom" /> 时，放在 <paramref name="CustomPosition" />
    ///         处的名义控件尺寸分量比例。常用值包括表示左上角的 <c>(0, 0)</c>、表示中心的
    ///         <c>(0.5, 0.5)</c> 和表示右下角的 <c>(1, 1)</c>。
    ///     </para>
    /// </param>
    public readonly record struct ModCardPileAnchor(
        ModCardPileAnchorKind Kind,
        Vector2 Offset = default,
        Vector2 CustomPosition = default,
        Vector2 CustomAuthoringPivot = default)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an anchor with an offset and default custom-position values.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建带偏移且自定义位置值为默认值的锚点。</para>
        /// </summary>
        /// <param name="kind">
        ///     <para xml:lang="en">The layout slot used by the card-pile control.</para>
        ///     <para xml:lang="zh-CN">卡牌牌堆控件使用的布局槽位。</para>
        /// </param>
        /// <param name="offset">
        ///     <para xml:lang="en">The additional local offset.</para>
        ///     <para xml:lang="zh-CN">额外局部偏移。</para>
        /// </param>
        public ModCardPileAnchor(ModCardPileAnchorKind kind, Vector2 offset)
            : this(kind, offset, default, default)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an anchor whose custom authoring pivot is <see cref="PivotUpperLeft" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建自定义定位枢轴为 <see cref="PivotUpperLeft" /> 的锚点。</para>
        /// </summary>
        /// <param name="kind">
        ///     <para xml:lang="en">The layout slot used by the card-pile control.</para>
        ///     <para xml:lang="zh-CN">卡牌牌堆控件使用的布局槽位。</para>
        /// </param>
        /// <param name="offset">
        ///     <para xml:lang="en">The additional local offset.</para>
        ///     <para xml:lang="zh-CN">额外局部偏移。</para>
        /// </param>
        /// <param name="customPosition">
        ///     <para xml:lang="en">The custom authoring point in the parent control's local coordinates.</para>
        ///     <para xml:lang="zh-CN">父控件局部坐标系中的自定义定位点。</para>
        /// </param>
        public ModCardPileAnchor(ModCardPileAnchorKind kind, Vector2 offset, Vector2 customPosition)
            : this(kind, offset, customPosition, default)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an anchor with separate horizontal and vertical pivot fractions.</para>
        ///     <para xml:lang="zh-CN">创建分别指定水平和垂直枢轴比例的锚点。</para>
        /// </summary>
        /// <param name="kind">
        ///     <para xml:lang="en">The layout slot used by the card-pile control.</para>
        ///     <para xml:lang="zh-CN">卡牌牌堆控件使用的布局槽位。</para>
        /// </param>
        /// <param name="offset">
        ///     <para xml:lang="en">The additional local offset.</para>
        ///     <para xml:lang="zh-CN">额外局部偏移。</para>
        /// </param>
        /// <param name="customPosition">
        ///     <para xml:lang="en">The custom authoring point in the parent control's local coordinates.</para>
        ///     <para xml:lang="zh-CN">父控件局部坐标系中的自定义定位点。</para>
        /// </param>
        /// <param name="customAuthoringPivotX">
        ///     <para xml:lang="en">The horizontal fraction of the nominal control size at the authoring point.</para>
        ///     <para xml:lang="zh-CN">定位点对应的名义控件宽度比例。</para>
        /// </param>
        /// <param name="customAuthoringPivotY">
        ///     <para xml:lang="en">The vertical fraction of the nominal control size at the authoring point.</para>
        ///     <para xml:lang="zh-CN">定位点对应的名义控件高度比例。</para>
        /// </param>
        public ModCardPileAnchor(
            ModCardPileAnchorKind kind,
            Vector2 offset,
            Vector2 customPosition,
            float customAuthoringPivotX,
            float customAuthoringPivotY)
            : this(kind, offset, customPosition, new(customAuthoringPivotX, customAuthoringPivotY))
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the pivot that places the control's upper-left corner at the authoring point.</para>
        ///     <para xml:lang="zh-CN">获取将控件左上角放在定位点的枢轴。</para>
        /// </summary>
        public static Vector2 PivotUpperLeft => Vector2.Zero;

        /// <summary>
        ///     <para xml:lang="en">Gets the pivot that places the control's center at the authoring point.</para>
        ///     <para xml:lang="zh-CN">获取将控件中心放在定位点的枢轴。</para>
        /// </summary>
        public static Vector2 PivotCenter => Vector2.One * 0.5f;

        /// <summary>
        ///     <para xml:lang="en">Gets an anchor that uses the selected UI style's default slot.</para>
        ///     <para xml:lang="zh-CN">获取使用所选界面样式默认槽位的锚点。</para>
        /// </summary>
        public static ModCardPileAnchor Default { get; } = new(ModCardPileAnchorKind.StyleDefault);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a custom anchor whose upper-left corner is <paramref name="upperLeftPosition" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建左上角位于 <paramref name="upperLeftPosition" /> 的自定义锚点。
        ///     </para>
        /// </summary>
        /// <param name="upperLeftPosition">
        ///     <para xml:lang="en">The upper-left position in the parent control's local coordinates.</para>
        ///     <para xml:lang="zh-CN">父控件局部坐标系中的左上角位置。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The custom anchor.</para>
        ///     <para xml:lang="zh-CN">自定义锚点。</para>
        /// </returns>
        public static ModCardPileAnchor AtPosition(Vector2 upperLeftPosition)
        {
            return new(ModCardPileAnchorKind.Custom, Vector2.Zero, upperLeftPosition);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a custom anchor that places <paramref name="chromePivotFraction" /> of the nominal control
        ///         size at <paramref name="authoringPoint" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建自定义锚点，将名义控件尺寸的 <paramref name="chromePivotFraction" /> 比例位置放在
        ///         <paramref name="authoringPoint" />。
        ///     </para>
        /// </summary>
        /// <param name="authoringPoint">
        ///     <para xml:lang="en">The authoring point in the parent control's local coordinates.</para>
        ///     <para xml:lang="zh-CN">父控件局部坐标系中的定位点。</para>
        /// </param>
        /// <param name="chromePivotFraction">
        ///     <para xml:lang="en">The component-wise fraction of the nominal control size at the authoring point.</para>
        ///     <para xml:lang="zh-CN">定位点对应的名义控件尺寸分量比例。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The custom anchor.</para>
        ///     <para xml:lang="zh-CN">自定义锚点。</para>
        /// </returns>
        public static ModCardPileAnchor AtPivot(Vector2 authoringPoint, Vector2 chromePivotFraction)
        {
            return new(ModCardPileAnchorKind.Custom, Vector2.Zero, authoringPoint, chromePivotFraction);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a custom anchor whose center is <paramref name="centerPosition" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建中心位于 <paramref name="centerPosition" /> 的自定义锚点。</para>
        /// </summary>
        /// <param name="centerPosition">
        ///     <para xml:lang="en">The center position in the parent control's local coordinates.</para>
        ///     <para xml:lang="zh-CN">父控件局部坐标系中的中心位置。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The custom anchor.</para>
        ///     <para xml:lang="zh-CN">自定义锚点。</para>
        /// </returns>
        public static ModCardPileAnchor AtCenter(Vector2 centerPosition)
        {
            return new(ModCardPileAnchorKind.Custom, Vector2.Zero, centerPosition, PivotCenter);
        }
    }
}
