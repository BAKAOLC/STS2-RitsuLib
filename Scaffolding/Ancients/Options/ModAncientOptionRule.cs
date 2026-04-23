using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Ancients.Options
{
    /// <summary>
    ///     Declarative rule for injecting extra options into an ancient's initial option pool.
    /// </summary>
    public sealed class ModAncientOptionRule
    {
        /// <summary>
        ///     Creates a rule with an option factory.
        /// </summary>
        /// <param name="optionFactory">
        ///     Produces zero or more options for the current ancient instance.
        /// </param>
        public ModAncientOptionRule(Func<AncientEventModel, IEnumerable<EventOption>> optionFactory)
        {
            ArgumentNullException.ThrowIfNull(optionFactory);
            OptionFactory = optionFactory;
        }

        /// <summary>
        ///     Produces options to append for a matching ancient instance.
        /// </summary>
        public Func<AncientEventModel, IEnumerable<EventOption>> OptionFactory { get; }

        /// <summary>
        ///     Optional predicate gate. When null, the rule is always considered.
        /// </summary>
        public Func<AncientEventModel, bool>? Condition { get; init; }

        /// <summary>
        ///     Higher priority rules run first; ties preserve registration order.
        /// </summary>
        public int Priority { get; init; }

        /// <summary>
        ///     When true, options with duplicate <see cref="EventOption.TextKey" /> are skipped.
        /// </summary>
        public bool SkipDuplicateTextKeys { get; init; } = true;

        /// <summary>
        ///     Convenience helper for a single optional option.
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
