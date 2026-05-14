namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Lifetime scope of a custom card pile.
    ///     自定义卡牌牌堆的生命周期作用域。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="CombatOnly" /> piles live on <c>PlayerCombatState</c> and are automatically disposed with
    ///         the combat; they participate in <c>PlayerCombatState.AllPiles</c> and <c>IsCombatPile</c>.
    ///     </para>
    ///     <para>
    ///         <see cref="RunPersistent" /> piles live on <c>Player</c> and persist across combats (much like
    ///         <c>Player.Deck</c>). They participate in <c>Player.Piles</c> after they have been resolved, but
    ///         persistence remains best-effort until explicit serialization support is added.
    ///     </para>
    ///     <para>
    ///         <see cref="CombatOnly" /> 牌堆存在于 <c>PlayerCombatState</c> 上，并随
    ///         战斗自动释放；它们参与 <c>PlayerCombatState.AllPiles</c> 和 <c>IsCombatPile</c>。
    ///     </para>
    ///     <para>
    ///         <see cref="RunPersistent" /> 牌堆存在于 <c>Player</c> 上，并跨战斗保留（很像
    ///         <c>Player.Deck</c>）。解析后它们会参与 <c>Player.Piles</c>，但
    ///         在加入显式序列化支持前，持久化仍是 best-effort。
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
        ///     在一次跑局期间附加到 <c>Player</c>。目前仅存储在内存中。
        /// </summary>
        RunPersistent = 1,
    }
}
