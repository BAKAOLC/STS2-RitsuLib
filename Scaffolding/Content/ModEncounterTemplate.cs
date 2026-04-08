using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="EncounterModel" /> for mods: <see cref="IModEncounterAssetOverrides" /> (combat scene path,
    ///     backgrounds, boss node, map-node preload, extra paths), optional <see cref="TryCreateEncounterCombatScene" />.
    ///     The background pipeline matches vanilla <c>EncounterModel.HasCustomBackground</c> semantics, with an explicit
    ///     switch to keep using the act’s combat
    ///     background when desired. For disk-free backgrounds, set <see cref="UseProgrammaticCombatBackground" /> and
    ///     implement <see cref="BuildProgrammaticCombatBackground" /> using <see cref="CombatBackgroundAssetsFactory" /> (or
    ///     reuse <see cref="ActModel.GenerateBackgroundAssets" />).
    ///     <para />
    ///     <b>Registration:</b> act-only — <c>ModContentRegistry.RegisterActEncounter&lt;TAct, TEncounter&gt;()</c> or
    ///     <c>ModContentPackBuilder.ActEncounter&lt;TAct, TEncounter&gt;()</c>; all acts —
    ///     <c>RegisterGlobalEncounter&lt;TEncounter&gt;()</c> or
    ///     <c>GlobalEncounter&lt;TEncounter&gt;()</c>. Register each <see cref="MonsterModel" /> used in this encounter with
    ///     <c>RegisterMonster&lt;T&gt;()</c> / <c>Monster&lt;T&gt;()</c> so <c>ModelDb.Monsters</c> lists them.
    /// </summary>
    public abstract class ModEncounterTemplate : EncounterModel, IModEncounterAssetOverrides,
        IModEncounterCombatSceneFactory
    {
        private BackgroundAssets? _programmaticCombatBackgroundSlot;

        /// <summary>
        ///     When <c>true</c> (default), combat background comes from the parent act’s
        ///     <see cref="MegaCrit.Sts2.Core.Models.ActModel.GenerateBackgroundAssets" />; profile paths from
        ///     <see cref="ContentAssetProfiles.Encounter(string, string?, string?)" /> are ignored for background selection (they
        ///     still preload encounter scenes / map art where applicable). When <c>false</c>, use encounter-specific layers / main
        ///     scene from <see cref="AssetProfile" />, like vanilla <c>HasCustomBackground</c>.
        /// </summary>
        protected virtual bool UseActCombatBackground => true;

        /// <summary>
        ///     When <c>true</c>, <see cref="BuildProgrammaticCombatBackground" /> supplies combat
        ///     <see cref="BackgroundAssets" /> instead of loading <c>res://scenes/backgrounds/&lt;encounter-id&gt;/…</c>.
        ///     Ignored when <see cref="IModEncounterAssetOverrides.CustomBackgroundScenePath" /> or
        ///     <see cref="IModEncounterAssetOverrides.CustomBackgroundLayersDirectoryPath" /> resolves to a valid path
        ///     (path-based custom background wins).
        /// </summary>
        protected virtual bool UseProgrammaticCombatBackground => false;

        internal bool UsesProgrammaticCombatBackground => UseProgrammaticCombatBackground;

        /// <inheritdoc />
        protected override bool HasCustomBackground =>
            UseProgrammaticCombatBackground
            || (!UseActCombatBackground && (
                !string.IsNullOrWhiteSpace(CustomBackgroundLayersDirectoryPath)
                || !string.IsNullOrWhiteSpace(CustomBackgroundScenePath)));

        /// <inheritdoc />
        public override bool HasScene =>
            base.HasScene
            || SuppliesEncounterCombatSceneFromFactory
            || (!string.IsNullOrWhiteSpace(CustomEncounterScenePath)
                && ResourceLoader.Exists(CustomEncounterScenePath));

        /// <summary>
        ///     <c>true</c> when <see cref="HasScene" /> should be true without <see cref="CustomEncounterScenePath" />.
        /// </summary>
        protected virtual bool SuppliesEncounterCombatSceneFromFactory => false;

        /// <inheritdoc />
        public virtual EncounterAssetProfile AssetProfile => EncounterAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomEncounterScenePath => AssetProfile.EncounterScenePath;

        /// <inheritdoc />
        public virtual string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;

        /// <inheritdoc />
        public virtual string? CustomBackgroundLayersDirectoryPath => AssetProfile.BackgroundLayersDirectoryPath;

        /// <inheritdoc />
        public virtual string? CustomBossNodePath => AssetProfile.BossNodeSpinePath;

        /// <inheritdoc />
        public virtual IEnumerable<string>? CustomExtraAssetPaths => AssetProfile.ExtraAssetPaths;

        /// <inheritdoc />
        public virtual IEnumerable<string>? CustomMapNodeAssetPaths => AssetProfile.MapNodeAssetPaths;

        bool IModEncounterCombatSceneFactory.SuppliesEncounterCombatSceneFromFactory =>
            SuppliesEncounterCombatSceneFromFactory;

        Control? IModEncounterCombatSceneFactory.TryCreateEncounterCombatScene()
        {
            return TryCreateEncounterCombatScene();
        }

        /// <summary>
        ///     Non-null combat root control; otherwise the default encounter scene path is used.
        /// </summary>
        protected virtual Control? TryCreateEncounterCombatScene()
        {
            return null;
        }

        /// <summary>
        ///     Build combat background assets when <see cref="UseProgrammaticCombatBackground" /> is <c>true</c>.
        ///     Return <c>null</c> to fall back to vanilla disk layout (may throw if folders are missing). To reuse the act
        ///     background, return <c>parentAct.GenerateBackgroundAssets(rng)</c>.
        /// </summary>
        protected virtual BackgroundAssets? BuildProgrammaticCombatBackground(ActModel parentAct, Rng rng)
        {
            return null;
        }

        internal void PrepareProgrammaticCombatBackground(ActModel parentAct, Rng rng)
        {
            _programmaticCombatBackgroundSlot = null;
            if (!UseProgrammaticCombatBackground)
                return;

            try
            {
                _programmaticCombatBackgroundSlot = BuildProgrammaticCombatBackground(parentAct, rng);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Assets] Mod encounter '{Id.Entry}' programmatic combat background failed ({ex.GetType().Name}: {ex.Message}).");
            }
        }

        internal void AbandonProgrammaticCombatBackgroundSlot()
        {
            _programmaticCombatBackgroundSlot = null;
        }

        internal BackgroundAssets? ConsumeProgrammaticCombatBackgroundSlot()
        {
            var slot = _programmaticCombatBackgroundSlot;
            _programmaticCombatBackgroundSlot = null;
            return slot;
        }
    }
}
