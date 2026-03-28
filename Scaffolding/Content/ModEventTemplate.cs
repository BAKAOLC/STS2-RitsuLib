using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="EventModel" /> with helpers for stable localization keys and relic options owned by the event.
    /// </summary>
    public abstract class ModEventTemplate : EventModel
    {
        /// <summary>
        ///     Builds a namespaced option key for <paramref name="pageName" /> / <paramref name="optionName" /> under this event
        ///     id.
        /// </summary>
        protected string ModOptionKey(string pageName, string optionName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pageName);
            ArgumentException.ThrowIfNullOrWhiteSpace(optionName);
            return $"{Id.Entry}.pages.{pageName}.options.{optionName}";
        }

        /// <summary>
        ///     Shortcut for <see cref="ModOptionKey" /> with the <c>INITIAL</c> page.
        /// </summary>
        protected new string InitialOptionKey(string optionName)
        {
            return ModOptionKey("INITIAL", optionName);
        }

        /// <summary>
        ///     Creates a relic-grant option for a mutable relic resolved from <typeparamref name="T" />.
        /// </summary>
        protected EventOption CreateModRelicOption<T>(Func<Task>? onChosen, string pageName = "INITIAL")
            where T : RelicModel
        {
            return CreateModRelicOption(ModelDb.Relic<T>().ToMutable(), onChosen, pageName);
        }

        /// <summary>
        ///     Creates a relic-grant option with a custom <paramref name="onChosen" /> callback and localization key.
        /// </summary>
        protected EventOption CreateModRelicOption(RelicModel relic, Func<Task>? onChosen, string pageName = "INITIAL")
        {
            relic.AssertMutable();
            relic.Owner = Owner ?? throw new InvalidOperationException(
                $"Event '{Id.Entry}' tried to create a relic option before the event owner was assigned.");
            return EventOption.FromRelic(relic, this, onChosen, ModOptionKey(pageName, relic.Id.Entry));
        }
    }
}
