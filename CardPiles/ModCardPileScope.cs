namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Lifetime scope of a custom card pile.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="CombatOnly" /> piles live on <c>PlayerCombatState</c> and are automatically disposed with
    ///         the combat; they participate in <c>PlayerCombatState.AllPiles</c> and <c>IsCombatPile</c>.
    ///     </para>
    ///     <para>
    ///         <see cref="RunPersistent" /> piles live on <c>Player</c> and persist across combats (much like
    ///         <c>Player.Deck</c>). The first release stores them identically to combat piles but does not yet
    ///         participate in <c>AllPiles</c>; treat persistence as best-effort until explicit serialization
    ///         support is added.
    ///     </para>
    /// </remarks>
    public enum ModCardPileScope
    {
        /// <summary>
        ///     Created lazily per <c>PlayerCombatState</c> and discarded when combat ends.
        /// </summary>
        CombatOnly = 0,

        /// <summary>
        ///     Attached to a <c>Player</c> for the duration of a run. Currently stored in memory only.
        /// </summary>
        RunPersistent = 1,
    }
}
