using System.Numerics;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     <para xml:lang="en">Immutable definition of a registered mod top-bar button.</para>
    ///     <para xml:lang="zh-CN">已注册的模组顶部栏按钮的不可变定义。</para>
    /// </summary>
    public sealed record ModTopBarButtonDefinition
    {
        internal ModTopBarButtonDefinition(
            string modId,
            string id,
            string? iconPath,
            int order,
            Vector2 offset,
            Action<ModTopBarButtonContext> onClick,
            Func<ModTopBarButtonContext, bool>? visibleWhen,
            Func<ModTopBarButtonContext, bool>? isOpenWhen,
            Func<ModTopBarButtonContext, int>? countProvider)
        {
            ModId = modId;
            Id = id;
            IconPath = iconPath;
            Order = order;
            Offset = offset;
            OnClick = onClick;
            VisibleWhen = visibleWhen;
            IsOpenWhen = isOpenWhen;
            CountProvider = countProvider;
        }

        /// <summary>
        ///     <para xml:lang="en">ID of the owning mod.</para>
        ///     <para xml:lang="zh-CN">所属模组的 ID。</para>
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     <para xml:lang="en">Trimmed global ID, for example <c>MYMOD_TOPBARBUTTON_RECIPES</c>.</para>
        ///     <para xml:lang="zh-CN">去除首尾空白后的全局 ID，例如 <c>MYMOD_TOPBARBUTTON_RECIPES</c>。</para>
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Godot resource path for the icon, or <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN">图标的 Godot 资源路径；未指定时为 <see langword="null" />。</para>
        /// </summary>
        public string? IconPath { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Global sort order among registered action buttons. Lower values appear closer to the
        ///         vanilla deck button.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         已注册操作按钮之间的全局排序值；值越小越靠近原版牌组按钮。
        ///     </para>
        /// </summary>
        public int Order { get; }

        /// <summary>
        ///     <para xml:lang="en">Additional visual offset from the automatically arranged slot.</para>
        ///     <para xml:lang="zh-CN">相对于自动排列槽位的额外视觉偏移。</para>
        /// </summary>
        public Vector2 Offset { get; }

        /// <summary>
        ///     <para xml:lang="en">Click handler. See <see cref="ModTopBarButtonSpec.OnClick" />.</para>
        ///     <para xml:lang="zh-CN">点击回调。参见 <see cref="ModTopBarButtonSpec.OnClick" />。</para>
        /// </summary>
        public Action<ModTopBarButtonContext> OnClick { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional visibility predicate. See <see cref="ModTopBarButtonSpec.VisibleWhen" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的可见性谓词。参见 <see cref="ModTopBarButtonSpec.VisibleWhen" />。
        ///     </para>
        /// </summary>
        public Func<ModTopBarButtonContext, bool>? VisibleWhen { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional predicate for the selected/open visual state. See
        ///         <see cref="ModTopBarButtonSpec.IsOpenWhen" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         控制选中或打开状态视觉效果的可选谓词。参见
        ///         <see cref="ModTopBarButtonSpec.IsOpenWhen" />。
        ///     </para>
        /// </summary>
        public Func<ModTopBarButtonContext, bool>? IsOpenWhen { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional count provider for the label. See <see cref="ModTopBarButtonSpec.CountProvider" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         计数标签的可选数量提供器。参见 <see cref="ModTopBarButtonSpec.CountProvider" />。
        ///     </para>
        /// </summary>
        public Func<ModTopBarButtonContext, int>? CountProvider { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Hover-tip title read from <c>static_hover_tips</c> under key <c>{Id}.title</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从 <c>static_hover_tips</c> 的 <c>{Id}.title</c> 键读取的悬停提示标题。
        ///     </para>
        /// </summary>
        public LocString Title => new(ModTopBarButtonSpec.HoverTipLocTable, $"{Id}.title");

        /// <summary>
        ///     <para xml:lang="en">
        ///         Hover-tip description read from <c>static_hover_tips</c> under key
        ///         <c>{Id}.description</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从 <c>static_hover_tips</c> 的 <c>{Id}.description</c> 键读取的悬停提示描述。
        ///     </para>
        /// </summary>
        public LocString Description => new(ModTopBarButtonSpec.HoverTipLocTable, $"{Id}.description");
    }
}
