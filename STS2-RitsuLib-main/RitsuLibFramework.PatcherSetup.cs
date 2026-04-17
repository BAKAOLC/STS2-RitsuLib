using STS2RitsuLib.Cards.FreePlay.Patches;
using STS2RitsuLib.Cards.Patches;
using STS2RitsuLib.Combat.CardTargeting.Patches;
using STS2RitsuLib.Combat.HealthBars.Patches;
using STS2RitsuLib.Combat.Rewards.Patches;
using STS2RitsuLib.Content.Patches;
using STS2RitsuLib.Interop.Patches;
using STS2RitsuLib.Lifecycle.Patches;
using STS2RitsuLib.Localization.Patches;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Relics.Patches;
using STS2RitsuLib.Scaffolding.Cards.HandGlow.Patches;
using STS2RitsuLib.Scaffolding.Cards.HandOutline.Patches;
using STS2RitsuLib.Scaffolding.Characters.Patches;
using STS2RitsuLib.Scaffolding.Content.Patches;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Settings.Patches;
using STS2RitsuLib.Timeline.Patches;
using STS2RitsuLib.Unlocks.Patches;
using STS2RitsuLib.Utils.Persistence.Patches;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     Android Arm64 + Mono 上 Harmony detour 仍存在 native SIGSEGV 风险。
        ///     当前先启用保守兼容模式，按补丁分组整体降级，优先保证游戏能启动。
        /// </summary>
        private static bool IsAndroidHarmonySafeMode()
        {
            return OperatingSystem.IsAndroid();
        }

        internal static ModPatcher GetFrameworkPatcher(FrameworkPatcherArea area)
        {
            lock (SyncRoot)
            {
                return FrameworkPatchersByArea.TryGetValue(area, out var patcher)
                    ? patcher
                    : throw new InvalidOperationException($"Framework patcher for area '{area}' is not available yet.");
            }
        }

        private static bool PatchAllRequired()
        {
            foreach (var area in Enum.GetValues<FrameworkPatcherArea>())
            {
                if (!FrameworkPatchersByArea.TryGetValue(area, out var patcher))
                    throw new InvalidOperationException($"Framework patcher for area '{area}' was not initialized.");

                if (!patcher.PatchAll())
                    return false;
            }

            return true;
        }

        private static void RegisterFrameworkPatcher(FrameworkPatcherArea area, ModPatcher patcher)
        {
            if (!FrameworkPatchersByArea.TryAdd(area, patcher))
                throw new InvalidOperationException($"Duplicate framework patcher registration for area '{area}'.");
        }

        private static void RegisterLifecyclePatches()
        {
            var patcher = CreatePatcher(Const.ModId, "framework-core", "framework core");
            if (IsAndroidHarmonySafeMode())
            {
                Logger.Warn(
                    "[Patcher - framework core] Android safe mode enabled. " +
                    "Keeping minimal ModelDb init compatibility, localization, BaseLib visual/property compatibility, and ancient dialogue shims on Arm64/Mono."
                );
                // Android still needs the lightweight ModelDb init lifecycle hook so ReflectionHelper type caches
                // are refreshed before ModelDb scans mod assemblies, and dynamically registered models can still
                // be injected without restoring the heavier lifecycle detour set.
                patcher.RegisterPatch<ModelRegistryLifecyclePatch>();
                patcher.RegisterPatch<NMainMenuContinueRunMissingCharacterPatch>();
                patcher.RegisterPatch<NContinueRunInfoShowInfoModelNotFoundPatch>();
                patcher.RegisterPatch<AndroidBaseLibCardUpgradeInternalCompatibilityPatch>();
                patcher.RegisterPatch<AndroidBaseLibEncounterGetBackgroundAssetsCompatibilityPatch>();
                patcher.RegisterPatch<AndroidBaseLibCharacterStringPropertyCompatibilityPatch>();
                patcher.RegisterPatch<AndroidBaseLibMonsterStringPropertyCompatibilityPatch>();
                patcher.RegisterPatch<AndroidBaseLibMonsterCreateVisualsCompatibilityPatch>();
                patcher.RegisterPatch<AndroidBaseLibCharacterCreateVisualsCompatibilityPatch>();
                patcher.RegisterPatch<AndroidBaseLibMonsterGenerateAnimatorCompatibilityPatch>();
                patcher.RegisterPatch<AndroidBaseLibCharacterGenerateAnimatorCompatibilityPatch>();
                patcher.RegisterPatch<EnergyIconHelperPathPatch>();
                patcher.RegisterPatch<LocTableHasEntryCompatibilityPatch>();
                patcher.RegisterPatch<LocTableGetLocStringCompatibilityPatch>();
                patcher.RegisterPatch<LocTableGetRawTextCompatibilityPatch>();
                patcher.RegisterPatch<AncientDialoguePopulateLocKeysPatch>();
                patcher.RegisterPatch<TheArchitectLoadDialogueMissingFallbackPatch>();
                RegisterFrameworkPatcher(FrameworkPatcherArea.Core, patcher);
                return;
            }

            patcher.RegisterPatch<ModTypeDiscoveryPatch>();
            patcher.RegisterPatch<SavedPropertiesTypeCacheInjectionPatch>();
            patcher.RegisterPatch<CoreInitializationLifecyclePatch>();
            patcher.RegisterPatch<NMainMenuContinueRunMissingCharacterPatch>();
            patcher.RegisterPatch<NMainMenuHarmonyPatchDumpPatch>();
            patcher.RegisterPatch<NContinueRunInfoShowInfoModelNotFoundPatch>();
            patcher.RegisterPatch<NRunHistoryRefreshAndSelectRunSuppressRethrowPatch>();
            patcher.RegisterPatch<RunHistoryMissingModelDbGetByIdTranspilerPatch>();
            patcher.RegisterPatch<NMultiplayerLoadGameScreenBeginRunMissingCharacterPatch>();
            patcher.RegisterPatch<NMultiplayerTestCharacterPaginatorAllCharactersPatch>();
            patcher.RegisterPatch<NCustomRunLoadScreenBeginRunMissingCharacterPatch>();
            patcher.RegisterPatch<NDailyRunLoadScreenBeginRunMissingCharacterPatch>();
            patcher.RegisterPatch<LocTableGetLocStringCompatibilityPatch>();
            patcher.RegisterPatch<LocTableGetRawTextCompatibilityPatch>();
            patcher.RegisterPatch<AncientDialoguePopulateLocKeysPatch>();
            patcher.RegisterPatch<TheArchitectLoadDialogueMissingFallbackPatch>();
            patcher.RegisterPatch<ModelRegistryLifecyclePatch>();
            patcher.RegisterPatch<GameNodeLifecyclePatch>();
            patcher.RegisterPatch<RunLifecyclePatch>();
            patcher.RegisterPatch<RunEndedLifecyclePatch>();
            patcher.RegisterPatch<CombatHookLifecyclePatch>();
            patcher.RegisterPatch<RewardHookLifecyclePatch>();
            patcher.RegisterPatch<GoldLossLifecyclePatch>();
            patcher.RegisterPatch<RelicObtainedLifecyclePatch>();
            patcher.RegisterPatch<RelicRemovedLifecyclePatch>();
            patcher.RegisterPatch<RoomHookLifecyclePatch>();
            patcher.RegisterPatch<ActHookLifecyclePatch>();
            patcher.RegisterPatch<RoomExitLifecyclePatch>();
            patcher.RegisterPatch<ActTransitionLifecyclePatch>();
            patcher.RegisterPatch<ActEnterMapSelectionSyncPatch>();
            patcher.RegisterPatch<SaveManagerLifecyclePatch>();
            patcher.RegisterPatch<RunSavingLifecyclePatch>();
            patcher.RegisterPatch<EpochLifecyclePatch>();
            patcher.RegisterPatch<UnlockIncrementLifecyclePatch>();
            patcher.RegisterPatch<GameOverScreenLifecyclePatch>();
            patcher.RegisterPatch<NHealthBarReadyForecastPatch>();
            patcher.RegisterPatch<CardModelShouldGlowGoldRegistryPatch>();
            patcher.RegisterPatch<CardModelShouldGlowRedRegistryPatch>();
            patcher.RegisterPatch<CardModelSetToFreeBindingPatch>();
            patcher.RegisterPatch<NHandCardHolderUpdateCardHandOutlinePatch>();
            patcher.RegisterPatch<NHandCardHolderFlashHandOutlinePatch>();
            patcher.RegisterPatch<NHealthBarRefreshForegroundForecastPatch>();
            patcher.RegisterPatch<NHealthBarRefreshMiddlegroundForecastPatch>();
            patcher.RegisterPatch<NHealthBarRefreshTextForecastPatch>();
            patcher.RegisterPatch<ArchaicToothGetTranscendenceStarterCardPatch>();
            patcher.RegisterPatch<ArchaicToothGetTranscendenceTransformedCardPatch>();
            patcher.RegisterPatch<ArchaicToothTranscendenceCardsPatch>();
            patcher.RegisterPatch<TouchOfOrobasGetUpgradedStarterRelicPatch>();
            patcher.RegisterPatch<CardModelIsValidTargetAnyPlayerPatch>();
            patcher.RegisterPatch<NCardPlayTryPlayCardAnyPlayerPatch>();
            patcher.RegisterPatch<NMouseCardPlayTargetSelectionAnyPlayerPatch>();
            patcher.RegisterPatch<NControllerCardPlayStartAnyPlayerPatch>();
            patcher.RegisterPatch<NControllerCardPlaySingleTargetingAnyPlayerPatch>();
            patcher.RegisterPatch<CardCmdAutoPlayAnyPlayerPatch>();
            patcher.RegisterPatch<CardRewardToSerializablePatch>();
            patcher.RegisterPatch<CombatRoomToSerializableRewardExtPatch>();
            patcher.RegisterPatch<CombatRoomFromSerializableRewardExtPatch>();
            patcher.RegisterPatch<RewardFromSerializableExtPatch>();
            RegisterFrameworkPatcher(FrameworkPatcherArea.Core, patcher);
        }

        private static void RegisterContentAssetPatches()
        {
            RitsuGodotNodeFactoryBootstrap.EnsureRegistered();

            var patcher = CreatePatcher(Const.ModId, "framework-content-assets", "content assets");
            if (IsAndroidHarmonySafeMode())
            {
                Logger.Warn(
                    "[Patcher - content assets] Android safe mode enabled. " +
                    "Skipping content-asset Harmony detours on Arm64/Mono."
                );
                RegisterFrameworkPatcher(FrameworkPatcherArea.ContentAssets, patcher);
                return;
            }

            patcher.RegisterPatch<EpochPortraitPathPatch>();
            patcher.RegisterPatch<CardPortraitPathPatch>();
            patcher.RegisterPatch<CardPortraitAvailabilityPatch>();
            patcher.RegisterPatch<CardTextureOverridePatch>();
            patcher.RegisterPatch<CardFrameMaterialPatch>();
            patcher.RegisterPatch<CardPoolFrameMaterialPatch>();
            patcher.RegisterPatch<CardAllPortraitPathsPatch>();
            patcher.RegisterPatch<CardOverlayPathPatch>();
            patcher.RegisterPatch<CardOverlayAvailabilityPatch>();
            patcher.RegisterPatch<CardOverlayCreatePatch>();
            patcher.RegisterPatch<CardBannerTexturePatch>();
            patcher.RegisterPatch<CardBannerMaterialPatch>();
            patcher.RegisterPatch<CardDynamicVarTooltipPatch>();
            patcher.RegisterPatch<DynamicVarTooltipClonePatch>();
            patcher.RegisterPatch<ModKeywordCardDescriptionPatches>();
            patcher.RegisterPatch<EnergyIconHelperPathPatch>();
            patcher.RegisterPatch<EnergyIconFormatterPatch>();

            patcher.RegisterPatch<RelicIconPathPatch>();
            patcher.RegisterPatch<RelicTexturePatch>();

            patcher.RegisterPatch<PowerIconPathPatch>();
            patcher.RegisterPatch<PowerTexturePatch>();
            patcher.RegisterPatch<PowerResolvedBigIconPathPatch>();

            patcher.RegisterPatch<OrbIconPatch>();
            patcher.RegisterPatch<OrbSpritePathPatch>();
            patcher.RegisterPatch<OrbAssetPathsPatch>();

            patcher.RegisterPatch<PotionImagePathPatch>();
            patcher.RegisterPatch<PotionTexturePatch>();

            patcher.RegisterPatch<AfflictionOverlayPathPatch>();
            patcher.RegisterPatch<AfflictionHasOverlayPatch>();
            patcher.RegisterPatch<AfflictionCreateOverlayPatch>();

            patcher.RegisterPatch<EnchantmentIntendedIconPathPatch>();

            patcher.RegisterPatch<ActBackgroundScenePathPatch>();
            patcher.RegisterPatch<ActRestSiteBackgroundPathPatch>();
            patcher.RegisterPatch<ActMapBackgroundPathPatch>();
            patcher.RegisterPatch<ActGenerateBackgroundAssetsPatch>();
            patcher.RegisterPatch<ActAssetPathsBackgroundLayersPatch>();

            patcher.RegisterPatch<ModModelRuntimeGodotFactoryPatches.MonsterCreatureVisualsRuntimeFactoryPatch>();
            patcher.RegisterPatch<ModModelRuntimeGodotFactoryPatches.EncounterCombatSceneRuntimeFactoryPatch>();
            patcher.RegisterPatch<ModModelRuntimeGodotFactoryPatches.EventLayoutPackedSceneRuntimeFactoryPatch>();
            patcher.RegisterPatch<ModModelRuntimeGodotFactoryPatches.EventBackgroundPackedSceneRuntimeFactoryPatch>();
            patcher.RegisterPatch<ModModelRuntimeGodotFactoryPatches.EventHasVfxRuntimeFactoryPatch>();
            patcher.RegisterPatch<ModModelRuntimeGodotFactoryPatches.EventCreateVfxRuntimeFactoryPatch>();
            patcher.RegisterPatch<ModModelRuntimeGodotFactoryPatches.OrbSpriteRuntimeFactoryPatch>();
            patcher.RegisterPatch<EncounterCreateScenePatch>();
            patcher.RegisterPatch<EncounterGetBackgroundAssetsProgrammaticPrepPatch>();
            patcher.RegisterPatch<EncounterCreateBackgroundAssetsForCustomPatch>();
            patcher.RegisterPatch<EncounterBossNodePathPatch>();
            patcher.RegisterPatch<EncounterMapNodeAssetPathsPatch>();
            patcher.RegisterPatch<EncounterGetAssetPathsPatch>();

            patcher.RegisterPatch<MonsterVisualsPathPatch>();

            patcher.RegisterPatch<RestSiteOptionIconPatch>();
            patcher.RegisterPatch<RestSiteOptionTitlePatch>();

            patcher.RegisterPatch<EventLayoutScenePatch>();
            patcher.RegisterPatch<EventInitialPortraitPatch>();
            patcher.RegisterPatch<EventBackgroundScenePathGetterPatch>();
            patcher.RegisterPatch<EventBackgroundScenePatch>();
            patcher.RegisterPatch<EventHasVfxPatch>();
            patcher.RegisterPatch<EventCreateVfxPatch>();
            patcher.RegisterPatch<EventGetAssetPathsPatch>();
            patcher.RegisterPatch<AncientMapIconTexturePatch>();
            patcher.RegisterPatch<AncientRunHistoryIconTexturePatch>();
            patcher.RegisterPatch<ImageHelperAncientModRunHistoryIconPathPatch>();
            patcher.RegisterPatch<ImageHelperModEncounterRunHistoryIconPathPatch>();
            patcher.RegisterPatch<AncientMapNodeAssetPathsPatch>();
            patcher.RegisterPatch<AncientEventProceduralBackgroundScenePatch>();
            patcher.RegisterPatch<NAncientEventLayoutProceduralStagePatch>();
            RegisterFrameworkPatcher(FrameworkPatcherArea.ContentAssets, patcher);
        }

        private static void RegisterSettingsUiPatches()
        {
            var patcher = CreatePatcher(Const.ModId, "framework-settings-ui", "settings ui");
            patcher.RegisterPatch<AndroidGraphicsSettingsCompatibilityPatch>();
            patcher.RegisterPatch<ModSettingsSubmenuPatch>();
            patcher.RegisterPatch<SettingsScreenModSettingsButtonPatch>();
            RegisterFrameworkPatcher(FrameworkPatcherArea.SettingsUi, patcher);
        }

        private static void RegisterCharacterAssetPatches()
        {
            var patcher = CreatePatcher(Const.ModId, "framework-character-assets", "character assets");
            if (IsAndroidHarmonySafeMode())
            {
                Logger.Warn(
                    "[Patcher - character assets] Android safe mode enabled. " +
                    "Keeping the compendium UI compatibility patch and skipping the rest of character-asset Harmony detours on Arm64/Mono."
                );
                patcher.RegisterPatch<AndroidCardLibraryCompendiumPatch>();
                RegisterFrameworkPatcher(FrameworkPatcherArea.CharacterAssets, patcher);
                return;
            }

            patcher.RegisterPatch<CharacterIconOutlineTexturePathPatch>();
            patcher.RegisterPatch<ModModelRuntimeGodotFactoryPatches.CharacterCreatureVisualsRuntimeFactoryPatch>();
            patcher.RegisterPatch<CharacterVisualsPathPatch>();
            patcher.RegisterPatch<CharacterEnergyCounterPathPatch>();
            patcher.RegisterPatch<CharacterMerchantAnimPathPatch>();
            patcher.RegisterPatch<CharacterRestSiteAnimPathPatch>();
            patcher.RegisterPatch<CharacterIconTexturePathPatch>();
            patcher.RegisterPatch<CharacterIconPathPatch>();
            patcher.RegisterPatch<CharacterSelectBgPathPatch>();
            patcher.RegisterPatch<CharacterSelectIconPathPatch>();
            patcher.RegisterPatch<CharacterSelectLockedIconPathPatch>();
            patcher.RegisterPatch<CharacterSelectTransitionPathPatch>();
            patcher.RegisterPatch<CharacterTrailPathPatch>();
            patcher.RegisterPatch<CharacterTrailStyleOverridePatch>();
            patcher.RegisterPatch<CharacterAttackSfxPatch>();
            patcher.RegisterPatch<CharacterCastSfxPatch>();
            patcher.RegisterPatch<CharacterDeathSfxPatch>();
            patcher.RegisterPatch<CharacterArmPointingTexturePathPatch>();
            patcher.RegisterPatch<CharacterArmRockTexturePathPatch>();
            patcher.RegisterPatch<CharacterArmPaperTexturePathPatch>();
            patcher.RegisterPatch<CharacterArmScissorsTexturePathPatch>();
            patcher.RegisterPatch<CharacterCombatSpineOverridePatch>();
            patcher.RegisterPatch<CharacterGameOverScreenCompatibilityPatch>();
            patcher.RegisterPatch<CharacterVanillaSelectionPolicyPatches>();
            patcher.RegisterPatch<ModCreatureNonSpineAnimationPlaybackPatch>();
            patcher.RegisterPatch<ModMerchantCharacterVisualPlaybackPatch>();
            patcher.RegisterPatch<NMerchantRoomProceduralCharacterInstantiationPatch>();
            patcher.RegisterPatch<NRestSiteCharacterCreateProceduralPatch>();
            patcher.RegisterPatch<NRestSiteRoomProceduralVisualPlaybackPatch>();
            patcher.RegisterPatch<CardLibraryCompendiumPatch>();
            patcher.RegisterPatch<StatsScreenCharacterStatsPatch>();
            RegisterFrameworkPatcher(FrameworkPatcherArea.CharacterAssets, patcher);
        }

        private static void RegisterContentRegistryPatches()
        {
            var patcher = CreatePatcher(Const.ModId, "framework-content-registry", "content registry");
            if (IsAndroidHarmonySafeMode())
            {
                Logger.Warn(
                    "[Patcher - content registry] Android safe mode enabled. " +
                    "Keeping fixed ModelDb entry compatibility, minimal model enumeration patches, and ModelIdSerializationCache sync patch, " +
                    "and skipping the rest of content-registry Harmony detours on Arm64/Mono."
                );

                // Android still needs a minimal set of getter postfixes so ModelDb-injected content can show up in
                // character selection, card/relic/potion pool enumeration, and runtime lookups without restoring the
                // higher-risk act/unlock/content-registry detour chain.
                patcher.RegisterPatch<ModelDbModdedEntryPatch>();
                patcher.RegisterPatch<AllCharactersPatch>();
                patcher.RegisterPatch<AllMonstersPatch>();
                patcher.RegisterPatch<AllPowersPatch>();
                patcher.RegisterPatch<AllOrbsPatch>();
                patcher.RegisterPatch<AllSharedCardPoolsPatch>();
                patcher.RegisterPatch<AllRelicPoolsPatch>();
                patcher.RegisterPatch<AllPotionPoolsPatch>();
                patcher.RegisterPatch<ModelIdSerializationCacheDynamicContentPatch>();
                RegisterFrameworkPatcher(FrameworkPatcherArea.ContentRegistry, patcher);
                return;
            }

            patcher.RegisterPatch<AllCharactersPatch>();
            patcher.RegisterPatch<AllMonstersPatch>();
            patcher.RegisterPatch<ActsPatch>();
            patcher.RegisterPatch<AllPowersPatch>();
            patcher.RegisterPatch<AllOrbsPatch>();
            patcher.RegisterPatch<AllSharedCardPoolsPatch>();
            patcher.RegisterPatch<AllSharedEventsPatch>();
            patcher.RegisterPatch<AllEventsPatch>();
            patcher.RegisterPatch<AllSharedAncientsPatch>();
            patcher.RegisterPatch<AllAncientsPatch>();
            patcher.RegisterPatch<DebugEnchantmentsPatch>();
            patcher.RegisterPatch<DebugAfflictionsPatch>();
            patcher.RegisterPatch<AchievementsPatch>();
            patcher.RegisterPatch<GoodModifiersPatch>();
            patcher.RegisterPatch<BadModifiersPatch>();
            patcher.RegisterPatch<AllRelicPoolsPatch>();
            patcher.RegisterPatch<AllPotionPoolsPatch>();
            patcher.RegisterPatch<ModelDbModdedEntryPatch>();
            patcher.RegisterPatch<ModelIdSerializationCacheDynamicContentPatch>();
            patcher.RegisterPatch<DynamicActContentPatchBootstrap>();
            RegisterFrameworkPatcher(FrameworkPatcherArea.ContentRegistry, patcher);
        }

        private static void RegisterPersistencePatches()
        {
            var patcher = CreatePatcher(Const.ModId, "framework-persistence", "persistence");
            patcher.RegisterPatch<ProfilePathInitializedPatch>();
            patcher.RegisterPatch<ProfileDeletePatch>();
            RegisterFrameworkPatcher(FrameworkPatcherArea.Persistence, patcher);
        }

        private static void RegisterUnlockPatches()
        {
            var patcher = CreatePatcher(Const.ModId, "framework-unlocks", "unlocks");
            if (IsAndroidHarmonySafeMode())
            {
                Logger.Warn(
                    "[Patcher - unlocks] Android safe mode enabled. " +
                    "Skipping unlock Harmony detours on Arm64/Mono."
                );
                RegisterFrameworkPatcher(FrameworkPatcherArea.Unlocks, patcher);
                return;
            }

            patcher.RegisterPatch<CharacterUnlockFilterPatch>();
            patcher.RegisterPatch<CharacterUnlockEpochRuntimeCompatibilityPatch>();
            patcher.RegisterPatch<SharedAncientUnlockFilterPatch>();
            patcher.RegisterPatch<CardUnlockFilterPatch>();
            patcher.RegisterPatch<RelicUnlockFilterPatch>();
            patcher.RegisterPatch<PotionUnlockFilterPatch>();
            patcher.RegisterPatch<GeneratedRoomEventUnlockFilterPatch>();
            patcher.RegisterPatch<EliteEpochCompatibilityPatch>();
            patcher.RegisterPatch<EliteEpochAfterCombatFallbackPatch>();
            patcher.RegisterPatch<BossEpochCompatibilityPatch>();
            patcher.RegisterPatch<AscensionOneEpochCompatibilityPatch>();
            patcher.RegisterPatch<PostRunCharacterUnlockEpochCompatibilityPatch>();
            patcher.RegisterPatch<AscensionEpochRevealCompatibilityPatch>();
            patcher.RegisterPatch<ProgressSaveManagerGetRevealableEpochsModTemplatePatch>();
            patcher.RegisterPatch<QueueTimelineExpansionSyncEpochIdListPatch>();
            patcher.RegisterPatch<NeowEpochQueueUnlocksCoExpansionScopePatch>();
            patcher.RegisterPatch<QueueTimelineExpansionUnlockModSlotsAfterNeowPatch>();
            patcher.RegisterPatch<NUnlockTimelineScreenExpansionSlotSortPatch>();
            patcher.RegisterPatch<NTimelineScreenAddEpochSlotsMergeModTemplatesPatch>();
            patcher.RegisterPatch<NTimelineScreenGetEraIconPolicyPatch>();
            patcher.RegisterPatch<NEraColumnHideEmptyIconPatch>();
            RegisterFrameworkPatcher(FrameworkPatcherArea.Unlocks, patcher);
        }

        internal enum FrameworkPatcherArea
        {
            Core,
            SettingsUi,
            ContentAssets,
            CharacterAssets,
            ContentRegistry,
            Persistence,
            Unlocks,
        }
    }
}
