using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a base <see cref="EncounterModel" /> for mods with asset overrides, optional runtime combat
    ///         scene creation, act-specific eligibility, and a choice of act, encounter-specific, or programmatically
    ///         created combat backgrounds.
    ///     </para>
    ///     <para xml:lang="en">
    ///         Register it for one act through <c>RegisterActEncounter</c>, or for every act through
    ///         <c>RegisterGlobalEncounter</c>. Each <see cref="MonsterModel" /> used by the encounter must also be
    ///         registered.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为模组提供基础 <see cref="EncounterModel" />，支持资源替换、可选的运行时战斗场景创建、按章节限制
    ///         适用范围，以及在章节背景、遭遇专属背景和程序化背景之间进行选择。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可通过 <c>RegisterActEncounter</c> 将其注册到单个章节，或通过
    ///         <c>RegisterGlobalEncounter</c> 注册到所有章节。遭遇使用的每个 <see cref="MonsterModel" />
    ///         也必须注册。
    ///     </para>
    /// </summary>
    public abstract class ModEncounterTemplate : EncounterModel, IModEncounterAssetOverrides,
        IModEncounterCombatSceneFactory, IModEncounterActValidity
    {
        private BackgroundAssets? _programmaticCombatBackgroundSlot;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether combat uses the parent act's background. The default is <see langword="true" />.
        ///         Setting it to <see langword="false" /> enables encounter-specific background paths from
        ///         <see cref="AssetProfile" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取战斗是否使用所属章节的背景；默认为 <see langword="true" />。设为
        ///         <see langword="false" /> 时启用 <see cref="AssetProfile" /> 中的遭遇专属背景路径。
        ///     </para>
        /// </summary>
        protected virtual bool UseActCombatBackground => true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether <see cref="BuildProgrammaticCombatBackground" /> supplies the combat background.
        ///         Valid encounter-specific background paths take precedence.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取是否由 <see cref="BuildProgrammaticCombatBackground" /> 提供战斗背景。有效的遭遇专属背景路径
        ///         优先级更高。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Gets whether <see cref="HasScene" /> should be true without
        ///         <see cref="CustomEncounterScenePath" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取在没有 <see cref="CustomEncounterScenePath" /> 时 <see cref="HasScene" /> 是否仍应为
        ///         <see langword="true" />。
        ///     </para>
        /// </summary>
        protected virtual bool SuppliesEncounterCombatSceneFromFactory => false;

        /// <inheritdoc />
        public virtual bool IsValidForAct(ActModel act)
        {
            return true;
        }

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
        ///     <para xml:lang="en">
        ///         Returns a runtime-created combat root, or <see langword="null" /> to use scene-path loading.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回运行时创建的战斗根控件；返回 <see langword="null" /> 时使用场景路径加载。
        ///     </para>
        /// </summary>
        protected virtual Control? TryCreateEncounterCombatScene()
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds combat background assets when <see cref="UseProgrammaticCombatBackground" /> is enabled.
        ///         Return <see langword="null" /> to fall back to the base disk layout, or call
        ///         <c>parentAct.GenerateBackgroundAssets(rng)</c> to reuse the act background.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         启用 <see cref="UseProgrammaticCombatBackground" /> 时创建战斗背景资源。返回
        ///         <see langword="null" /> 会回退到游戏本体的磁盘布局；调用
        ///         <c>parentAct.GenerateBackgroundAssets(rng)</c> 可复用章节背景。
        ///     </para>
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
