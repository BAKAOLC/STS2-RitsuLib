namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">Specifies the lifetime and storage owner of a mod card pile.</para>
    ///     <para xml:lang="zh-CN">指定模组卡牌牌堆的生命周期与存储主体。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         <see cref="CombatOnly" /> piles are attached to <c>PlayerCombatState</c>, participate in
    ///         <c>PlayerCombatState.AllPiles</c> and <c>IsCombatPile</c>, and are discarded with that combat.
    ///     </para>
    ///     <para xml:lang="en">
    ///         <see cref="RunPersistent" /> piles live on <c>Player</c> and persist across combats (much like
    ///         <c>Player.Deck</c>). They participate in <c>Player.Piles</c> after they have been resolved and
    ///         are serialized through RitsuLib run-saved data.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="CombatOnly" /> 牌堆附加到 <c>PlayerCombatState</c>，参与
    ///         <c>PlayerCombatState.AllPiles</c> 与 <c>IsCombatPile</c>，并随该场战斗一同丢弃。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="RunPersistent" /> 牌堆附加到 <c>Player</c>，并像 <c>Player.Deck</c> 一样
    ///         跨战斗保留。解析后它们会参与 <c>Player.Piles</c>，并通过 RitsuLib
    ///         跑局保存数据序列化。
    ///     </para>
    /// </remarks>
    public enum ModCardPileScope
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates the pile lazily for each <c>PlayerCombatState</c> and discards it when combat ends.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为每个 <c>PlayerCombatState</c> 延迟创建牌堆，并在战斗结束时丢弃。
        ///     </para>
        /// </summary>
        CombatOnly = 0,

        /// <summary>
        ///     <para xml:lang="en">Attaches the pile to a <c>Player</c> and saves it with the run.</para>
        ///     <para xml:lang="zh-CN">将牌堆附加到 <c>Player</c>，并随跑局保存。</para>
        /// </summary>
        RunPersistent = 1,
    }
}
