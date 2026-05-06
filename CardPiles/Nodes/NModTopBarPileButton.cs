namespace STS2RitsuLib.CardPiles.Nodes
{
    /// <summary>
    ///     Thin specialization of <see cref="NModCardPileButton" /> for top-bar piles. Presently reuses the
    ///     base button unchanged; placement differences (size, margins) are handled by the top-bar injection
    ///     patch rather than by this class. The type still exists so style-specific behaviour can be added
    ///     later without breaking callers.
    /// </summary>
    public sealed partial class NModTopBarPileButton
    {
        /// <summary>
        ///     Builds a new top-bar button for <paramref name="definition" />. This currently produces the
        ///     same node as <see cref="NModCardPileButton.Create" />; a dedicated class simplifies identifying
        ///     top-bar instances in the scene tree.
        /// </summary>
        public static NModCardPileButton Create(ModCardPileDefinition definition)
        {
            return NModCardPileButton.Create(definition);
        }
    }
}
