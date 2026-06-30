using STS2RitsuLib.Audio.Patches;
using STS2RitsuLib.CardPiles.Patches;
using STS2RitsuLib.Cards.FreePlay.Patches;
using STS2RitsuLib.Cards.Patches;
using STS2RitsuLib.Cards.Transforms.Patches;
using STS2RitsuLib.CardTags.Patches;
using STS2RitsuLib.Combat.AttackHits.Patches;
using STS2RitsuLib.Combat.CardTargeting.Patches;
using STS2RitsuLib.Combat.Healing.Patches;
using STS2RitsuLib.Combat.HealthBars.Patches;
using STS2RitsuLib.Combat.Rewards.Patches;
using STS2RitsuLib.Combat.SecondaryResources.Patches;
using STS2RitsuLib.Combat.Ui.Patches;
using STS2RitsuLib.Content.Patches;
using STS2RitsuLib.Diagnostics;
using STS2RitsuLib.Diagnostics.Patches;
using STS2RitsuLib.Interop.Patches;
using STS2RitsuLib.Keywords.Patches;
using STS2RitsuLib.Lifecycle.Patches;
using STS2RitsuLib.Localization.Patches;
using STS2RitsuLib.Models.Capabilities.Patches;
using STS2RitsuLib.Models.Identity.Patches;
using STS2RitsuLib.Models.Patches;
using STS2RitsuLib.Networking.JoinDiagnostics.Patches;
using STS2RitsuLib.Networking.ManagedActions.Patches;
using STS2RitsuLib.Networking.Sidecar.Patches;
using STS2RitsuLib.Networking.StateDivergence.Patches;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Platform;
using STS2RitsuLib.Platform.Patches;
using STS2RitsuLib.Relics.Patches;
using STS2RitsuLib.Relics.Visibility.Patches;
using STS2RitsuLib.RunData.Patches;
using STS2RitsuLib.RuntimeInput.Patches;
using STS2RitsuLib.Saves.Patches;
using STS2RitsuLib.Scaffolding.Cards.HandGlow.Patches;
using STS2RitsuLib.Scaffolding.Cards.HandOutline.Patches;
using STS2RitsuLib.Scaffolding.Characters.Patches;
using STS2RitsuLib.Scaffolding.Content.Patches;
using STS2RitsuLib.Settings.Patches;
using STS2RitsuLib.Timeline.Patches;
using STS2RitsuLib.TopBar.Patches;
using STS2RitsuLib.Ui.RichTextEffects.Patches;
using STS2RitsuLib.Unlocks.Patches;
using STS2RitsuLib.Utils.Persistence.Patches;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
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

                if (!RitsuLibStartupAudit.Measure(
                        $"patchAll.{area}({patcher.RegisteredPatchCount})", patcher.PatchAll))
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
            patcher.RegisterPatch<ReflectionHelperModTypeCachePostModLoadPatch>();
            patcher.RegisterPatch<ModTypeDiscoveryPatch>();
            patcher.RegisterPatch<NAudioManagerGuidMappedStudioEventsPatches.PlayOneShot>();
            patcher.RegisterPatch<NAudioManagerGuidMappedStudioEventsPatches.PlayLoop>();
            patcher.RegisterPatch<NAudioManagerGuidMappedStudioEventsPatches.StopLoop>();
            patcher.RegisterPatch<NAudioManagerGuidMappedStudioEventsPatches.SetParam>();
            patcher.RegisterPatch<NAudioManagerGuidMappedStudioEventsPatches.StopAllLoops>();
            patcher.RegisterPatch<NAudioManagerGuidMappedStudioEventsPatches.PlayMusic>();
            patcher.RegisterPatch<NAudioManagerGuidMappedStudioEventsPatches.StopMusic>();
            patcher.RegisterPatch<NAudioManagerGuidMappedStudioEventsPatches.UpdateMusicParameter>();
            patcher.RegisterPatch<NRunMusicControllerGuidMappedStudioEventsPatches.UpdateMusic>();
            patcher.RegisterPatch<NRunMusicControllerGuidMappedStudioEventsPatches.PlayCustomMusic>();
            patcher.RegisterPatch<NRunMusicControllerGuidMappedStudioEventsPatches.StopCustomMusic>();
            patcher.RegisterPatch<NRunMusicControllerGuidMappedStudioEventsPatches.StopMusic>();
            patcher.RegisterPatch<NRunMusicControllerGuidMappedStudioEventsPatches.UpdateMusicParameter>();
            patcher.RegisterPatch<NRunMusicControllerGuidMappedStudioEventsPatches.UpdateAmbience>();
            patcher.RegisterPatch<NRunMusicControllerGuidMappedStudioEventsPatches.TriggerCampfireGoingOut>();
            patcher.RegisterPatch<SavedPropertiesTypeCacheInjectionPatch>();
            patcher.RegisterPatch<SavedAttachedStatePatches.SavedPropertiesFromInternalPatch>();
            patcher.RegisterPatch<SavedAttachedStatePatches.SavedPropertiesFillInternalPatch>();
            patcher.RegisterPatch<ModelSavedDataPatches.SavedPropertiesFromInternalPatch>();
            patcher.RegisterPatch<ModelSavedDataPatches.SavedPropertiesFillInternalPatch>();
            patcher.RegisterPatch<RunSavedDataLoadRunSavePatch>();
            patcher.RegisterPatch<RunSavedDataLoadMultiplayerRunSavePatch>();
            patcher.RegisterPatch<RunSavedDataCanonicalizeSavePatch>();
            patcher.RegisterPatch<RunSavedDataFromSerializablePatch>();
            patcher.RegisterPatch<RunSavedDataToSavePatch>();
            patcher.RegisterPatch<RunSavedDataSaveStoreWritePatch>();
#if STS2_AT_LEAST_0_104_0
            patcher.RegisterPatch<RunSavedDataSaveRunPatch>();
#else
            patcher.RegisterPatch<RunSavedDataLegacySaveRunPatch>();
