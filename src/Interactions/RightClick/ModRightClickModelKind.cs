namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     <para xml:lang="en">Lists the model families supported by the built-in right-click node patches.</para>
    ///     <para xml:lang="zh-CN">列出内置右键节点补丁支持的模型类别。</para>
    /// </summary>
    public enum ModRightClickModelKind
    {
        /// <summary>
        ///     <para xml:lang="en">A card in a supported combat UI.</para>
        ///     <para xml:lang="zh-CN">受支持战斗界面中的卡牌。</para>
        /// </summary>
        Card = 0,

        /// <summary>
        ///     <para xml:lang="en">A relic.</para>
        ///     <para xml:lang="zh-CN">遗物。</para>
        /// </summary>
        Relic = 1,

        /// <summary>
        ///     <para xml:lang="en">A power.</para>
        ///     <para xml:lang="zh-CN">能力。</para>
        /// </summary>
        Power = 2,

        /// <summary>
        ///     <para xml:lang="en">A potion.</para>
        ///     <para xml:lang="zh-CN">药水。</para>
        /// </summary>
        Potion = 3,

        /// <summary>
        ///     <para xml:lang="en">An orb in the local player's active orb queue.</para>
        ///     <para xml:lang="zh-CN">本地玩家当前充能球队列中的充能球。</para>
        /// </summary>
        Orb = 4,
    }
}
