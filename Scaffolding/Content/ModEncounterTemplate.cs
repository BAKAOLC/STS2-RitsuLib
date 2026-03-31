using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="EncounterModel" /> for mods: wires <see cref="IModEncounterAssetOverrides" /> for combat scene,
    ///     encounter-specific backgrounds (main scene + <c>_bg_</c>/<c>_fg_</c> layers), boss map node path, optional map-node
    ///     preload list, and extra asset paths. Background pipeline matches vanilla
    ///     <c>EncounterModel.HasCustomBackground</c> semantics, with an explicit switch to keep using the act’s combat
    ///     background when desired.
    ///     <para />
    ///     <b>Registration:</b> act-only — <c>ModContentRegistry.RegisterActEncounter&lt;TAct, TEncounter&gt;()</c> or
    ///     <c>ModContentPackBuilder.ActEncounter&lt;TAct, TEncounter&gt;()</c>; all acts —
    ///     <c>RegisterGlobalEncounter&lt;TEncounter&gt;()</c> or
    ///     <c>GlobalEncounter&lt;TEncounter&gt;()</c>. Register each <see cref="MonsterModel" /> used in this encounter with
    ///     <c>RegisterMonster&lt;T&gt;()</c> / <c>Monster&lt;T&gt;()</c> so <c>ModelDb.Monsters</c> lists them.
    /// </summary>
    public abstract class ModEncounterTemplate : EncounterModel, IModEncounterAssetOverrides
    {
        /// <summary>
        ///     When <c>true</c> (default), this encounter uses the parent act’s
        ///     <see cref="MegaCrit.Sts2.Core.Models.ActModel.GenerateBackgroundAssets" />
        ///     unless you set <see cref="CustomBackgroundScenePath" /> / <see cref="CustomBackgroundLayersDirectoryPath" /> in
        ///     <see cref="AssetProfile" />.
        ///     When <c>false</c>, this encounter always uses the encounter-specific background tree
        ///     (<c>res://scenes/backgrounds/&lt;id&gt;/…</c>), like vanilla <c>HasCustomBackground</c>.
        /// </summary>
        protected virtual bool UseActCombatBackground => true;

        /// <inheritdoc />
        protected override bool HasCustomBackground =>
            !UseActCombatBackground
            || !string.IsNullOrWhiteSpace(CustomBackgroundLayersDirectoryPath)
            || !string.IsNullOrWhiteSpace(CustomBackgroundScenePath);

        /// <inheritdoc />
        public override bool HasScene =>
            base.HasScene || !string.IsNullOrWhiteSpace(CustomEncounterScenePath);

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
    }
}