#endif
            patcher.RegisterPatch<RunSavedDataStartRunLobbyCtorPatch>();
            patcher.RegisterPatch<RunSavedDataStartRunLobbyCleanUpPatch>();
            patcher.RegisterPatch<RunSavedDataLobbyPlayerSetReadySerializePatch>();
            patcher.RegisterPatch<RunSavedDataLobbyPlayerSetReadyDeserializePatch>();
            patcher.RegisterPatch<RunSavedDataLobbyPlayerSetReadyHandlerPatch>();
            patcher.RegisterPatch<RunSavedDataLobbyPlayerChangedCharacterSerializePatch>();
            patcher.RegisterPatch<RunSavedDataLobbyPlayerChangedCharacterDeserializePatch>();
            patcher.RegisterPatch<RunSavedDataLobbyPlayerChangedCharacterHandlerPatch>();
            patcher.RegisterPatch<RunSavedDataLobbyPlayerJoinedPatch>();
            patcher.RegisterPatch<RunSavedDataPrepareNewRunPayloadPatch>();
            patcher.RegisterPatch<RunSavedDataLobbyBeginRunMessageSerializePatch>();
            patcher.RegisterPatch<RunSavedDataLobbyBeginRunMessageDeserializePatch>();
            patcher.RegisterPatch<RunSavedDataInitializeNewRunPatch>();
            patcher.RegisterPatch<RunSavedDataClientLoadJoinResponseMessageSerializePatch>();
            patcher.RegisterPatch<RunSavedDataClientLoadJoinResponseMessageDeserializePatch>();
            patcher.RegisterPatch<RunSavedDataClientRejoinResponseMessageSerializePatch>();
            patcher.RegisterPatch<RunSavedDataClientRejoinResponseMessageDeserializePatch>();
            patcher.RegisterPatch<RunSavedDataCombatReplaySerializePatch>();
            patcher.RegisterPatch<RunSavedDataCombatReplayDeserializePatch>();
            patcher.RegisterPatch<ProgressStatePreserveUnknownRecordsFromSerializablePatch>();
            patcher.RegisterPatch<ProgressStatePreserveUnknownRecordsToSerializablePatch>();
            patcher.RegisterPatch<ProgressStatePreserveUnknownRecordsLoadProgressPatch>();
            patcher.RegisterPatch<ModelCloneRegistryPatch>();
            patcher.RegisterPatch<CoreInitializationLifecyclePatch>();
            patcher.RegisterPatch<DevConsoleAutocompleteEnhancementPatch>();
            patcher.RegisterPatch<DevConsoleAutocompleteUnlockArgumentPatch>();
            patcher.RegisterPatch<RitsuSteamInputManifestInstallPatch>();
            patcher.RegisterPatch<RitsuSteamInputBackendProcessPatch>();
            patcher.RegisterPatch<SteamWorkshopRuntimeInstallCallbackPatch>();
            patcher.RegisterPatch<DevConsoleAutocompleteApplyCandidatePatch>();
            patcher.RegisterPatch<DevConsoleAutocompleteGhostTextPatch>();
            patcher.RegisterPatch<DevConsoleHistoryNavigationInputPatch>();
            patcher.RegisterPatch<DevConsoleHistoryNavigationProcessCommandPatch>();
            patcher.RegisterPatch<DevConsoleHistoryNavigationHideConsolePatch>();
            patcher.RegisterPatch<DevConsoleHistoryNavigationShowConsolePatch>();
            patcher.RegisterPatch<NMainMenuContinueRunMissingCharacterPatch>();
            patcher.RegisterPatch<NMainMenuReadyLifecyclePatch>();
            patcher.RegisterPatch<NContinueRunInfoShowInfoModelNotFoundPatch>();
            patcher.RegisterPatch<NRunHistoryRefreshAndSelectRunSuppressRethrowPatch>();
            patcher.RegisterPatch<SentryDiagnosticsTelemetryPatch>();
#if STS2_AT_LEAST_0_107_1
            patcher.RegisterPatch<SentryGdExtensionShutdown1071WorkaroundPatch>();
#endif
            patcher.RegisterPatch<RunHistoryMissingModelDbGetByIdTranspilerPatch>();
#if STS2_AT_LEAST_0_106_1
            patcher.RegisterPatch<NRunHistoryDroppedFilePreviewPatch>();
#endif
            patcher.RegisterPatch<NMultiplayerLoadGameScreenBeginRunMissingCharacterPatch>();
            patcher.RegisterPatch<NMultiplayerTestCharacterPaginatorAllCharactersPatch>();
            patcher.RegisterPatch<NCustomRunLoadScreenBeginRunMissingCharacterPatch>();
            patcher.RegisterPatch<RitsuLibModelDbInitPostfixPatch>();
            patcher.RegisterPatch<LocManagerGetTableI18NBridgePatch>();
            patcher.RegisterPatch<LocTableHasEntryI18NBridgePatch>();
            patcher.RegisterPatch<LocTableIsLocalKeyI18NBridgePatch>();
            patcher.RegisterPatch<LocTableGetRawTextI18NBridgePatch>();
            patcher.RegisterPatch<LocTableGetLocStringI18NBridgePatch>();
            patcher.RegisterPatch<LocTableGetLocStringCompatibilityPatch>();
            patcher.RegisterPatch<LocTableGetRawTextCompatibilityPatch>();
            patcher.RegisterPatch<LocManagerLoadLocFormattersPostfixPatch>();
            patcher.RegisterPatch<AncientDialoguePopulateLocKeysPatch>();
            patcher.RegisterPatch<AncientEventInitialOptionsRegistryPatch>();
            patcher.RegisterPatch<ModAncientActValidityPatch>();
            patcher.RegisterPatch<ModEncounterActValidityPatch>();
            patcher.RegisterPatch<TheArchitectLoadDialogueMissingFallbackPatch>();
            patcher.RegisterPatch<ModelRegistryLifecyclePatch>();
            patcher.RegisterPatch<GameNodeLifecyclePatch>();
            patcher.RegisterPatch<RunLifecyclePatch>();
            patcher.RegisterPatch<RitsuLibSidecarNetReceivePatch>();
            patcher.RegisterPatch<RitsuLibSidecarNativeTrailerSendPatch>();
            if (!RitsuLibMobileSteamRuntime.SuppressNativeSteamIntegration)
                patcher.RegisterPatch<RitsuLibSidecarNativeTrailerSteamSendPatch>();
