using System.Numerics;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     <para xml:lang="en">Describes a mod-owned action button shown in the vanilla top bar.</para>
    ///     <para xml:lang="zh-CN">描述显示在原版顶部栏中、归属于模组的操作按钮。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Hover-tip localization uses <c>static_hover_tips</c> keys <c>{id}.title</c> and
    ///         <c>{id}.description</c>, where <c>id</c> is the registered global button ID.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         悬停提示本地化使用 <c>static_hover_tips</c> 中的 <c>{id}.title</c> 和
    ///         <c>{id}.description</c> 键，其中 <c>id</c> 是已注册的全局按钮 ID。
    ///     </para>
    /// </remarks>
    public sealed record ModTopBarButtonSpec
    {
        /// <summary>
        ///     <para xml:lang="en">Localization table used by the button's hover tip.</para>
        ///     <para xml:lang="zh-CN">按钮悬停提示使用的本地化表。</para>
        /// </summary>
        public const string HoverTipLocTable = ModTopBarButtonLocConstants.HoverTipLocTable;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Godot resource path for the button icon, for example <c>res://my_mod/icon.png</c>.
        ///         When omitted, RitsuLib attempts to clone the vanilla deck icon.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按钮图标的 Godot 资源路径，例如 <c>res://my_mod/icon.png</c>。
        ///         未指定时，RitsuLib 会尝试克隆原版牌组图标。
        ///     </para>
        /// </summary>
        public string? IconPath { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Global sort order among registered action buttons. Lower values appear closer to the
        ///         vanilla deck button.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         已注册操作按钮之间的全局排序值；值越小越靠近原版牌组按钮。
        ///     </para>
        /// </summary>
        public int Order { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Additional visual offset from the automatically arranged slot, in pixels.</para>
        ///     <para xml:lang="zh-CN">相对于自动排列槽位的额外视觉像素偏移。</para>
        /// </summary>
        public Vector2 Offset { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Required click handler. Registration fails if this property is <see langword="null" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         必需的点击回调。此属性为 <see langword="null" /> 时注册失败。
        ///     </para>
        /// </summary>
        public Action<ModTopBarButtonContext>? OnClick { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional visibility predicate evaluated during <see cref="Godot.Node._Process" />.
        ///         <see langword="null" /> keeps the button visible.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <see cref="Godot.Node._Process" /> 中求值的可选可见性谓词。
        ///         为 <see langword="null" /> 时按钮保持可见。
        ///     </para>
        /// </summary>
        public Func<ModTopBarButtonContext, bool>? VisibleWhen { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional predicate evaluated during <see cref="Godot.Node._Process" /> to control the
        ///         selected/open tilt used by vanilla top-bar buttons. <see langword="null" /> keeps the
        ///         button closed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <see cref="Godot.Node._Process" /> 中求值的可选谓词，用于控制原版顶部栏按钮使用的
        ///         选中或打开状态倾斜效果。为 <see langword="null" /> 时按钮保持关闭状态。
        ///     </para>
        /// </summary>
        public Func<ModTopBarButtonContext, bool>? IsOpenWhen { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional count provider evaluated during <see cref="Godot.Node._Process" />.
        ///         <see langword="null" /> or a negative result hides the count label. Increases use the
        ///         vanilla count-bump animation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <see cref="Godot.Node._Process" /> 中求值的可选数量提供器。
        ///         为 <see langword="null" /> 或返回负数时隐藏数量标签；数值增加时使用原版数量弹起动画。
        ///     </para>
        /// </summary>
        public Func<ModTopBarButtonContext, int>? CountProvider { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Localization constants used by mod top-bar buttons.</para>
    ///     <para xml:lang="zh-CN">模组顶部栏按钮使用的本地化常量。</para>
    /// </summary>
    internal static class ModTopBarButtonLocConstants
    {
        public const string HoverTipLocTable = "static_hover_tips";
    }
}
