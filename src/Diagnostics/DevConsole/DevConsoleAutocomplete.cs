using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the public API for registering and applying developer-console autocomplete enhancements.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供注册和应用开发者控制台自动补全增强功能的公共 API。
    ///     </para>
    /// </summary>
    public static class DevConsoleAutocomplete
    {
        /// <summary>
        ///     <para xml:lang="en">Registers a command-argument autocomplete binding.</para>
        ///     <para xml:lang="zh-CN">注册命令参数的自动补全绑定。</para>
        /// </summary>
        public static void Register(DevConsoleAutocompleteBinding binding)
        {
            DevConsoleAutocompleteRegistry.Register(binding);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers enhancements for a specific argument index of a command.</para>
        ///     <para xml:lang="zh-CN">为命令的指定参数索引注册增强功能。</para>
        /// </summary>
        public static void Register(
            string commandName,
            int argumentIndex,
            DevConsoleAutocompleteEnhancements enhancements,
            Func<DevConsoleAutocompleteContext, bool>? appliesWhen = null)
        {
            DevConsoleAutocompleteRegistry.Register(commandName, argumentIndex, enhancements, appliesWhen);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers enhancements selected by <paramref name="appliesWhen" />.</para>
        ///     <para xml:lang="zh-CN">注册由 <paramref name="appliesWhen" /> 选择的增强功能。</para>
        /// </summary>
        public static void Register(
            string commandName,
            DevConsoleAutocompleteEnhancements enhancements,
            Func<DevConsoleAutocompleteContext, bool> appliesWhen)
        {
            DevConsoleAutocompleteRegistry.Register(commandName, enhancements, appliesWhen);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves the enhancements applicable to a completion request.</para>
        ///     <para xml:lang="zh-CN">解析适用于补全请求的增强功能。</para>
        /// </summary>
        public static DevConsoleAutocompleteEnhancements Resolve(
            AbstractConsoleCmd command,
            string[] completedArgs,
            int argumentIndex)
        {
            return DevConsoleAutocompleteRegistry.Resolve(command, completedArgs, argumentIndex);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a match-predicate chain for mod commands that call <c>CompleteArgument</c> directly.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为直接调用 <c>CompleteArgument</c> 的模组命令构建匹配谓词链。
        ///     </para>
        /// </summary>
        public static Func<string, string, bool>? BuildMatchPredicate(
            AbstractConsoleCmd command,
            string[] completedArgs,
            Func<string, string, bool>? inner = null)
        {
            return DevConsoleAutocompleteEnhancer.BuildMatchPredicate(
                Resolve(command, completedArgs, completedArgs.Length),
                inner,
                completedArgs);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies result enhancements for a mod command that calls <c>CompleteArgument</c> directly.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为直接调用 <c>CompleteArgument</c> 的模组命令应用结果增强功能。
        ///     </para>
        /// </summary>
        public static void ApplyToResult(
            AbstractConsoleCmd command,
            string[] completedArgs,
            ref CompletionResult result)
        {
            DevConsoleAutocompleteEnhancer.ApplyToResult(
                ref result,
                Resolve(command, completedArgs, result.ArgumentIndex),
                completedArgs);
        }
    }
}