#if STS2_AT_LEAST_0_106_1
            patcher.RegisterPatch<RitsuLibSidecarSyncNetBufferPatch>();
#endif
            patcher.RegisterPatch<RitsuLibSidecarSyncLocationChangedPatch>();
            patcher.RegisterPatch<RitsuLibManagedNetActionMessagePatches.RequestEnqueueManagedAction>();
            patcher.RegisterPatch<RitsuLibManagedNetActionMessagePatches.EnqueueManagedAction>();
            patcher.RegisterPatch<RitsuLibManagedNetActionMessagePatches.RequestSerialize>();
            patcher.RegisterPatch<RitsuLibManagedNetActionMessagePatches.RequestDeserialize>();
            patcher.RegisterPatch<RitsuLibManagedNetActionMessagePatches.AnnouncementSerialize>();
            patcher.RegisterPatch<RitsuLibManagedNetActionMessagePatches.AnnouncementDeserialize>();
            patcher.RegisterPatch<RitsuLibManagedNetActionMessagePatches.ReplayEventSerialize>();
            patcher.RegisterPatch<RitsuLibManagedNetActionMessagePatches.ReplayEventDeserialize>();
            patcher.RegisterPatch<ModModelIdentityPlayerRunStatePatch>();
            patcher.RegisterPatch<ModModelIdentityRunStateAddCardPatch>();
            patcher.RegisterPatch<ModModelIdentityCombatStateAddCardPatch>();
            patcher.RegisterPatch<CardCmdTransformPatch>();
            patcher.RegisterPatch<ModModelIdentityPlayerAddRelicPatch>();
            patcher.RegisterPatch<ModModelIdentityPlayerRemoveRelicPatch>();
            patcher.RegisterPatch<ModModelIdentityPlayerAddPotionPatch>();
            patcher.RegisterPatch<ModModelIdentityPlayerRemovePotionPatch>();
            patcher.RegisterPatch<ModModelIdentityPowerApplyPatch>();
            patcher.RegisterPatch<ModModelIdentityPowerRemovePatch>();
            patcher.RegisterPatch<ModModelIdentityEnchantmentPatch>();
            patcher.RegisterPatch<ModModelIdentityAfflictionPatch>();
            patcher.RegisterPatch<ModModelIdentityCombatStateAddCreaturePatch>();
            patcher.RegisterPatch<ModModelIdentityCombatStateRemoveCreaturePatch>();
            patcher.RegisterPatch<ModModelIdentityPlayerSyncPatch>();
            patcher.RegisterPatch<RitsuLibSidecarLobbyHelloPatch>();
            patcher.RegisterPatch<RitsuLibSidecarStartRunLobbyHostClientConnectedPatch>();
            patcher.RegisterPatch<RitsuLibSidecarStartRunLobbyHostClientDisconnectedPatch>();
            patcher.RegisterPatch<RitsuLibSidecarPreRunCapabilityGatePatch>();
            patcher.RegisterPatch<RitsuLibSidecarChecksumDivergenceRelayPatch>();
            patcher.RegisterPatch<JoinFailureDiagnosticsBeginAttemptPatch>();
            patcher.RegisterPatch<JoinFailureDiagnosticsInitialInfoSerializePatch>();
            patcher.RegisterPatch<JoinFailureDiagnosticsInitialInfoDeserializePatch>();
            patcher.RegisterPatch<JoinFailureDiagnosticsInitialInfoPatch>();
            patcher.RegisterPatch<JoinFailureDiagnosticsPopupCreatePatch>();
            patcher.RegisterPatch<JoinFailureDiagnosticsPopupReadyPatch>();
            patcher.RegisterPatch<StateDivergenceDiagnosticsLogPatch>();
            patcher.RegisterPatch<StateDivergenceSupplementSerializePatch>();
            patcher.RegisterPatch<StateDivergenceSupplementDeserializePatch>();
            patcher.RegisterPatch<StateDivergenceDiagnosticsPopupCreatePatch>();
            patcher.RegisterPatch<StateDivergenceDiagnosticsPopupReadyPatch>();
            patcher.RegisterPatch<RunEndedLifecyclePatch>();
            patcher.RegisterPatch<BeforeCombatStartLifecyclePatch>();
            patcher.RegisterPatch<AfterCombatEndLifecyclePatch>();
            patcher.RegisterPatch<AfterCombatVictoryLifecyclePatch>();
            patcher.RegisterPatch<BeforeSideTurnStartLifecyclePatch>();
            patcher.RegisterPatch<AfterSideTurnStartLifecyclePatch>();
            patcher.RegisterPatch<BeforeCardPlayedLifecyclePatch>();
            patcher.RegisterPatch<AfterCardPlayedLifecyclePatch>();
            patcher.RegisterPatch<AfterCardChangedPilesLifecyclePatch>();
            patcher.RegisterPatch<AfterCardDrawnLifecyclePatch>();
            patcher.RegisterPatch<AfterCardDiscardedLifecyclePatch>();
            patcher.RegisterPatch<AfterCardExhaustedLifecyclePatch>();
            patcher.RegisterPatch<BeforeFlushLifecyclePatch>();
#if !STS2_AT_LEAST_0_105_0
            patcher.RegisterPatch<AfterCardRetainedLifecyclePatch>();
#else
            patcher.RegisterPatch<AfterFlushLifecyclePatch>();
