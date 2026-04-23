namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Visual family of a mod card pile. Drives which UI chrome (top bar button, bottom-row combat button,
    ///     or extra hand) is created for the pile, and how <see cref="ModCardPileAnchor" /> is interpreted.
    /// </summary>
    public enum ModCardPileUiStyle
    {
        /// <summary>
        ///     No UI chrome. Cards fly to the coordinate declared by <c>Anchor.Custom(...)</c>; suitable for
        ///     purely invisible holding piles.
        /// </summary>
        Headless = 0,

        /// <summary>
        ///     Button in the top bar, next to the vanilla deck button (<c>NTopBarDeckButton</c>).
        /// </summary>
        TopBarDeck = 1,

        /// <summary>
        ///     Button on the bottom-left of the combat UI (next to the draw pile).
        /// </summary>
        BottomLeft = 2,

        /// <summary>
        ///     Button on the bottom-right of the combat UI (next to the exhaust pile).
        /// </summary>
        BottomRight = 3,

        /// <summary>
        ///     Extra hand-like container; cards inside are rendered as <c>NCard</c> nodes and can be previewed,
        ///     similar to the vanilla <c>NPlayerHand</c>.
        /// </summary>
        ExtraHand = 4,
    }
}
