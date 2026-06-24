using Godot;
using MegaCrit.Sts2.Core.HoverTips;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Builds a vanilla <see cref="HoverTip" /> for a <see cref="ModCardPileDefinition" /> by combining its
    ///     localized title / description (resolved against <see cref="ModCardPileSpec.HoverTipLocTable" />)
    ///     with the icon texture loaded from <c>IconPath</c>. Mirrors <c>ModKeywordRegistry.CreateHoverTip</c>
    ///     so the same hover UX is available for piles.
    ///     为 <see cref="ModCardPileDefinition" /> 构建原版 <see cref="HoverTip" />：组合其本地化 title /
    ///     description（基于 <see cref="ModCardPileSpec.HoverTipLocTable" /> 解析）和从 <c>IconPath</c>
    ///     加载的图标 texture。它对应 <c>ModKeywordRegistry.CreateHoverTip</c>，让牌堆也具备相同 hover UX。
    /// </summary>
    public static class ModCardPileHoverTipFactory
    {
        /// <summary>
        ///     Produces a <see cref="HoverTip" /> for <paramref name="definition" />. Title and description
        ///     come from <see cref="ModCardPileDefinition.Title" /> / <see cref="ModCardPileDefinition.Description" />
        ///     (keys use <see cref="ModCardPileDefinition.Id" /> as stem), and the icon is loaded
        ///     from <c>ResourceLoader</c> when <see cref="ModCardPileDefinition.IconPath" /> exists.
        ///     <see cref="ModCardPileDefinition.Title" /> / <see cref="ModCardPileDefinition.Description" />
        ///     为 <paramref name="definition" /> 生成 <see cref="HoverTip" />。标题和描述
        ///     来自 <see cref="ModCardPileDefinition.Title" /> / <see cref="ModCardPileDefinition.Description" />
        ///     （key 使用 <see cref="ModCardPileDefinition.Id" /> 作为 stem），并且当
        ///     <see cref="ModCardPileDefinition.IconPath" /> 存在时，图标从 <c>ResourceLoader</c> 加载。
        ///     <see cref="ModCardPileDefinition.Title" />
        ///     <see cref="ModCardPileDefinition.Description" />
        /// </summary>
        public static HoverTip Create(ModCardPileDefinition definition)
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
