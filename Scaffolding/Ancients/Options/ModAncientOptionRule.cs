using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Ancients.Options
{
    /// <summary>
    ///     Declarative rule for injecting extra options into an ancient's initial option pool.
    ///     用于向古代的初始选项池注入额外选项的声明式规则。
    /// </summary>
    public sealed class ModAncientOptionRule
    {
        /// <summary>
        ///     Creates a rule with an option factory.
        ///     创建带选项工厂的规则。
        /// </summary>
        /// <param name="optionFactory">
        ///     Produces zero or more options for the current ancient instance.
        ///     为当前古代实例生成零个或多个选项。
        /// </param>
        public ModAncientOptionRule(Func<AncientEventModel, IEnumerable<EventOption>> optionFactory)
        {
            ArgumentNullException.ThrowIfNull(optionFactory);
            OptionFactory = optionFactory;
        }

        /// <summary>
        ///     Produces options to append for a matching ancient instance.
        ///     为匹配的古代实例生成要追加的选项。
        /// </summary>
        public Func<AncientEventModel, IEnumerable<EventOption>> OptionFactory { get; }

        /// <summary>
        ///     Optional predicate gate. When null, the rule is always considered.
        ///     可选谓词门控。为 null 时，始终考虑该规则。
        /// </summary>
        public Func<AncientEventModel, bool>? Condition { get; init; }

        /// <summary>
        ///     Higher priority rules run first; ties preserve registration order.
        ///     优先级越高越先运行；相同优先级保留注册顺序。
        /// </summary>
        public int Priority { get; init; }

        /// <summary>
        ///     When true, options with duplicate <see cref="EventOption.TextKey" /> are skipped.
        ///     为 true 时，跳过具有重复 <see cref="EventOption.TextKey" /> 的选项。
        /// </summary>
        public bool SkipDuplicateTextKeys { get; init; } = true;

        /// <summary>
        ///     Convenience helper for a single optional option.
        ///     单个可选选项的便捷 helper。
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