#endif
            patcher.RegisterPatch<BeforeDeathLifecyclePatch>();
            patcher.RegisterPatch<AfterDeathLifecyclePatch>();
            patcher.RegisterPatch<AfterGoldGainedLifecyclePatch>();
            patcher.RegisterPatch<AfterPotionProcuredLifecyclePatch>();
            patcher.RegisterPatch<AfterPotionDiscardedLifecyclePatch>();
            patcher.RegisterPatch<AfterRewardTakenLifecyclePatch>();
            patcher.RegisterPatch<GoldLossLifecyclePatch>();
            patcher.RegisterPatch<RelicObtainedLifecyclePatch>();
            patcher.RegisterPatch<RelicRemovedLifecyclePatch>();
            patcher.RegisterPatch<BeforeRoomEnteredLifecyclePatch>();
            patcher.RegisterPatch<AfterRoomEnteredLifecyclePatch>();
            patcher.RegisterPatch<ActHookLifecyclePatch>();
            patcher.RegisterPatch<AttackCommandExecuteHitHookPatch>();
            patcher.RegisterPatch<CreatureCmdHealHookPatch>();
            patcher.RegisterPatch<BeforeAttackLifecyclePatch>();
            patcher.RegisterPatch<AfterAttackLifecyclePatch>();
            patcher.RegisterPatch<AfterBlockBrokenLifecyclePatch>();
            patcher.RegisterPatch<AfterBlockClearedLifecyclePatch>();
            patcher.RegisterPatch<BeforeBlockGainedLifecyclePatch>();
            patcher.RegisterPatch<AfterBlockGainedLifecyclePatch>();
            patcher.RegisterPatch<BeforeCardAutoPlayedLifecyclePatch>();
            patcher.RegisterPatch<AfterCardEnteredCombatLifecyclePatch>();
            patcher.RegisterPatch<AfterCardGeneratedForCombatLifecyclePatch>();
            patcher.RegisterPatch<BeforeCardRemovedLifecyclePatch>();
            patcher.RegisterPatch<AfterCreatureAddedToCombatLifecyclePatch>();
            patcher.RegisterPatch<AfterCurrentHpChangedLifecyclePatch>();
            patcher.RegisterPatch<AfterEnergyResetLifecyclePatch>();
            patcher.RegisterPatch<AfterEnergySpentLifecyclePatch>();
            patcher.RegisterPatch<BeforeHandDrawLifecyclePatch>();
            patcher.RegisterPatch<AfterHandEmptiedLifecyclePatch>();
            patcher.RegisterPatch<AfterItemPurchasedLifecyclePatch>();
            patcher.RegisterPatch<AfterMapGeneratedLifecyclePatch>();
            patcher.RegisterPatch<AfterPlayerTurnStartLifecyclePatch>();
            patcher.RegisterPatch<BeforePotionUsedLifecyclePatch>();
            patcher.RegisterPatch<AfterPotionUsedLifecyclePatch>();
            patcher.RegisterPatch<AfterRestSiteHealLifecyclePatch>();
            patcher.RegisterPatch<AfterRestSiteSmithLifecyclePatch>();
            patcher.RegisterPatch<AfterShuffleLifecyclePatch>();
            patcher.RegisterPatch<AfterStarsGainedLifecyclePatch>();
            patcher.RegisterPatch<AfterStarsSpentLifecyclePatch>();
            patcher.RegisterPatch<AfterSummonLifecyclePatch>();
            patcher.RegisterPatch<AfterTakingExtraTurnLifecyclePatch>();
            patcher.RegisterPatch<BeforeTurnEndLifecyclePatch>();
            patcher.RegisterPatch<AfterTurnEndLifecyclePatch>();
            patcher.RegisterPatch<RoomExitLifecyclePatch>();
            patcher.RegisterPatch<ActEnteringLifecyclePatch>();
            patcher.RegisterPatch<RewardsScreenContinuingLifecyclePatch>();
            patcher.RegisterPatch<ActEnterMapSelectionSyncPatch>();
            patcher.RegisterPatch<ProfileIdInitializedLifecyclePatch>();
            patcher.RegisterPatch<ProfileSwitchLifecyclePatch>();
            patcher.RegisterPatch<ProgressSaveLifecyclePatch>();
            patcher.RegisterPatch<ProfileDeleteLifecyclePatch>();
            patcher.RegisterPatch<ModDataCloudSyncPatches.AfterInitProfileId>();
            patcher.RegisterPatch<ModDataCloudSyncPatches.AfterSwitchProfileId>();
            patcher.RegisterPatch<RunSavingLifecyclePatch>();
            patcher.RegisterPatch<EpochObtainedLifecyclePatch>();
            patcher.RegisterPatch<EpochRevealedLifecyclePatch>();
            patcher.RegisterPatch<UnlockIncrementLifecyclePatch>();
            patcher.RegisterPatch<GameOverScreenLifecyclePatch>();
            patcher.RegisterPatch<NHealthBarReadyForecastPatch>();
            patcher.RegisterPatch<NHealthBarRefreshForegroundOrderedPatch>();
            patcher.RegisterPatch<NHealthBarContainerResizeForecastPatch>();
            patcher.RegisterPatch<CardModelShouldGlowGoldRegistryPatch>();
            patcher.RegisterPatch<CardModelShouldGlowRedRegistryPatch>();
            patcher.RegisterPatch<CardModelSetToFreeThisTurnBindingPatch>();
            patcher.RegisterPatch<CardModelFreeThisTurnEndCleanupPatch>();
            patcher.RegisterPatch<CardModelFreeAfterPlayedCleanupPatch>();
            patcher.RegisterPatch<CardModelSetToFreeThisCombatBindingPatch>();
            patcher.RegisterPatch<NHandCardHolderUpdateCardHandOutlinePatch>();
            patcher.RegisterPatch<NHandCardHolderFlashHandOutlinePatch>();
            patcher.RegisterPatch<NHandCardHolderDynamicOutlineTickPatch>();
            patcher.RegisterPatch<NHealthBarRefreshMiddlegroundForecastPatch>();
            patcher.RegisterPatch<NHealthBarRefreshTextForecastPatch>();
            patcher.RegisterPatch<NPowerExtraCornerAmountLabelsPatch>();
            patcher.RegisterPatch<NPowerExtraCornerAmountLabelsExitTreePatch>();
            patcher.RegisterPatch<NRelicInventoryHolderExtraCornerAmountLabelsPatch>();
            patcher.RegisterPatch<NRelicInventoryHolderExtraCornerAmountLabelsExitTreePatch>();
            patcher.RegisterPatch<NRelicInventoryAddVisibilityPatch>();
            patcher.RegisterPatch<NRelicInventoryRemoveVisibilityPatch>();
            patcher.RegisterPatch<NRelicInventoryAnimateVisibilityPatch>();
            patcher.RegisterPatch<RelicModelHoverTipsVisibilityPatch>();
            patcher.RegisterPatch<RelicModelHoverTipsExcludingRelicVisibilityPatch>();
            patcher.RegisterPatch<NRelicBasicHolderVisibilityPatch>();
            patcher.RegisterPatch<NMultiplayerPlayerStateRelicObtainedVisibilityPatch>();
            patcher.RegisterPatch<NMultiplayerPlayerStateRelicRemovedVisibilityPatch>();
            patcher.RegisterPatch<NMultiplayerPlayerExpandedStateRelicClickVisibilityPatch>();
            patcher.RegisterPatch<NMultiplayerPlayerExpandedStateReadyVisibilityPatch>();
            patcher.RegisterPatch<HoveredModelTrackerRelicVisibilityPatch>();
            patcher.RegisterPatch<NIntentExtraCornerAmountLabelsPatch>();
            patcher.RegisterPatch<NIntentExtraCornerAmountLabelsExitTreePatch>();
            patcher.RegisterPatch<ArchaicToothGetTranscendenceStarterCardPatch>();
            patcher.RegisterPatch<ArchaicToothGetTranscendenceTransformedCardPatch>();
            patcher.RegisterPatch<ArchaicToothTranscendenceCardsPatch>();
            patcher.RegisterPatch<DustyTomeSetupForPlayerPatch>();
            patcher.RegisterPatch<TouchOfOrobasGetUpgradedStarterRelicPatch>();
            patcher.RegisterPatch<CardModelIsValidTargetAnyPlayerPatch>();
            patcher.RegisterPatch<NCardPlayTryPlayCardAnyPlayerPatch>();
            patcher.RegisterPatch<NMouseCardPlayTargetSelectionAnyPlayerPatch>();
            patcher.RegisterPatch<NControllerCardPlayStartAnyPlayerPatch>();
            patcher.RegisterPatch<NControllerCardPlaySingleTargetingAnyPlayerPatch>();
            patcher.RegisterPatch<CardCmdAutoPlayAnyPlayerPatch>();
            patcher.RegisterPatch<ModelDbInitCustomTargetTypeRegistrationPatch>();
            patcher.RegisterPatch<AttackCommandGetPossibleTargetsCustomTargetTypePatch>();
            patcher.RegisterPatch<NCardPlayShowMultiCreatureTargetingVisualsCustomTargetTypePatch>();
            patcher.RegisterPatch<ActionTargetExtensionsIsSingleTargetCustomTargetTypePatch>();
            patcher.RegisterPatch<NTargetManagerAllowedToTargetCreatureCustomTargetTypePatch>();
            patcher.RegisterPatch<CardModelCanPlayTargetingCustomTargetTypePatch>();
            patcher.RegisterPatch<CardModelIsValidTargetCustomTargetTypePatch>();
