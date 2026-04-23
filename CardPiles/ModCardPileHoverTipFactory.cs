using Godot;
using MegaCrit.Sts2.Core.HoverTips;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Builds a vanilla <see cref="HoverTip" /> for a <see cref="ModCardPileDefinition" /> by combining its
    ///     localized title / description (resolved against <see cref="ModCardPileSpec.HoverTipLocTable" />)
    ///     with the icon texture loaded from <c>IconPath</c>. Mirrors <c>ModKeywordRegistry.CreateHoverTip</c>
    ///     so the same hover UX is available for piles.
    /// </summary>
    public static class ModCardPileHoverTipFactory
    {
        /// <summary>
        ///     Produces a <see cref="HoverTip" /> for <paramref name="definition" />. Title and description
        ///     come from <see cref="ModCardPileDefinition.Title" /> / <see cref="ModCardPileDefinition.Description" />
        ///     (both derived from <see cref="ModCardPileDefinition.LocStem" />), and the icon is loaded
        ///     from <c>ResourceLoader</c> when <see cref="ModCardPileDefinition.IconPath" /> exists.
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
