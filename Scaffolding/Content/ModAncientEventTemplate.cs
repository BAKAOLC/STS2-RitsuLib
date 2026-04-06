using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Localization;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="AncientEventModel" /> with helpers for option keys, relic rewards that complete the ancient flow,
    ///     optional <see cref="IModAncientEventAssetOverrides" /> presentation paths, and dialogue loaded from the
    ///     <c>ancients</c> localization table (<see cref="AncientDialogueLocalization.BuildDialogueSetForModAncient" />).
    /// </summary>
    public abstract class ModAncientEventTemplate : AncientEventModel, IModAncientEventAssetOverrides
    {
        /// <inheritdoc />
        public virtual EventAssetProfile AssetProfile => EventAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomLayoutScenePath => AssetProfile.LayoutScenePath;

        /// <inheritdoc />
        public virtual string? CustomInitialPortraitPath => AssetProfile.InitialPortraitPath;

        /// <inheritdoc />
        public virtual string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;

        /// <inheritdoc />
        public virtual string? CustomVfxScenePath => AssetProfile.VfxScenePath;

        /// <inheritdoc />
        public virtual AncientEventPresentationAssetProfile AncientPresentationAssetProfile =>
            AncientEventPresentationAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomMapIconPath => AncientPresentationAssetProfile.MapIconPath;

        /// <inheritdoc />
        public virtual string? CustomMapIconOutlinePath => AncientPresentationAssetProfile.MapIconOutlinePath;

        /// <inheritdoc />
        public virtual string? CustomRunHistoryIconPath => AncientPresentationAssetProfile.RunHistoryIconPath;

        /// <inheritdoc />
        public virtual string? CustomRunHistoryIconOutlinePath =>
            AncientPresentationAssetProfile.RunHistoryIconOutlinePath;

        /// <inheritdoc />
        /// <remarks>
        ///     Default implementation scans <c>ancients</c> JSON (and other loaded loc) for this ancient&apos;s
        ///     <c>talk</c> keys. Override if you need a non-localized or custom dialogue structure.
        /// </remarks>
        protected override AncientDialogueSet DefineDialogues()
        {
            return AncientDialogueLocalization.BuildDialogueSetForModAncient(Id.Entry);
        }

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
            return CreateModRelicOption(
                relic,
                async () =>
                {
                    var owner = Owner ?? throw new InvalidOperationException(
                        $"Ancient '{Id.Entry}' had no owner when a relic option was chosen.");
                    relic.Owner = owner;
                    await RelicCmd.Obtain(relic, owner);
                    Done();
                },
                pageName);
        }

        /// <summary>
        ///     Creates a relic option with an explicit post-pick handler and localization key.
        ///     When <see cref="EventModel.Owner" /> is still null (e.g. dev-console completion on <c>AllPossibleOptions</c>),
        ///     <paramref name="relic" />.Owner is left unset until the option runs or real event flow assigns it.
        /// </summary>
        protected EventOption CreateModRelicOption(
            RelicModel relic,
            Func<Task>? onChosen,
            string pageName = "INITIAL")
        {
            relic.AssertMutable();
            if (Owner != null)
                relic.Owner = Owner;

            return EventOption.FromRelic(relic, this, onChosen, ModOptionKey(pageName, relic.Id.Entry));
        }
    }
}
