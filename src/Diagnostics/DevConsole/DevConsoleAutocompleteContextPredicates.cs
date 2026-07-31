namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">Provides reusable predicates for autocomplete binding registration.</para>
    ///     <para xml:lang="zh-CN">提供自动补全绑定注册所需的可复用谓词。</para>
    /// </summary>
    public static class DevConsoleAutocompleteContextPredicates
    {
        /// <summary>
        ///     <para xml:lang="en">Matches the first argument when no preceding arguments are present.</para>
        ///     <para xml:lang="zh-CN">匹配不存在前置参数时的第一个参数。</para>
        /// </summary>
        public static bool IsFirstArgument(DevConsoleAutocompleteContext context)
        {
            return context is { ArgumentIndex: 0, CompletedArgs.Count: 0 };
        }

        /// <summary>
        ///     <para xml:lang="en">Matches the second argument when exactly one preceding argument is present.</para>
        ///     <para xml:lang="zh-CN">匹配恰有一个前置参数时的第二个参数。</para>
        /// </summary>
        public static bool IsSecondArgument(DevConsoleAutocompleteContext context)
        {
            return context is { ArgumentIndex: 1, CompletedArgs.Count: 1 };
        }

        /// <summary>
        ///     <para xml:lang="en">Matches an <c>ancient</c> option token following the ancient-event ID.</para>
        ///     <para xml:lang="zh-CN">匹配 <c>ancient</c> 命令中先古之民事件 ID 之后的选项标记。</para>
        /// </summary>
        public static bool IsAncientChoiceArgument(DevConsoleAutocompleteContext context)
        {
            if (!context.CommandName.Equals("ancient", StringComparison.OrdinalIgnoreCase))
                return false;

            return context is { ArgumentIndex: 1, CompletedArgs.Count: 1 };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Matches a relic ID used alone or after <c>add</c> or <c>remove</c> in the <c>relic</c> command.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         匹配 <c>relic</c> 命令中单独使用或位于 <c>add</c>、<c>remove</c> 之后的遗物 ID。
        ///     </para>
        /// </summary>
        public static bool IsRelicIdArgument(DevConsoleAutocompleteContext context)
        {
            if (IsFirstArgument(context))
                return true;

            if (context.ArgumentIndex != 1 || context.CompletedArgs.Count != 1)
                return false;

            var subcommand = context.CompletedArgs[0];
            return subcommand.Equals("add", StringComparison.OrdinalIgnoreCase) ||
                   subcommand.Equals("remove", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     <para xml:lang="en">Matches trailing <c>unlock</c> arguments that identify discovery entries.</para>
        ///     <para xml:lang="zh-CN">匹配用于标识解锁条目的 <c>unlock</c> 尾随参数。</para>
        /// </summary>
        public static bool IsUnlockDiscoveryIdArgument(DevConsoleAutocompleteContext context)
        {
            if (context.ArgumentIndex < 1 || context.CompletedArgs.Count < 1)
                return false;

            return DevConsoleUnlockAutocompleteSources.SupportsDiscoveryIds(context.CompletedArgs[0]);
        }
    }
}
