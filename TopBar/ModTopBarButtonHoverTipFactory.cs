using Godot;
using MegaCrit.Sts2.Core.HoverTips;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     Produces a vanilla <see cref="HoverTip" /> for a <see cref="ModTopBarButtonDefinition" /> from
    ///     its icon path + localized title / description. Mirrors <c>ModCardPileHoverTipFactory</c>.
    ///     基于图标路径 + 本地化标题/描述，为 <see cref="ModTopBarButtonDefinition" /> 生成原版 <see cref="HoverTip" />。
    ///     与 <c>ModCardPileHoverTipFactory</c> 保持一致。
    /// </summary>
    public static class ModTopBarButtonHoverTipFactory
    {
        /// <summary>
        ///     Builds a <see cref="HoverTip" /> combining the <see cref="ModTopBarButtonDefinition.Title" />
        ///     / <see cref="ModTopBarButtonDefinition.Description" /> loc strings with the icon texture at
        ///     <see cref="ModTopBarButtonDefinition.IconPath" />. Falls back to a text-only hover tip when
        ///     the icon path is empty or points at a missing resource.
        ///     构建一个 <see cref="HoverTip" />，将 <see cref="ModTopBarButtonDefinition.Title" />
        ///     / <see cref="ModTopBarButtonDefinition.Description" /> 本地化字符串与
        ///     <see cref="ModTopBarButtonDefinition.IconPath" /> 处的图标纹理组合起来。图标路径为空或指向缺失资源时，
        ///     回退为纯文本悬停提示。
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
