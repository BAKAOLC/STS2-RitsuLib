using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Ancients.Options
{
    /// <summary>
    ///     <para xml:lang="en">Describes additional choices for an Ancient's initial option list.</para>
    ///     <para xml:lang="zh-CN">描述要添加到先古之民初始选项列表中的额外选项。</para>
    /// </summary>
    public sealed class ModAncientOptionRule
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a rule with the specified option factory.</para>
        ///     <para xml:lang="zh-CN">使用指定的选项工厂创建规则。</para>
        /// </summary>
        /// <param name="optionFactory">
        ///     <para xml:lang="en">Produces zero or more options for the current Ancient instance.</para>
        ///     <para xml:lang="zh-CN">为当前先古之民实例生成零个或多个选项。</para>
        /// </param>
        public ModAncientOptionRule(Func<AncientEventModel, IEnumerable<EventOption>> optionFactory)
        {
            ArgumentNullException.ThrowIfNull(optionFactory);
            OptionFactory = optionFactory;
        }

        /// <summary>
        ///     <para xml:lang="en">Produces options to append for a matching Ancient instance.</para>
        ///     <para xml:lang="zh-CN">为匹配的先古之民实例生成要追加的选项。</para>
        /// </summary>
        public Func<AncientEventModel, IEnumerable<EventOption>> OptionFactory { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional condition evaluated before the option factory. A <see langword="null" /> condition
        ///         always passes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         调用选项工厂前评估的可选条件；为 <see langword="null" /> 时始终通过。
        ///     </para>
        /// </summary>
        public Func<AncientEventModel, bool>? Condition { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Higher-priority rules run first; ties preserve registration order.</para>
        ///     <para xml:lang="zh-CN">优先级较高的规则先运行；优先级相同时保留注册顺序。</para>
        /// </summary>
        public int Priority { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Whether to skip an option whose non-empty <see cref="EventOption.TextKey" /> has already appeared.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         是否跳过非空 <see cref="EventOption.TextKey" /> 已出现过的选项。
        ///     </para>
        /// </summary>
        public bool SkipDuplicateTextKeys { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Creates a rule whose factory returns at most one option.</para>
        ///     <para xml:lang="zh-CN">创建选项工厂至多返回一个选项的规则。</para>
        /// </summary>
        public static ModAncientOptionRule Single(
            Func<AncientEventModel, EventOption?> optionFactory,
            Func<AncientEventModel, bool>? condition = null,
            int priority = 0,
            bool skipDuplicateTextKeys = true)
        {
            ArgumentNullException.ThrowIfNull(optionFactory);

            return new(ancient =>
            {
                var option = optionFactory(ancient);
                return option == null ? [] : [option];
            })
            {
                Condition = condition,
                Priority = priority,
                SkipDuplicateTextKeys = skipDuplicateTextKeys,
            };
        }
    }
}
