namespace STS2RitsuLib.CardPiles.Nodes
{
    /// <summary>
    ///     Process-wide lookup from <see cref="ModCardPileDefinition" /> to the currently live UI instance
    ///     (button or extra-hand container). Used by
    ///     <see cref="ModCardPileLayout.GetTargetPosition" /> and by the <c>NCard.FindOnTable</c> patch to
    ///     answer "where did the card fly to?" without forcing callers to look up scene nodes themselves.
    ///     从 <see cref="ModCardPileDefinition" /> 到当前 live UI 实例（button 或 extra-hand container）的进程级查找表。
    ///     <see cref="ModCardPileLayout.GetTargetPosition" /> 和 <c>NCard.FindOnTable</c> patch 使用它回答
    ///     “卡牌飞到了哪里？”，无需强迫调用方自行查找 scene node。
    /// </summary>
    /// <remarks>
    ///     The registry only holds weak UI state (Godot nodes clean themselves up on scene unload); entries
    ///     are replaced when a new combat UI reinjects piles.
    ///     registry 只持有弱 UI 状态（Godot node 会在 scene unload 时自行清理）；新的 combat UI 重新注入牌堆时，
    ///     entry 会被替换。
    /// </remarks>
    internal static class ModCardPileButtonRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<string, NModCardPileButton> Buttons = [];
        private static readonly Dictionary<string, NModExtraHand> ExtraHands = [];

        internal static void RegisterButton(ModCardPileDefinition definition, NModCardPileButton button)
        {
            lock (SyncRoot)
            {
                Buttons[definition.Id] = button;
            }
        }

        internal static void UnregisterButton(ModCardPileDefinition definition, NModCardPileButton button)
        {
            lock (SyncRoot)
            {
                if (Buttons.TryGetValue(definition.Id, out var existing) && ReferenceEquals(existing, button))
                    Buttons.Remove(definition.Id);
            }
        }

        internal static NModCardPileButton? TryGetButton(ModCardPileDefinition definition)
        {
            lock (SyncRoot)
            {
                return Buttons.GetValueOrDefault(definition.Id);
            }
        }

        internal static void RegisterExtraHand(ModCardPileDefinition definition, NModExtraHand hand)
        {
            lock (SyncRoot)
            {
                ExtraHands[definition.Id] = hand;
            }
        }

        internal static void UnregisterExtraHand(ModCardPileDefinition definition, NModExtraHand hand)
        {
            lock (SyncRoot)
            {
                if (ExtraHands.TryGetValue(definition.Id, out var existing) && ReferenceEquals(existing, hand))
                    ExtraHands.Remove(definition.Id);
            }
        }

        internal static NModExtraHand? TryGetExtraHand(ModCardPileDefinition definition)
        {
            lock (SyncRoot)
            {
                return ExtraHands.GetValueOrDefault(definition.Id);
            }
        }
    }
}
