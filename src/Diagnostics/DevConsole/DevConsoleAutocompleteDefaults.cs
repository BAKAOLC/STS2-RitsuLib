namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers built-in autocomplete bindings aligned with the game's command argument positions.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         注册与游戏命令参数位置一致的内置自动补全绑定。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Commands whose completion values are not model IDs retain their original completion behavior. This
    ///         includes content-type names, room types, numeric indices, fixed option lists, and subcommand selectors.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         补全值不是模型 ID 的命令会保留原有补全行为，包括内容类型名称、房间类型、数字索引、固定选项列表和
    ///         子命令选择项。
    ///     </para>
    /// </remarks>
    internal static class DevConsoleAutocompleteDefaults
    {
        public static void Register()
        {
            RegisterModelEntryIdFirstArgument(
                "power",
                "afflict",
                "ancient",
                "card",
                "enchant",
                "event",
                "fight",
                "potion",
                "remove_card");

            RegisterPileNameSecondArgument("card", "remove_card");

            DevConsoleAutocompleteRegistry.Register(
                "ancient",
                DevConsoleAutocompleteEnhancements.AncientChoice,
                DevConsoleAutocompleteContextPredicates.IsAncientChoiceArgument);

            DevConsoleAutocompleteRegistry.Register(
                "relic",
                DevConsoleAutocompleteEnhancements.RitsuLibModEntryId,
                DevConsoleAutocompleteContextPredicates.IsRelicIdArgument);

            DevConsoleAutocompleteRegistry.Register(
                "unlock",
                DevConsoleAutocompleteEnhancements.RitsuLibModEntryId,
                DevConsoleAutocompleteContextPredicates.IsUnlockDiscoveryIdArgument);
        }

        private static void RegisterModelEntryIdFirstArgument(params string[] commandNames)
        {
            foreach (var commandName in commandNames)
                DevConsoleAutocompleteRegistry.Register(
                    commandName,
                    DevConsoleAutocompleteEnhancements.RitsuLibModEntryId,
                    DevConsoleAutocompleteContextPredicates.IsFirstArgument);
        }

        private static void RegisterPileNameSecondArgument(params string[] commandNames)
        {
            foreach (var commandName in commandNames)
                DevConsoleAutocompleteRegistry.Register(
                    commandName,
                    DevConsoleAutocompleteEnhancements.PileName,
                    DevConsoleAutocompleteContextPredicates.IsSecondArgument);
        }
    }
}
