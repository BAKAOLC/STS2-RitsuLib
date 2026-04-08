using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="EventModel" /> for mods: localization helpers, relic options,
    ///     <see cref="IModEventAssetOverrides" />
    ///     paths, and optional runtime hooks <see cref="TryCreateLayoutPackedScene" />,
    ///     <see cref="TryCreateBackgroundPackedScene" />, <see cref="TryCreateEventVfx" />.
    /// </summary>
    public abstract class ModEventTemplate : EventModel, IModEventAssetOverrides, IModEventLayoutPackedSceneFactory,
        IModEventBackgroundPackedSceneFactory, IModEventVfxFactory
    {
        /// <summary>
        ///     <c>true</c> enables <see cref="TryCreateEventVfx" /> instead of the default VFX path.
        /// </summary>
        protected virtual bool SuppliesCustomEventVfx => false;

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

        PackedScene? IModEventBackgroundPackedSceneFactory.TryCreateBackgroundPackedScene()
        {
            return TryCreateBackgroundPackedScene();
        }

        PackedScene? IModEventLayoutPackedSceneFactory.TryCreateLayoutPackedScene()
        {
            return TryCreateLayoutPackedScene();
        }

        bool IModEventVfxFactory.SuppliesCustomEventVfx => SuppliesCustomEventVfx;

        Node2D? IModEventVfxFactory.TryCreateEventVfx()
        {
            return TryCreateEventVfx();
        }

        /// <summary>
        ///     Non-null layout scene; otherwise <see cref="CustomLayoutScenePath" /> resolution runs.
        /// </summary>
        protected virtual PackedScene? TryCreateLayoutPackedScene()
        {
            return null;
        }

        /// <summary>
        ///     Non-null background scene; otherwise <see cref="CustomBackgroundScenePath" /> resolution runs.
        /// </summary>
        protected virtual PackedScene? TryCreateBackgroundPackedScene()
        {
            return null;
        }

        /// <summary>
        ///     VFX root when <see cref="SuppliesCustomEventVfx" /> is <c>true</c>; <c>null</c> falls through to path loading.
        /// </summary>
        protected virtual Node2D? TryCreateEventVfx()
        {
            return null;
        }

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
