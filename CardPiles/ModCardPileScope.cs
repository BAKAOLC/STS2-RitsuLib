namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Lifetime scope of a custom card pile.
    ///     自定义 card pile 的生命周期 scope。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="CombatOnly" /> piles live on <c>PlayerCombatState</c> and are automatically disposed with
    ///         the combat; they participate in <c>PlayerCombatState.AllPiles</c> and <c>IsCombatPile</c>.
    ///         <c>CombatOnly</c> pile 存在于 <c>PlayerCombatState</c> 上，并随战斗自动释放；
    ///         它们参与 <c>PlayerCombatState.AllPiles</c> 和 <c>IsCombatPile</c>。
    ///     </para>
    ///     <para>
    ///         <see cref="RunPersistent" /> piles live on <c>Player</c> and persist across combats (much like
    ///         <c>Player.Deck</c>). They participate in <c>Player.Piles</c> after they have been resolved, but
    ///         persistence remains best-effort until explicit serialization support is added.
    ///         <c>RunPersistent</c> pile 存在于 <c>Player</c> 上并跨战斗保留（很像 <c>Player.Deck</c>）。
    ///         它们在解析后会参与 <c>Player.Piles</c>；在显式序列化支持加入前，
    ///         persistence 仍视为 best-effort。
    ///     </para>
    /// </remarks>
    public enum ModCardPileScope
    {
        /// <summary>
        ///     Created lazily per <c>PlayerCombatState</c> and discarded when combat ends.
        ///     按 <c>PlayerCombatState</c> 懒创建，并在战斗结束时丢弃。
        /// </summary>
        CombatOnly = 0,

        /// <summary>
        ///     Attached to a <c>Player</c> for the duration of a run. Currently stored in memory only.
        ///     在一次 run 期间附着到 <c>Player</c>。目前仅存储在内存中。
        /// </summary>
        RunPersistent = 1,
    }
}
