using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content
{
    public abstract class ModAncientEventTemplate : AncientEventModel
    {
        protected string ModOptionKey(string pageName, string optionName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pageName);
            ArgumentException.ThrowIfNullOrWhiteSpace(optionName);
            return $"{Id.Entry}.pages.{pageName}.options.{optionName}";
        }

        protected new string InitialOptionKey(string optionName)
        {
            return ModOptionKey("INITIAL", optionName);
        }

        protected EventOption CreateModRelicOption<T>(string pageName = "INITIAL") where T : RelicModel
        {
            return CreateModRelicOption(ModelDb.Relic<T>().ToMutable(), pageName);
        }

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
