using Godot;
using MegaCrit.Sts2.Core.HoverTips;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     <para xml:lang="en">Creates hover tips for registered mod top-bar buttons.</para>
    ///     <para xml:lang="zh-CN">为已注册的模组顶部栏按钮创建悬停提示。</para>
    /// </summary>
    public static class ModTopBarButtonHoverTipFactory
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a <see cref="HoverTip" /> from the definition's localized title, description, and
        ///         optional icon. An empty or unavailable icon path produces a text-only tip.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用定义中的本地化标题、描述和可选图标创建 <see cref="HoverTip" />。
        ///         图标路径为空或资源不可用时创建纯文本提示。
        ///     </para>
        /// </summary>
        public static HoverTip Create(ModTopBarButtonDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            Texture2D? icon = null;
            if (!string.IsNullOrWhiteSpace(definition.IconPath)
                && ResourceLoader.Exists(definition.IconPath))
                icon = ResourceLoader.Load<Texture2D>(definition.IconPath);

            return new(definition.Title, definition.Description, icon);
        }
    }
}
