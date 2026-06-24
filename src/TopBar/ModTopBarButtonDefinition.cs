using System.Numerics;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     Immutable registry entry for a mod-owned top-bar button.
    ///     mod 拥有的顶部栏按钮的不可变注册表条目。
    /// </summary>
    public sealed record ModTopBarButtonDefinition
    {
        internal ModTopBarButtonDefinition(
            string modId,
            string id,
            string? iconPath,
            int order,
            Vector2 offset,
            Action<ModTopBarButtonContext>? onClick,
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
        ///     Owning mod id.
        ///     所属 mod id。
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     Normalized global id (e.g. <c>MYMOD_TOPBARBUTTON_RECIPES</c>).
        ///     规范化全局 id (e.g. <c>MYMOD_TOPBARBUTTON_RECIPES</c>)。
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Godot resource path for the icon, or null.
        ///     图标的 Godot 资源路径, 或 null。
        /// </summary>
        public string? IconPath { get; }

        /// <summary>
        ///     Sort order within this mod's top-bar buttons.
        ///     排序顺序 在此 mod 的顶部栏按钮内。
        /// </summary>
        public int Order { get; }

        /// <summary>
        ///     Extra pixel offset on top of the auto-stacked slot.
        ///     叠加在自动堆叠槽上的额外像素偏移。
        /// </summary>
        public Vector2 Offset { get; }

        /// <summary>
        ///     Click handler; see <see cref="ModTopBarButtonSpec.OnClick" />.
        ///     点击处理器; 见 <see cref="ModTopBarButtonSpec.OnClick" />。
        /// </summary>
        public Action<ModTopBarButtonContext>? OnClick { get; }

        /// <summary>
        ///     Optional visibility predicate; see <see cref="ModTopBarButtonSpec.VisibleWhen" />.
        ///     可选可见性谓词; 见 <see cref="ModTopBarButtonSpec.VisibleWhen" />。
        /// </summary>
        public Func<ModTopBarButtonContext, bool>? VisibleWhen { get; }

        /// <summary>
        ///     Optional "screen open" predicate; see <see cref="ModTopBarButtonSpec.IsOpenWhen" />.
        ///     可选“屏幕打开”谓词; 见 <see cref="ModTopBarButtonSpec.IsOpenWhen" />。
        /// </summary>
        public Func<ModTopBarButtonContext, bool>? IsOpenWhen { get; }

        /// <summary>
        ///     Optional count provider for the badge; see <see cref="ModTopBarButtonSpec.CountProvider" />.
        ///     徽章的可选计数提供器；见 <see cref="ModTopBarButtonSpec.CountProvider" />。
        /// </summary>
        public Func<ModTopBarButtonContext, int>? CountProvider { get; }

        /// <summary>
        ///     Hover-tip title resolved against <c>static_hover_tips</c> with key <c>{Id}.title</c>.
        ///     悬停提示标题 基于解析 <c>static_hover_tips</c> 与 键 <c>{Id}.title</c>。
        /// </summary>
        public LocString Title => new(ModTopBarButtonSpec.HoverTipLocTable, $"{Id}.title");

        /// <summary>
        ///     Hover-tip description resolved against <c>static_hover_tips</c> with key <c>{Id}.description</c>.
        ///     根据键 <c>{Id}.description</c> 从 <c>static_hover_tips</c> 解析的悬停提示描述。
        /// </summary>
        public LocString Description => new(ModTopBarButtonSpec.HoverTipLocTable, $"{Id}.description");
    }
}
