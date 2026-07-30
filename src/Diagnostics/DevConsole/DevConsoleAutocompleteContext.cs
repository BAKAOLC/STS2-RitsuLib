using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes a developer-console <see cref="AbstractConsoleCmd.CompleteArgument" /> invocation.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述开发者控制台对 <see cref="AbstractConsoleCmd.CompleteArgument" /> 的调用。
    ///     </para>
    /// </summary>
    public sealed class DevConsoleAutocompleteContext
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a context for an autocomplete request.</para>
        ///     <para xml:lang="zh-CN">创建自动补全请求的上下文。</para>
        /// </summary>
        public DevConsoleAutocompleteContext(
            AbstractConsoleCmd command,
            string[] completedArgs,
            int argumentIndex)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            ArgumentNullException.ThrowIfNull(completedArgs);
            ArgumentOutOfRangeException.ThrowIfNegative(argumentIndex);
            CompletedArgs = [.. completedArgs];
            ArgumentIndex = argumentIndex;
        }

        /// <summary>
        ///     <para xml:lang="en">The console command producing completions.</para>
        ///     <para xml:lang="zh-CN">生成补全结果的控制台命令。</para>
        /// </summary>
        public AbstractConsoleCmd Command { get; }

        /// <summary>
        ///     <para xml:lang="en">The arguments preceding the token being completed.</para>
        ///     <para xml:lang="zh-CN">待补全标记之前已有的参数。</para>
        /// </summary>
        public IReadOnlyList<string> CompletedArgs { get; }

        /// <summary>
        ///     <para xml:lang="en">The zero-based index of the argument being completed.</para>
        ///     <para xml:lang="zh-CN">待补全参数的从零开始索引。</para>
        /// </summary>
        public int ArgumentIndex { get; }

        /// <summary>
        ///     <para xml:lang="en">The developer-console command name.</para>
        ///     <para xml:lang="zh-CN">开发者控制台命令名称。</para>
        /// </summary>
        public string CommandName => Command.CmdName;
    }
}
