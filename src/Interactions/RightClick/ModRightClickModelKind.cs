namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     Model families supported by the built-in right-click node patches.
    ///     内置右键节点 patch 支持的模型类别。
    /// </summary>
    public enum ModRightClickModelKind
    {
        /// <summary>
        ///     Card in a supported combat UI surface.
        ///     受支持战斗界面中的卡牌。
        /// </summary>
        Card = 0,

        /// <summary>
        ///     Relic.
        ///     遗物。
        /// </summary>
        Relic = 1,

        /// <summary>
        ///     Power.
        ///     能力。
        /// </summary>
        Power = 2,

        /// <summary>
        ///     Potion.
        ///     药水。
        /// </summary>
        Potion = 3,

        /// <summary>
        ///     Orb in the local player's active orb queue.
        ///     本地玩家当前充能球队列中的充能球。
        /// </summary>
        Orb = 4,
    }
}
