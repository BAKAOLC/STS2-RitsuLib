using Godot;
using MegaCrit.Sts2.Core.HoverTips;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     Produces a vanilla <see cref="HoverTip" /> for a <see cref="ModTopBarButtonDefinition" /> from
    ///     its icon path + localized title / description. Mirrors <c>ModCardPileHoverTipFactory</c>.
    /// </summary>
    public static class ModTopBarButtonHoverTipFactory
    {
        /// <summary>
        ///     Builds a <see cref="HoverTip" /> combining the <see cref="ModTopBarButtonDefinition.Title" />
        ///     / <see cref="ModTopBarButtonDefinition.Description" /> loc strings with the icon texture at
        ///     <see cref="ModTopBarButtonDefinition.IconPath" />. Falls back to a text-only hover tip when
        ///     the icon path is empty or points at a missing resource.
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
