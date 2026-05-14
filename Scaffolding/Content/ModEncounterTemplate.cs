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
    ///     <see cref="ActModel.GenerateBackgroundAssets" />）。
    ///     <para />
    ///     Mod 遭遇的基础 <see cref="EncounterModel" />：<see cref="IModEncounterAssetOverrides" />（战斗场景路径、背景、Boss
    ///     节点、地图节点预加载、额外路径），以及可选 <see cref="TryCreateEncounterCombatScene" />。背景管线匹配原版
    ///     <c>EncounterModel.HasCustomBackground</c> 语义，并提供显式开关，以便需要时继续使用章节的战斗背景。对于无磁盘背景，请设置
    ///     <see cref="UseProgrammaticCombatBackground" />，并使用 <see cref="CombatBackgroundAssetsFactory" /> 实现
    ///     <see cref="BuildProgrammaticCombatBackground" />（或复用 <see cref="ActModel.GenerateBackgroundAssets" />）。
    ///     <para />
    ///     <b>注册：</b>仅某个章节 - <c>ModContentRegistry.RegisterActEncounter&lt;TAct, TEncounter&gt;()</c> 或
    ///     <c>ModContentPackBuilder.ActEncounter&lt;TAct, TEncounter&gt;()</c>；所有章节 -
    ///     <c>RegisterGlobalEncounter&lt;TEncounter&gt;()</c> 或 <c>GlobalEncounter&lt;TEncounter&gt;()</c>。请使用
    ///     <c>RegisterMonster&lt;T&gt;()</c> / <c>Monster&lt;T&gt;()</c> 注册此遭遇使用的每个 <see cref="MonsterModel" />，使
    ///     <c>ModelDb.Monsters</c> 能列出它们。
    ///     <see cref="ActModel.GenerateBackgroundAssets" />）。
    ///     <para />
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
        ///     为 <c>true</c>（默认）时，战斗背景来自父级章节的
        ///     <see cref="MegaCrit.Sts2.Core.Models.ActModel.GenerateBackgroundAssets" />；来自
        ///     <see cref="ContentAssetProfiles.Encounter(string, string?, string?)" /> 的 profile 路径不会参与背景选择（这些路径
        ///     仍会在适用时预加载遭遇场景 / 地图美术）。为 <c>false</c> 时，像原版 <c>HasCustomBackground</c> 一样使用遭遇专属图层 / 主
        ///     场景，来源为 <see cref="AssetProfile" />。
        /// </summary>
        protected virtual bool UseActCombatBackground => true;

        /// <summary>
        ///     When <c>true</c>, <see cref="BuildProgrammaticCombatBackground" /> supplies combat
        ///     <see cref="BackgroundAssets" /> instead of loading <c>res://scenes/backgrounds/&lt;encounter-id&gt;/…</c>.
        ///     Ignored when <see cref="IModEncounterAssetOverrides.CustomBackgroundScenePath" /> or
        ///     <see cref="IModEncounterAssetOverrides.CustomBackgroundLayersDirectoryPath" /> resolves to a valid path
        ///     (path-based custom background wins).
        ///     为 <c>true</c> 时，由 <see cref="BuildProgrammaticCombatBackground" /> 提供战斗
        ///     <see cref="BackgroundAssets" />，而不是加载 <c>res://scenes/backgrounds/&lt;encounter-id&gt;/…</c>。
        ///     如果 <see cref="IModEncounterAssetOverrides.CustomBackgroundScenePath" /> 或
        ///     <see cref="IModEncounterAssetOverrides.CustomBackgroundLayersDirectoryPath" /> 能解析到有效路径，则忽略此项
        ///     （基于路径的自定义背景优先）。
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
        ///     当没有 <see cref="CustomEncounterScenePath" /> 也应让 <see cref="HasScene" /> 为 true 时返回 <c>true</c>。
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

        /// <inheritdoc />
        public virtual string? CustomRunHistoryIconPath => AssetProfile.RunHistoryIconPath;

        /// <inheritdoc />
        public virtual string? CustomRunHistoryIconOutlinePath => AssetProfile.RunHistoryIconOutlinePath;

        bool IModEncounterCombatSceneFactory.SuppliesEncounterCombatSceneFromFactory =>
            SuppliesEncounterCombatSceneFromFactory;

        Control? IModEncounterCombatSceneFactory.TryCreateEncounterCombatScene()
        {
            return TryCreateEncounterCombatScene();
        }

        /// <summary>
        ///     Non-null combat root control; otherwise the default encounter scene path is used.
        ///     返回非 null 战斗根控件时直接使用；否则使用默认遭遇场景路径。
        /// </summary>
        protected virtual Control? TryCreateEncounterCombatScene()
        {
            return null;
        }

        /// <summary>
        ///     Build combat background assets when <see cref="UseProgrammaticCombatBackground" /> is <c>true</c>.
        ///     Return <c>null</c> to fall back to vanilla disk layout (may throw if folders are missing). To reuse the act
        ///     background, return <c>parentAct.GenerateBackgroundAssets(rng)</c>.
        ///     <c>parentAct.GenerateBackgroundAssets(rng)</c>。
        ///     当 <see cref="UseProgrammaticCombatBackground" /> 为 <c>true</c> 时构建战斗背景资源。返回 <c>null</c>
        ///     会回退到原版磁盘布局（若目录缺失可能抛出异常）。若要复用章节背景，请返回 <c>parentAct.GenerateBackgroundAssets(rng)</c>。
        ///     <c>parentAct.GenerateBackgroundAssets(rng)</c>。
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
