using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="AncientEventModel" /> with helpers for option keys and relic rewards that complete the ancient
    ///     flow.
    /// </summary>
    public abstract class ModAncientEventTemplate : AncientEventModel
    {
        /// <summary>
        ///     Builds a namespaced option key for <paramref name="pageName" /> / <paramref name="optionName" /> under this ancient
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
        ///     Creates a relic option that obtains the relic for the event owner and calls <see cref="AncientEventModel.Done" />.
        /// </summary>
        protected EventOption CreateModRelicOption<T>(string pageName = "INITIAL") where T : RelicModel
        {
            return CreateModRelicOption(ModelDb.Relic<T>().ToMutable(), pageName);
        }

        /// <summary>
        ///     Creates a relic option that obtains <paramref name="relic" /> for the owner and completes the ancient.
        /// </summary>
        protected EventOption CreateModRelicOption(RelicModel relic, string pageName = "INITIAL")
        {
            var owner = Owner ?? throw new InvalidOperationException(
                $"Ancient '{Id.Entry}' tried to create a relic option before the event owner was assigned.");

            return CreateModRelicOption(
                relic,
                async () =>
                {
                    await RelicCmd.Obtain(relic, owner);
                    Done();
                },
                pageName);
        }

        /// <summary>
        ///     Creates a relic option with an explicit post-pick handler and localization key.
        /// </summary>
        protected EventOption CreateModRelicOption(
            RelicModel relic,
            Func<Task>? onChosen,
            string pageName = "INITIAL")
        {
            relic.AssertMutable();
            relic.Owner = Owner ?? throw new InvalidOperationException(
                $"Ancient '{Id.Entry}' tried to create a relic option before the event owner was assigned.");
            return EventOption.FromRelic(relic, this, onChosen, ModOptionKey(pageName, relic.Id.Entry));
        }
    }
}