#if STS2_AT_LEAST_0_106_0
            patcher.RegisterPatch<PotionModelIsValidTargetCustomTargetTypePatch>();
#endif
            patcher.RegisterPatch<NCardPlayTryPlayCardCustomTargetTypePatch>();
            patcher.RegisterPatch<NMouseCardPlayTargetSelectionCustomTargetTypePatch>();
            patcher.RegisterPatch<NControllerCardPlayStartCustomTargetTypePatch>();
            patcher.RegisterPatch<NControllerCardPlaySingleTargetingCustomTargetTypePatch>();
            patcher.RegisterPatch<NPotionPopupCustomTargetLabelPatch>();
            patcher.RegisterPatch<NPotionHolderUsePotionCustomSingleTargetPatch>();
            patcher.RegisterPatch<PotionModelOnUseWrapperCustomMultiTargetVfxPatch>();
            patcher.RegisterPatch<HoverTipFactoryFromKeywordPatch>();
            patcher.RegisterPatch<CardModelKeywordsModSeedPatch>();
            patcher.RegisterPatch<CardModelTagsModSeedPatch>();
            patcher.RegisterPatch<CardModelHoverTipsModKeywordPatch>();
            patcher.RegisterPatch<ModelCapabilityHookListenerPatches.RunStateHookListenersPatch>();
            patcher.RegisterPatch<ModelCapabilityHookListenerPatches.CombatStateHookListenersPatch>();
            patcher.RegisterPatch<ModelCapabilityHookListenerPatches.HookPlayerChoiceContextConstructorPatch>();
            patcher.RegisterPatch<CardModelCanPlaySecondaryResourcesPatch>();
            patcher.RegisterPatch<CardModelSpendResourcesSecondaryResourcesPatch>();
            patcher.RegisterPatch<CardModelOnPlayWrapperSecondaryResourcesPatch>();
            patcher.RegisterPatch<HookBeforeCardPlayedSecondaryResourcesPatch>();
            patcher.RegisterPatch<CardModelEndOfTurnSecondaryResourcesPatch>();
            patcher.RegisterPatch<HookAfterSideTurnStartSecondaryResourcesPatch>();
            patcher.RegisterPatch<NCombatUiActivateSecondaryResourcesPatch>();
            patcher.RegisterPatch<NCombatUiAnimOutSecondaryResourcesPatch>();
            patcher.RegisterPatch<NCombatUiDeactivateSecondaryResourcesPatch>();
            patcher.RegisterPatch<NMultiplayerPlayerStateCombatSetUpSecondaryResourcesPatch>();
            patcher.RegisterPatch<NMultiplayerPlayerStateCombatEndedSecondaryResourcesPatch>();
            patcher.RegisterPatch<NCardUpdateVisualsSecondaryResourceCardUiPatch>();
            patcher.RegisterPatch<OrbModelCapabilityPatches.OrbQueueBeforeTurnEndPatch>();
            patcher.RegisterPatch<OrbModelCapabilityPatches.OrbQueueAfterTurnStartPatch>();
            patcher.RegisterPatch<OrbModelCapabilityPatches.OrbCmdPassivePatch>();
            patcher.RegisterPatch<OrbModelCapabilityPatches.AfterOrbEvokedHookPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.UpdateDynamicVarPreviewPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.TitlePatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.CardTypePatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.CardRarityPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.TargetTypePatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.TagsPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.EnergyCostPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.HasLocalEnergyCostModifierPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.StarCostPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.StarCostColorPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.IsPlayablePatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.HasTurnEndInHandEffectPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.ResultPileTypeForCardPlayPatch>();
            patcher.RegisterPatch<CardOnPlayHookPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.TransformCarryOverPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.FromSerializableUpgradeReplayPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.UpgradeInternalPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.FinalizeUpgradeInternalPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.DowngradeInternalPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.TransformPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.DescriptionPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.HoverTipsPatch>();
            patcher.RegisterPatch<CardOverlayCapabilityPatches.ReloadOverlayPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.ShouldGlowGoldPatch>();
            patcher.RegisterPatch<CardModelCapabilityPatches.ShouldGlowRedPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.RelicDynamicDescriptionPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.RelicDynamicEventDescriptionPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.PotionDynamicDescriptionPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.EnchantmentDynamicDescriptionPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.EnchantmentDynamicExtraCardTextPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.AfflictionDynamicDescriptionPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.AfflictionDynamicExtraCardTextPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.PowerDescriptionPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.PowerSmartDescriptionPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.PowerRemoteDescriptionPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.OrbDescriptionPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.OrbSmartDescriptionPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.RelicHoverTipsPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.RelicHoverTipsExcludingRelicPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.PowerHoverTipsPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.OrbHoverTipsPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.PotionHoverTipsPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.AfflictionHoverTipsPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.EnchantmentHoverTipsPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.CardRunAssetPathsPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.CharacterAssetPathsPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.CharacterSelectAssetPathsPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.OrbAssetPathsPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.MonsterAssetPathsPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.ActAssetPathsPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.EncounterAssetPathsPatch>();
            patcher.RegisterPatch<ModelDisplayCapabilityPatches.EventAssetPathsPatch>();
            patcher.RegisterPatch<CardRewardToSerializablePatch>();
            patcher.RegisterPatch<CombatRoomToSerializableRewardExtPatch>();
            if (!RitsuLibMobileSteamRuntime.SuppressNativeSteamIntegration)
                patcher.RegisterPatch<CombatRoomFromSerializableRewardExtPatch>();
            patcher.RegisterPatch<RewardFromSerializableExtPatch>();
            patcher.RegisterPatch<ModCardPileGetPatch>();
            patcher.RegisterPatch<ModCardPileIsCombatPatch>();
            patcher.RegisterPatch<ModCardPileGetTargetPositionPatch>();
            patcher.RegisterPatch<ModCardPileShuffleVfxStartPositionPatch>();
            patcher.RegisterPatch<ModCardPileAllPilesPatch>();
            patcher.RegisterPatch<ModCardPilePlayerPilesPatch>();
            patcher.RegisterPatch<ModCardPileFindOnTablePatch>();
            patcher.RegisterPatch<ModCardPileCombatPilesContainerReadyPatch>();
            patcher.RegisterPatch<ModCardPileCombatPilesContainerInitializePatch>();
            patcher.RegisterPatch<ModCardPileTopBarReadyPatch>();
            patcher.RegisterPatch<ModCardPileTopBarInitializePatch>();
            patcher.RegisterPatch<ModCardPileCombatUiReadyPatch>();
            patcher.RegisterPatch<ModCardPileCombatUiActivatePatch>();
            patcher.RegisterPatch<ModTopBarActionButtonReadyPatch>();
            patcher.RegisterPatch<ModTopBarActionButtonInitializePatch>();
            patcher.RegisterPatch<MegaRichTextLabelReadyModRichTextEffectPatch>();
            patcher.RegisterPatch<MegaRichTextLabelSetTextAutoSizeModRichTextEffectPatch>();
            RegisterFrameworkPatcher(FrameworkPatcherArea.Core, patcher);
        }

        private static void RegisterContentAssetPatches()
        {
            var patcher = CreatePatcher(Const.ModId, "framework-content-assets", "content assets");
            patcher.RegisterPatch<EpochPortraitPathPatch>();
            patcher.RegisterPatch<EpochBigPortraitPathPatch>();
#if STS2_AT_LEAST_0_106_0
            patcher.RegisterPatch<EpochArtPlaceholderPatch>();
#endif
            patcher.RegisterPatch<CardPortraitPathPatch>();
            patcher.RegisterPatch<CardBetaPortraitPathPatch>();
            patcher.RegisterPatch<CardPortraitAvailabilityPatch>();
            patcher.RegisterPatch<CardBetaPortraitAvailabilityPatch>();
            patcher.RegisterPatch<CardTextureOverridePatch>();
            patcher.RegisterPatch<CardPortraitBorderTexturePatch>();
            patcher.RegisterPatch<CardEnergyIconTexturePatch>();
#if STS2_AT_LEAST_0_105_0
            patcher.RegisterPatch<CardAncientBorderTexturePatch>();
#endif
            patcher.RegisterPatch<CardAncientTextBgTexturePatch>();
            patcher.RegisterPatch<CardFrameMaterialPatch>();
            patcher.RegisterPatch<CardPoolFrameMaterialPatch>();
            patcher.RegisterPatch<CardVisualStylePatch>();
            patcher.RegisterPatch<CardPortraitMaterialPatch>();
            patcher.RegisterPatch<CardNodeMaterialPatch>();
            patcher.RegisterPatch<CardAllPortraitPathsPatch>();
            patcher.RegisterPatch<CardOverlayPathPatch>();
            patcher.RegisterPatch<CardOverlayAvailabilityPatch>();
            patcher.RegisterPatch<CardOverlayCreatePatch>();
            patcher.RegisterPatch<CardBannerTexturePatch>();
            patcher.RegisterPatch<CardBannerMaterialPatch>();
            patcher.RegisterPatch<CardDynamicVarTooltipPatch>();
            patcher.RegisterPatch<DynamicVarTooltipClonePatch>();
            patcher.RegisterPatch<ContentSourceKeywordHoverTipPatch>();
            patcher.RegisterPatch<ContentSourceStaticHoverTipPatch>();
            patcher.RegisterPatch<ContentSourceEnergyHoverTipPatch>();
            patcher.RegisterPatch<ContentSourceCardHoverTipsPatch>();
            patcher.RegisterPatch<ContentSourceModelHoverTipPatch>();
            patcher.RegisterPatch<ContentSourcePotionFactoryHoverTipPatch>();
            patcher.RegisterPatch<ContentSourcePowerHoverTipsPatch>();
            patcher.RegisterPatch<ContentSourceNHoverTipSetShowPatch>();
            patcher.RegisterPatch<ContentSourceCreatureHoverTipsPatch>();
            patcher.RegisterPatch<ContentSourceEventLayoutBadgePatch>();
            patcher.RegisterPatch<ModKeywordCardDescriptionPatches>();
            patcher.RegisterPatch<EnergyIconHelperPathPatch>();
            patcher.RegisterPatch<EnergyIconFormatterPatch>();

            patcher.RegisterPatch<RelicIconPathPatch>();
            patcher.RegisterPatch<RelicPackedIconOutlinePathPatch>();
            patcher.RegisterPatch<RelicTexturePatch>();
            patcher.RegisterPatch<RelicIconOutlineTexturePatch>();
            patcher.RegisterPatch<RelicBigIconTexturePatch>();

            patcher.RegisterPatch<PowerIconPathPatch>();
            patcher.RegisterPatch<PowerTexturePatch>();
            patcher.RegisterPatch<PowerBigIconTexturePatch>();
            patcher.RegisterPatch<PowerResolvedBigIconPathPatch>();

            patcher.RegisterPatch<OrbIconPatch>();
            patcher.RegisterPatch<OrbSpritePathPatch>();
            patcher.RegisterPatch<OrbAssetPathsPatch>();
            patcher.RegisterPatch<NOrbValueDisplayPolicyPatch>();

            patcher.RegisterPatch<PotionImagePathPatch>();
            patcher.RegisterPatch<PotionOutlinePathPatch>();
            patcher.RegisterPatch<PotionTexturePatch>();
            patcher.RegisterPatch<PotionOutlineTexturePatch>();

            patcher.RegisterPatch<AfflictionOverlayPathPatch>();
            patcher.RegisterPatch<AfflictionHasOverlayPatch>();
            patcher.RegisterPatch<AfflictionCreateOverlayPatch>();

            patcher.RegisterPatch<EnchantmentIntendedIconPathPatch>();
            patcher.RegisterPatch<ModifierIconPathPatch>();

            patcher.RegisterPatch<ActBackgroundScenePathPatch>();
            patcher.RegisterPatch<ActRestSiteBackgroundPathPatch>();
            patcher.RegisterPatch<ActMapBackgroundPathPatch>();
            patcher.RegisterPatch<ActMapMidBackgroundPathPatch>();
            patcher.RegisterPatch<ActMapBottomBackgroundPathPatch>();
            patcher.RegisterPatch<ActGenerateBackgroundAssetsPatch>();
            patcher.RegisterPatch<ActAssetPathsBackgroundLayersPatch>();

            patcher.RegisterPatch<ModModelRuntimeGodotFactoryPatches.MonsterCreatureVisualsRuntimeFactoryPatch>();
            patcher.RegisterPatch<ModModelRuntimeGodotFactoryPatches.MonsterCreatureAnimatorRuntimeFactoryPatch>();
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
            patcher.RegisterPatch<AncientMapIconOutlineTexturePatch>();
            patcher.RegisterPatch<AncientRunHistoryIconTexturePatch>();
            patcher.RegisterPatch<AncientRunHistoryIconOutlineTexturePatch>();
            patcher.RegisterPatch<ImageHelperAncientModRunHistoryIconPathPatch>();
            patcher.RegisterPatch<ImageHelperModEncounterRunHistoryIconPathPatch>();
            patcher.RegisterPatch<AncientMapNodeAssetPathsPatch>();
            patcher.RegisterPatch<AncientEventProceduralBackgroundScenePatch>();
            patcher.RegisterPatch<NAncientEventLayoutProceduralStagePatch>();
            patcher.RegisterPatch<BadgePoolCreateAllPatch>();
            patcher.RegisterPatch<AssetCacheLoadBadgeFallbackPatch>();
            patcher.RegisterPatch<NBadgeCreateIconPatch>();
            patcher.RegisterPatch<BadgeIconGetterPatch>();
            RegisterFrameworkPatcher(FrameworkPatcherArea.ContentAssets, patcher);
        }

        private static void RegisterSettingsUiPatches()
        {
            var patcher = CreatePatcher(Const.ModId, "framework-settings-ui", "settings ui");
            patcher.RegisterPatch<ModSettingsSubmenuPatch>();
            patcher.RegisterPatch<ModSettingsRunSubmenuStackPatch>();
            patcher.RegisterPatch<SettingsSaveDuplicateWorkshopModListPatch>();
            patcher.RegisterPatch<SettingsScreenModSettingsButtonPatch>();
            patcher.RegisterPatch<MainMenuModSettingsButtonPatch>();
            RegisterFrameworkPatcher(FrameworkPatcherArea.SettingsUi, patcher);
        }

        private static void RegisterCharacterAssetPatches()
        {
            var patcher = CreatePatcher(Const.ModId, "framework-character-assets", "character assets");
            patcher.RegisterPatch<CharacterIconOutlineTexturePathPatch>();
            patcher.RegisterPatch<ModModelRuntimeGodotFactoryPatches.CharacterCreatureVisualsRuntimeFactoryPatch>();
            patcher.RegisterPatch<ModModelRuntimeGodotFactoryPatches.CharacterCreatureAnimatorRuntimeFactoryPatch>();
            patcher.RegisterPatch<CharacterVisualsPathPatch>();
            patcher.RegisterPatch<CharacterEnergyCounterRuntimeFactoryPatch>();
            patcher.RegisterPatch<CharacterEnergyCounterStarAnchorPatch>();
            patcher.RegisterPatch<CharacterEnergyCounterPathPatch>();
            patcher.RegisterPatch<CharacterMerchantAnimPathPatch>();
            patcher.RegisterPatch<CharacterRestSiteAnimPathPatch>();
            patcher.RegisterPatch<CharacterIconTexturePathPatch>();
            patcher.RegisterPatch<CharacterIconPathPatch>();
            patcher.RegisterPatch<ModModelRuntimeGodotFactoryPatches.CharacterIconRuntimeFactoryPatch>();
            patcher.RegisterPatch<CharacterSelectBgPathPatch>();
            patcher.RegisterPatch<CharacterSelectBackgroundRuntimeFactoryPatch>();
            patcher.RegisterPatch<CharacterSelectIconPathPatch>();
            patcher.RegisterPatch<CharacterSelectLockedIconPathPatch>();
            patcher.RegisterPatch<CharacterSelectTransitionPathPatch>();
            patcher.RegisterPatch<CharacterMapMarkerPathPatch>();
            patcher.RegisterPatch<CharacterTrailPathPatch>();
            patcher.RegisterPatch<CharacterTrailStyleOverridePatch>();
            patcher.RegisterPatch<CharacterAttackSfxPatch>();
            patcher.RegisterPatch<CharacterCastSfxPatch>();
            patcher.RegisterPatch<CharacterDeathSfxPatch>();
            patcher.RegisterPatch<ColorfulPhilosophersCardPoolColorOrderPatch>();
            patcher.RegisterPatch<CharacterArmPointingTexturePathPatch>();
            patcher.RegisterPatch<CharacterArmRockTexturePathPatch>();
            patcher.RegisterPatch<CharacterArmPaperTexturePathPatch>();
            patcher.RegisterPatch<CharacterArmScissorsTexturePathPatch>();
            patcher.RegisterPatch<CharacterCombatSpineOverridePatch>();
            patcher.RegisterPatch<CharacterGameOverScreenCompatibilityPatch>();
            patcher.RegisterPatch<CharacterVanillaSelectionPolicyPatches>();
            patcher.RegisterPatch<CharacterVanillaSelectionPolicyAllCharactersPatch>();
            patcher.RegisterPatch<CharacterSelectButtonScrollPatch>();
            patcher.RegisterPatch<CustomRunCharacterSelectButtonScrollPatch>();
            patcher.RegisterPatch<ModCreatureCombatAnimationPlaybackPatch>();
            patcher.RegisterPatch<NCreatureCombatAnimationInitialBootstrapPatch>();
            patcher.RegisterPatch<NCreatureNonSpineDeathAnimationTriggerPatch>();
            patcher.RegisterPatch<NCombatUiNonSpineDeathAnimationRewardDelayPatch>();
            patcher.RegisterPatch<NCreatureNonSpineReviveAnimationTriggerPatch>();
            patcher.RegisterPatch<ModMerchantCharacterVisualPlaybackPatch>();
            patcher.RegisterPatch<NMerchantRoomProceduralCharacterInstantiationPatch>();
            patcher.RegisterPatch<NFakeMerchantProceduralCharacterInstantiationPatch>();
            patcher.RegisterPatch<NRestSiteCharacterCreateProceduralPatch>();
            patcher.RegisterPatch<NRestSiteRoomProceduralVisualPlaybackPatch>();
            patcher.RegisterPatch<CardLibraryCompendiumPatch>();
            patcher.RegisterPatch<StatsScreenCharacterStatsPatch>();
            RegisterFrameworkPatcher(FrameworkPatcherArea.CharacterAssets, patcher);
        }

        private static void RegisterContentRegistryPatches()
        {
            var patcher = CreatePatcher(Const.ModId, "framework-content-registry", "content registry");
            patcher.RegisterPatch<AllCharactersPatch>();
            patcher.RegisterPatch<AllMonstersPatch>();
            patcher.RegisterPatch<ActsPatch>();
#if STS2_AT_LEAST_0_107_1
            patcher.RegisterPatch<ActsByIndexPatch>();
#endif
#if !STS2_AT_LEAST_0_107_1
            patcher.RegisterPatch<RelicCollectionActListPatch>();
#endif
            patcher.RegisterPatch<AllPowersPatch>();
            patcher.RegisterPatch<AllOrbsPatch>();
            patcher.RegisterPatch<OrbModelRandomPoolPolicyPatch>();
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
            patcher.RegisterPatch<MutuallyExclusiveModifiersPatch>();
            patcher.RegisterPatch<AllRelicPoolsPatch>();
            patcher.RegisterPatch<AllRelicsPatch>();
            patcher.RegisterPatch<AllPotionPoolsPatch>();
            patcher.RegisterPatch<ModelDbModdedEntryPatch>();
            patcher.RegisterPatch<ModelIdSerializationCacheDynamicContentPatch>();
            patcher.RegisterPatch<LocalOnlyModelIdSortingPatch>();
            patcher.RegisterPatch<DynamicActContentPatchBootstrap>();
            patcher.RegisterPatch<DynamicCharacterStarterContentPatchBootstrap>();
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
            patcher.RegisterPatch<CharacterUnlockEpochRuntimeCompatibilityPatch>();
            patcher.RegisterPatch<CardUnlockFilterPatch>();
            patcher.RegisterPatch<RelicUnlockFilterPatch>();
            patcher.RegisterPatch<PotionUnlockFilterPatch>();
            patcher.RegisterPatch<GeneratedRoomEventUnlockFilterPatch>();
            patcher.RegisterPatch<EliteEpochCompatibilityPatch>();
            patcher.RegisterPatch<BossEpochCompatibilityPatch>();
            patcher.RegisterPatch<AscensionOneEpochCompatibilityPatch>();
            patcher.RegisterPatch<PostRunCharacterUnlockEpochCompatibilityPatch>();
            patcher.RegisterPatch<AscensionEpochRevealCompatibilityPatch>();
            patcher.RegisterPatch<ModCharacterRootEpochBackfillPatch>();
            patcher.RegisterPatch<ModRequiredEpochUnlockStatePatch>();
            patcher.RegisterPatch<StartRunLobbyLocalHostUnlockStatePatch>();
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
