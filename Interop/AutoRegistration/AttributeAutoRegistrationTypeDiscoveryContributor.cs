using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Content;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Timeline;
using STS2RitsuLib.Timeline.Scaffolding;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     Built-in contributor: scans assemblies once for ritsulib auto-registration attributes, sorts them
    ///     deterministically,
    ///     and dispatches through the existing explicit registry APIs.
    /// </summary>
    public sealed class AttributeAutoRegistrationTypeDiscoveryContributor : IModTypeDiscoveryContributor
    {
        private static readonly Lock Gate = new();
        private static readonly HashSet<Assembly> ProcessedAssemblies = [];

        /// <inheritdoc />
        public void Contribute(Harmony harmony, IReadOnlyDictionary<string, Assembly> modAssembliesByManifestId,
            Type modType)
        {
            ArgumentNullException.ThrowIfNull(harmony);
            ArgumentNullException.ThrowIfNull(modAssembliesByManifestId);
            ArgumentNullException.ThrowIfNull(modType);

            var assembly = modType.Assembly;
            lock (Gate)
            {
                if (!ProcessedAssemblies.Add(assembly))
                    return;
            }

            ProcessAssembly(assembly, modAssembliesByManifestId);
        }

        private static void ProcessAssembly(Assembly assembly,
            IReadOnlyDictionary<string, Assembly> modAssembliesByManifestId)
        {
            var logger = RitsuLibFramework.Logger;
            var operations = new List<AutoRegistrationOperation>();
            var loadableTypes = AssemblyTypeScanHelper.GetLoadableTypes(assembly, logger);

            foreach (var type in loadableTypes)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                try
                {
                    operations.AddRange(BuildOperations(type, modAssembliesByManifestId));
                }
                catch (Exception ex)
                {
                    logger.Error(
                        $"[AutoRegister] Failed to inspect type '{type.FullName}' in assembly '{assembly.FullName}': {ex.Message}");
                }
            }

            if (operations.Count == 0)
                return;

            var orderedOperations = OrderOperations(operations);
            var succeeded = 0;
            var failed = 0;

            foreach (var operation in orderedOperations)
                try
                {
                    operation.Execute();
                    succeeded++;
                }
                catch (Exception ex)
                {
                    failed++;
                    logger.Error(
                        $"[AutoRegister] {operation.AttributeName} failed for '{operation.SourceType.FullName}' (mod '{operation.OwnerModId}', signature '{operation.Signature}'): {ex.Message}");
                }

            logger.Info(
                $"[AutoRegister] Processed assembly '{assembly.GetName().Name}': {operations.Count} operation(s), {succeeded} succeeded, {failed} failed.");
        }

        private static IEnumerable<AutoRegistrationOperation> BuildOperations(Type type,
            IReadOnlyDictionary<string, Assembly> modAssembliesByManifestId)
        {
            var ownerModId = ResolveOwnerModId(type, modAssembliesByManifestId);
            if (ownerModId == null)
                return [];

            var contentRegistry = ModContentRegistry.For(ownerModId);
            var keywordRegistry = ModKeywordRegistry.For(ownerModId);
            var timelineRegistry = RitsuLibFramework.GetTimelineRegistry(ownerModId);
            var unlockRegistry = RitsuLibFramework.GetUnlockRegistry(ownerModId);
            var packContext = new ModContentPackContext(ownerModId, contentRegistry, keywordRegistry, timelineRegistry,
                unlockRegistry);
            var operations = new List<AutoRegistrationOperation>();
            var signatures = new HashSet<string>(StringComparer.Ordinal);

            foreach (var attribute in type.GetCustomAttributes(false).OfType<Attribute>())
                Append(attribute, false);

            foreach (var attribute in EnumerateInheritedRegistrationAttributes(type))
                Append(attribute, true);

            return operations;

            void Append(Attribute attribute, bool inherited)
            {
                switch (attribute)
                {
                    case RegisterCardAttribute registerCard:
                        RegisterCase($"RegisterCard:{registerCard.PoolType.FullName}->{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerCard.Order,
                                $"RegisterCard:{registerCard.PoolType.FullName}->{type.FullName}",
                                nameof(RegisterCardAttribute),
                                () => contentRegistry.RegisterCard(registerCard.PoolType, type,
                                    ResolvePublicEntryOptions(registerCard)),
                                [TypeDependencyKey(type)]));
                        });
                        break;
                    case RegisterRelicAttribute registerRelic:
                        RegisterCase($"RegisterRelic:{registerRelic.PoolType.FullName}->{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerRelic.Order,
                                $"RegisterRelic:{registerRelic.PoolType.FullName}->{type.FullName}",
                                nameof(RegisterRelicAttribute),
                                () => contentRegistry.RegisterRelic(registerRelic.PoolType, type,
                                    ResolvePublicEntryOptions(registerRelic)),
                                [TypeDependencyKey(type)]));
                        });
                        break;
                    case RegisterPotionAttribute registerPotion:
                        RegisterCase($"RegisterPotion:{registerPotion.PoolType.FullName}->{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerPotion.Order,
                                $"RegisterPotion:{registerPotion.PoolType.FullName}->{type.FullName}",
                                nameof(RegisterPotionAttribute),
                                () => contentRegistry.RegisterPotion(registerPotion.PoolType, type,
                                    ResolvePublicEntryOptions(registerPotion)),
                                [TypeDependencyKey(type)]));
                        });
                        break;
                    case RegisterCharacterAttribute registerCharacter:
                        RegisterCase($"RegisterCharacter:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerCharacter.Order,
                                $"RegisterCharacter:{type.FullName}", nameof(RegisterCharacterAttribute),
                                () => contentRegistry.RegisterCharacter(type),
                                providedKeys: [$"RegisterCharacter:{type.FullName}"]));
                        });
                        break;
                    case RegisterActAttribute registerAct:
                        RegisterCase($"RegisterAct:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerAct.Order,
                                $"RegisterAct:{type.FullName}", nameof(RegisterActAttribute),
                                () => contentRegistry.RegisterAct(type),
                                providedKeys: [$"RegisterAct:{type.FullName}"]));
                        });
                        break;
                    case RegisterMonsterAttribute registerMonster:
                        RegisterCase($"RegisterMonster:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerMonster.Order,
                                $"RegisterMonster:{type.FullName}", nameof(RegisterMonsterAttribute),
                                () => contentRegistry.RegisterMonster(type)));
                        });
                        break;
                    case RegisterPowerAttribute registerPower:
                        RegisterCase($"RegisterPower:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerPower.Order,
                                $"RegisterPower:{type.FullName}", nameof(RegisterPowerAttribute),
                                () => contentRegistry.RegisterPower(type)));
                        });
                        break;
                    case RegisterOrbAttribute registerOrb:
                        RegisterCase($"RegisterOrb:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerOrb.Order,
                                $"RegisterOrb:{type.FullName}", nameof(RegisterOrbAttribute),
                                () => contentRegistry.RegisterOrb(type)));
                        });
                        break;
                    case RegisterEnchantmentAttribute registerEnchantment:
                        RegisterCase($"RegisterEnchantment:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerEnchantment.Order,
                                $"RegisterEnchantment:{type.FullName}", nameof(RegisterEnchantmentAttribute),
                                () => contentRegistry.RegisterEnchantment(type)));
                        });
                        break;
                    case RegisterAfflictionAttribute registerAffliction:
                        RegisterCase($"RegisterAffliction:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerAffliction.Order,
                                $"RegisterAffliction:{type.FullName}", nameof(RegisterAfflictionAttribute),
                                () => contentRegistry.RegisterAffliction(type)));
                        });
                        break;
                    case RegisterAchievementAttribute registerAchievement:
                        RegisterCase($"RegisterAchievement:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerAchievement.Order,
                                $"RegisterAchievement:{type.FullName}", nameof(RegisterAchievementAttribute),
                                () => contentRegistry.RegisterAchievement(type)));
                        });
                        break;
                    case RegisterSingletonAttribute registerSingleton:
                        RegisterCase($"RegisterSingleton:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerSingleton.Order,
                                $"RegisterSingleton:{type.FullName}", nameof(RegisterSingletonAttribute),
                                () => contentRegistry.RegisterSingleton(type)));
                        });
                        break;
                    case RegisterGoodModifierAttribute registerGoodModifier:
                        RegisterCase($"RegisterGoodModifier:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerGoodModifier.Order,
                                $"RegisterGoodModifier:{type.FullName}", nameof(RegisterGoodModifierAttribute),
                                () => contentRegistry.RegisterGoodModifier(type)));
                        });
                        break;
                    case RegisterBadModifierAttribute registerBadModifier:
                        RegisterCase($"RegisterBadModifier:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerBadModifier.Order,
                                $"RegisterBadModifier:{type.FullName}", nameof(RegisterBadModifierAttribute),
                                () => contentRegistry.RegisterBadModifier(type)));
                        });
                        break;
                    case RegisterSharedCardPoolAttribute registerSharedCardPool:
                        RegisterCase($"RegisterSharedCardPool:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerSharedCardPool.Order,
                                $"RegisterSharedCardPool:{type.FullName}", nameof(RegisterSharedCardPoolAttribute),
                                () => contentRegistry.RegisterSharedCardPool(type),
                                providedKeys: [$"RegisterSharedCardPool:{type.FullName}"]));
                        });
                        break;
                    case RegisterSharedRelicPoolAttribute registerSharedRelicPool:
                        RegisterCase($"RegisterSharedRelicPool:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerSharedRelicPool.Order,
                                $"RegisterSharedRelicPool:{type.FullName}", nameof(RegisterSharedRelicPoolAttribute),
                                () => contentRegistry.RegisterSharedRelicPool(type),
                                providedKeys: [$"RegisterSharedRelicPool:{type.FullName}"]));
                        });
                        break;
                    case RegisterSharedPotionPoolAttribute registerSharedPotionPool:
                        RegisterCase($"RegisterSharedPotionPool:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerSharedPotionPool.Order,
                                $"RegisterSharedPotionPool:{type.FullName}", nameof(RegisterSharedPotionPoolAttribute),
                                () => contentRegistry.RegisterSharedPotionPool(type)));
                        });
                        break;
                    case RegisterSharedEventAttribute registerSharedEvent:
                        RegisterCase($"RegisterSharedEvent:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerSharedEvent.Order,
                                $"RegisterSharedEvent:{type.FullName}", nameof(RegisterSharedEventAttribute),
                                () => contentRegistry.RegisterSharedEvent(type)));
                        });
                        break;
                    case RegisterSharedAncientAttribute registerSharedAncient:
                        RegisterCase($"RegisterSharedAncient:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerSharedAncient.Order,
                                $"RegisterSharedAncient:{type.FullName}", nameof(RegisterSharedAncientAttribute),
                                () => contentRegistry.RegisterSharedAncient(type)));
                        });
                        break;
                    case RegisterGlobalEncounterAttribute registerGlobalEncounter:
                        RegisterCase($"RegisterGlobalEncounter:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentPrimary,
                                registerGlobalEncounter.Order,
                                $"RegisterGlobalEncounter:{type.FullName}", nameof(RegisterGlobalEncounterAttribute),
                                () => contentRegistry.RegisterGlobalEncounter(type)));
                        });
                        break;
                    case RegisterCharacterStarterCardAttribute starterCard:
                        RegisterCase(
                            $"RegisterCharacterStarterCard:{starterCard.CharacterType.FullName}->{type.FullName}:x{starterCard.Count}",
                            () =>
                            {
                                EnsurePositive(starterCard.Count, nameof(starterCard.Count));
                                EnsureConcreteSubtype(starterCard.CharacterType, typeof(CharacterModel),
                                    nameof(starterCard.CharacterType));
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentSecondary,
                                    starterCard.Order,
                                    $"RegisterCharacterStarterCard:{starterCard.CharacterType.FullName}->{type.FullName}:x{starterCard.Count}",
                                    nameof(RegisterCharacterStarterCardAttribute),
                                    () => contentRegistry.RegisterCharacterStarterCard(starterCard.CharacterType, type,
                                        starterCard.Count),
                                    [
                                        $"RegisterCharacter:{starterCard.CharacterType.FullName}",
                                        TypeDependencyKey(type),
                                    ]));
                            });
                        break;
                    case RegisterCharacterStarterRelicAttribute starterRelic:
                        RegisterCase(
                            $"RegisterCharacterStarterRelic:{starterRelic.CharacterType.FullName}->{type.FullName}:x{starterRelic.Count}",
                            () =>
                            {
                                EnsurePositive(starterRelic.Count, nameof(starterRelic.Count));
                                EnsureConcreteSubtype(starterRelic.CharacterType, typeof(CharacterModel),
                                    nameof(starterRelic.CharacterType));
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentSecondary,
                                    starterRelic.Order,
                                    $"RegisterCharacterStarterRelic:{starterRelic.CharacterType.FullName}->{type.FullName}:x{starterRelic.Count}",
                                    nameof(RegisterCharacterStarterRelicAttribute),
                                    () => contentRegistry.RegisterCharacterStarterRelic(starterRelic.CharacterType,
                                        type,
                                        starterRelic.Count),
                                    [
                                        $"RegisterCharacter:{starterRelic.CharacterType.FullName}",
                                        TypeDependencyKey(type),
                                    ]));
                            });
                        break;
                    case RegisterCharacterStarterPotionAttribute starterPotion:
                        RegisterCase(
                            $"RegisterCharacterStarterPotion:{starterPotion.CharacterType.FullName}->{type.FullName}:x{starterPotion.Count}",
                            () =>
                            {
                                EnsurePositive(starterPotion.Count, nameof(starterPotion.Count));
                                EnsureConcreteSubtype(starterPotion.CharacterType, typeof(CharacterModel),
                                    nameof(starterPotion.CharacterType));
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentSecondary,
                                    starterPotion.Order,
                                    $"RegisterCharacterStarterPotion:{starterPotion.CharacterType.FullName}->{type.FullName}:x{starterPotion.Count}",
                                    nameof(RegisterCharacterStarterPotionAttribute),
                                    () => contentRegistry.RegisterCharacterStarterPotion(starterPotion.CharacterType,
                                        type,
                                        starterPotion.Count),
                                    [
                                        $"RegisterCharacter:{starterPotion.CharacterType.FullName}",
                                        TypeDependencyKey(type),
                                    ]));
                            });
                        break;
                    case RegisterActEncounterAttribute actEncounter:
                        RegisterCase($"RegisterActEncounter:{actEncounter.ActType.FullName}->{type.FullName}", () =>
                        {
                            EnsureConcreteSubtype(actEncounter.ActType, typeof(ActModel), nameof(actEncounter.ActType));
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentSecondary,
                                actEncounter.Order,
                                $"RegisterActEncounter:{actEncounter.ActType.FullName}->{type.FullName}",
                                nameof(RegisterActEncounterAttribute),
                                () => contentRegistry.RegisterActEncounter(actEncounter.ActType, type),
                                [
                                    $"RegisterAct:{actEncounter.ActType.FullName}",
                                    TypeDependencyKey(type),
                                ]));
                        });
                        break;
                    case RegisterActEventAttribute actEvent:
                        RegisterCase($"RegisterActEvent:{actEvent.ActType.FullName}->{type.FullName}", () =>
                        {
                            EnsureConcreteSubtype(actEvent.ActType, typeof(ActModel), nameof(actEvent.ActType));
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentSecondary,
                                actEvent.Order,
                                $"RegisterActEvent:{actEvent.ActType.FullName}->{type.FullName}",
                                nameof(RegisterActEventAttribute),
                                () => contentRegistry.RegisterActEvent(actEvent.ActType, type),
                                [
                                    $"RegisterAct:{actEvent.ActType.FullName}",
                                    TypeDependencyKey(type),
                                ]));
                        });
                        break;
                    case RegisterActAncientAttribute actAncient:
                        RegisterCase($"RegisterActAncient:{actAncient.ActType.FullName}->{type.FullName}", () =>
                        {
                            EnsureConcreteSubtype(actAncient.ActType, typeof(ActModel), nameof(actAncient.ActType));
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.ContentSecondary,
                                actAncient.Order,
                                $"RegisterActAncient:{actAncient.ActType.FullName}->{type.FullName}",
                                nameof(RegisterActAncientAttribute),
                                () => contentRegistry.RegisterActAncient(actAncient.ActType, type),
                                [
                                    $"RegisterAct:{actAncient.ActType.FullName}",
                                    TypeDependencyKey(type),
                                ]));
                        });
                        break;
                    case RegisterOwnedKeywordAttribute ownedKeyword:
                        RegisterCase($"RegisterOwnedKeyword:{ownedKeyword.LocalKeywordStem}:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.Keywords,
                                ownedKeyword.Order,
                                $"RegisterOwnedKeyword:{ownedKeyword.LocalKeywordStem}:{type.FullName}",
                                nameof(RegisterOwnedKeywordAttribute),
                                () => keywordRegistry.RegisterOwned(
                                    ValidateNonEmpty(ownedKeyword.LocalKeywordStem,
                                        nameof(ownedKeyword.LocalKeywordStem)),
                                    ownedKeyword.TitleTable,
                                    ownedKeyword.TitleKey,
                                    ownedKeyword.DescriptionTable,
                                    ownedKeyword.DescriptionKey,
                                    ownedKeyword.IconPath,
                                    ownedKeyword.CardDescriptionPlacement,
                                    ownedKeyword.IncludeInCardHoverTip)));
                        });
                        break;
                    case RegisterOwnedCardKeywordAttribute ownedCardKeyword:
                        RegisterCase($"RegisterOwnedCardKeyword:{ownedCardKeyword.LocalKeywordStem}:{type.FullName}",
                            () =>
                            {
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.Keywords,
                                    ownedCardKeyword.Order,
                                    $"RegisterOwnedCardKeyword:{ownedCardKeyword.LocalKeywordStem}:{type.FullName}",
                                    nameof(RegisterOwnedCardKeywordAttribute),
                                    () => keywordRegistry.RegisterCardKeywordOwned(
                                        ValidateNonEmpty(ownedCardKeyword.LocalKeywordStem,
                                            nameof(ownedCardKeyword.LocalKeywordStem)),
                                        ownedCardKeyword.LocKeyPrefix,
                                        ownedCardKeyword.IconPath,
                                        ownedCardKeyword.CardDescriptionPlacement,
                                        ownedCardKeyword.IncludeInCardHoverTip)));
                            });
                        break;
                    case AutoTimelineSlotAttribute autoTimelineSlot:
                        RegisterCase($"AutoTimelineSlot:{type.FullName}@{autoTimelineSlot.Era}", () =>
                        {
                            EnsureConcreteSubtype(type, typeof(ModEpochTemplate), nameof(type));
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.TimelineLayout,
                                autoTimelineSlot.Order,
                                $"AutoTimelineSlot:{type.FullName}@{autoTimelineSlot.Era}",
                                nameof(AutoTimelineSlotAttribute),
                                () => ModTimelineLayoutRegistry.RegisterAutoTimelineSlot(type, autoTimelineSlot.Era,
                                    ownerModId),
                                [TypeDependencyKey(type)]));
                        });
                        break;
                    case AutoTimelineSlotBeforeColumnAttribute autoBeforeColumn:
                        RegisterCase($"AutoTimelineSlotBeforeColumn:{type.FullName}<{autoBeforeColumn.AnchorEra}", () =>
                        {
                            EnsureConcreteSubtype(type, typeof(ModEpochTemplate), nameof(type));
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.TimelineLayout,
                                autoBeforeColumn.Order,
                                $"AutoTimelineSlotBeforeColumn:{type.FullName}<{autoBeforeColumn.AnchorEra}",
                                nameof(AutoTimelineSlotBeforeColumnAttribute),
                                () => ModTimelineLayoutRegistry.RegisterAutoTimelineSlotBeforeEraColumn(type,
                                    autoBeforeColumn.AnchorEra, ownerModId),
                                [TypeDependencyKey(type)]));
                        });
                        break;
                    case AutoTimelineSlotBeforeEpochColumnAttribute autoBeforeEpochColumn:
                        RegisterCase(
                            $"AutoTimelineSlotBeforeEpochColumn:{type.FullName}<{autoBeforeEpochColumn.ReferenceEpochType.FullName}",
                            () =>
                            {
                                EnsureConcreteSubtype(type, typeof(ModEpochTemplate), nameof(type));
                                EnsureConcreteSubtype(autoBeforeEpochColumn.ReferenceEpochType, typeof(EpochModel),
                                    nameof(autoBeforeEpochColumn.ReferenceEpochType));
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.TimelineLayout,
                                    autoBeforeEpochColumn.Order,
                                    $"AutoTimelineSlotBeforeEpochColumn:{type.FullName}<{autoBeforeEpochColumn.ReferenceEpochType.FullName}",
                                    nameof(AutoTimelineSlotBeforeEpochColumnAttribute),
                                    () => ModTimelineLayoutRegistry.RegisterAutoTimelineSlotBeforeEpochColumn(type,
                                        autoBeforeEpochColumn.ReferenceEpochType, ownerModId),
                                    [TypeDependencyKey(autoBeforeEpochColumn.ReferenceEpochType)]));
                            });
                        break;
                    case AutoTimelineSlotAfterColumnAttribute autoAfterColumn:
                        RegisterCase($"AutoTimelineSlotAfterColumn:{type.FullName}>{autoAfterColumn.AnchorEra}", () =>
                        {
                            EnsureConcreteSubtype(type, typeof(ModEpochTemplate), nameof(type));
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.TimelineLayout,
                                autoAfterColumn.Order,
                                $"AutoTimelineSlotAfterColumn:{type.FullName}>{autoAfterColumn.AnchorEra}",
                                nameof(AutoTimelineSlotAfterColumnAttribute),
                                () => ModTimelineLayoutRegistry.RegisterAutoTimelineSlotAfterEraColumn(type,
                                    autoAfterColumn.AnchorEra, ownerModId),
                                [TypeDependencyKey(type)]));
                        });
                        break;
                    case AutoTimelineSlotAfterEpochColumnAttribute autoAfterEpochColumn:
                        RegisterCase(
                            $"AutoTimelineSlotAfterEpochColumn:{type.FullName}>{autoAfterEpochColumn.ReferenceEpochType.FullName}",
                            () =>
                            {
                                EnsureConcreteSubtype(type, typeof(ModEpochTemplate), nameof(type));
                                EnsureConcreteSubtype(autoAfterEpochColumn.ReferenceEpochType, typeof(EpochModel),
                                    nameof(autoAfterEpochColumn.ReferenceEpochType));
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.TimelineLayout,
                                    autoAfterEpochColumn.Order,
                                    $"AutoTimelineSlotAfterEpochColumn:{type.FullName}>{autoAfterEpochColumn.ReferenceEpochType.FullName}",
                                    nameof(AutoTimelineSlotAfterEpochColumnAttribute),
                                    () => ModTimelineLayoutRegistry.RegisterAutoTimelineSlotAfterEpochColumn(type,
                                        autoAfterEpochColumn.ReferenceEpochType, ownerModId),
                                    [TypeDependencyKey(autoAfterEpochColumn.ReferenceEpochType)]));
                            });
                        break;
                    case AutoTimelineSlotInColumnAttribute autoInColumn:
                        RegisterCase($"AutoTimelineSlotInColumn:{type.FullName}={autoInColumn.AnchorEra}", () =>
                        {
                            EnsureConcreteSubtype(type, typeof(ModEpochTemplate), nameof(type));
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.TimelineLayout,
                                autoInColumn.Order,
                                $"AutoTimelineSlotInColumn:{type.FullName}={autoInColumn.AnchorEra}",
                                nameof(AutoTimelineSlotInColumnAttribute),
                                () => ModTimelineLayoutRegistry.RegisterAutoTimelineSlotInEraColumn(type,
                                    autoInColumn.AnchorEra, ownerModId),
                                [TypeDependencyKey(type)]));
                        });
                        break;
                    case AutoTimelineSlotInEpochColumnAttribute autoInEpochColumn:
                        RegisterCase(
                            $"AutoTimelineSlotInEpochColumn:{type.FullName}={autoInEpochColumn.ReferenceEpochType.FullName}",
                            () =>
                            {
                                EnsureConcreteSubtype(type, typeof(ModEpochTemplate), nameof(type));
                                EnsureConcreteSubtype(autoInEpochColumn.ReferenceEpochType, typeof(EpochModel),
                                    nameof(autoInEpochColumn.ReferenceEpochType));
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.TimelineLayout,
                                    autoInEpochColumn.Order,
                                    $"AutoTimelineSlotInEpochColumn:{type.FullName}={autoInEpochColumn.ReferenceEpochType.FullName}",
                                    nameof(AutoTimelineSlotInEpochColumnAttribute),
                                    () => ModTimelineLayoutRegistry.RegisterAutoTimelineSlotInEpochColumn(type,
                                        autoInEpochColumn.ReferenceEpochType, ownerModId),
                                    [TypeDependencyKey(autoInEpochColumn.ReferenceEpochType)]));
                            });
                        break;
                    case RegisterArchaicToothTranscendenceAttribute archaicTooth:
                        RegisterCase(
                            $"RegisterArchaicToothTranscendence:{type.FullName}->{archaicTooth.AncientCardType.FullName}",
                            () =>
                            {
                                EnsureConcreteSubtype(type, typeof(CardModel), nameof(type));
                                EnsureConcreteSubtype(archaicTooth.AncientCardType, typeof(CardModel),
                                    nameof(archaicTooth.AncientCardType));
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.AncientMappings,
                                    archaicTooth.Order,
                                    $"RegisterArchaicToothTranscendence:{type.FullName}->{archaicTooth.AncientCardType.FullName}",
                                    nameof(RegisterArchaicToothTranscendenceAttribute),
                                    () => RitsuLibFramework.RegisterArchaicToothTranscendenceMapping(
                                        ModelDb.GetId(type),
                                        archaicTooth.AncientCardType,
                                        ownerModId),
                                    [TypeDependencyKey(type), TypeDependencyKey(archaicTooth.AncientCardType)]));
                            });
                        break;
                    case RegisterTouchOfOrobasRefinementAttribute touchOfOrobas:
                        RegisterCase(
                            $"RegisterTouchOfOrobasRefinement:{type.FullName}->{touchOfOrobas.UpgradedRelicType.FullName}",
                            () =>
                            {
                                EnsureConcreteSubtype(type, typeof(RelicModel), nameof(type));
                                EnsureConcreteSubtype(touchOfOrobas.UpgradedRelicType, typeof(RelicModel),
                                    nameof(touchOfOrobas.UpgradedRelicType));
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.AncientMappings,
                                    touchOfOrobas.Order,
                                    $"RegisterTouchOfOrobasRefinement:{type.FullName}->{touchOfOrobas.UpgradedRelicType.FullName}",
                                    nameof(RegisterTouchOfOrobasRefinementAttribute),
                                    () => RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping(
                                        ModelDb.GetId(type),
                                        touchOfOrobas.UpgradedRelicType,
                                        ownerModId),
                                    [TypeDependencyKey(type), TypeDependencyKey(touchOfOrobas.UpgradedRelicType)]));
                            });
                        break;
                    case RegisterEpochCardsAttribute registerEpochCards:
                    {
                        EnsureConcreteSubtype(type, typeof(EpochModel), nameof(type));
                        var cardTypes = ValidateTypeList(registerEpochCards.CardTypes,
                            nameof(registerEpochCards.CardTypes), typeof(CardModel));
                        var epochCardsSignature =
                            $"RegisterEpochCards:{type.FullName}:{string.Join(",", cardTypes.Select(static t => t.FullName))}";
                        if (inherited && signatures.Contains(epochCardsSignature))
                            break;

                        operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.TimelineLayout,
                            registerEpochCards.Order,
                            epochCardsSignature,
                            nameof(RegisterEpochCardsAttribute),
                            () => ModEpochGatedContentPackHelper.ApplyExplicitTypes(type, packContext, cardTypes,
                                []),
                            cardTypes.Select(TypeDependencyKey).ToArray()));
                        signatures.Add(epochCardsSignature);
                        break;
                    }
                    case RequireAllCardsInPoolAttribute requireAllCardsInPool:
                        RegisterCase($"RequireAllCardsInPool:{type.FullName}:{requireAllCardsInPool.PoolType.FullName}",
                            () =>
                            {
                                EnsureConcreteSubtype(type, typeof(EpochModel), nameof(type));
                                EnsureConcreteSubtype(requireAllCardsInPool.PoolType, typeof(CardPoolModel),
                                    nameof(requireAllCardsInPool.PoolType));
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.TimelineLayout,
                                    requireAllCardsInPool.Order,
                                    $"RequireAllCardsInPool:{type.FullName}:{requireAllCardsInPool.PoolType.FullName}",
                                    nameof(RequireAllCardsInPoolAttribute),
                                    () => ModEpochGatedContentPackHelper.ApplyRequireAllPoolCards(type,
                                        requireAllCardsInPool.PoolType,
                                        packContext),
                                    [$"RegisterSharedCardPool:{requireAllCardsInPool.PoolType.FullName}"]));
                            });
                        break;
                    case RegisterEpochRelicsFromPoolAttribute registerEpochRelicsFromPool:
                        RegisterCase(
                            $"RegisterEpochRelicsFromPool:{type.FullName}:{registerEpochRelicsFromPool.PoolType.FullName}",
                            () =>
                            {
                                EnsureConcreteSubtype(type, typeof(EpochModel), nameof(type));
                                EnsureConcreteSubtype(registerEpochRelicsFromPool.PoolType,
                                    typeof(RelicPoolModel),
                                    nameof(registerEpochRelicsFromPool.PoolType));
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.TimelineLayout,
                                    registerEpochRelicsFromPool.Order,
                                    $"RegisterEpochRelicsFromPool:{type.FullName}:{registerEpochRelicsFromPool.PoolType.FullName}",
                                    nameof(RegisterEpochRelicsFromPoolAttribute),
                                    () => ModEpochGatedContentPackHelper.ApplyRelicsFromPool(type,
                                        registerEpochRelicsFromPool.PoolType,
                                        packContext),
                                    [$"RegisterSharedRelicPool:{registerEpochRelicsFromPool.PoolType.FullName}"]));
                            });
                        break;
                    case RegisterEpochAttribute registerEpoch:
                        RegisterCase($"RegisterEpoch:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.Timeline,
                                registerEpoch.Order,
                                $"RegisterEpoch:{type.FullName}", nameof(RegisterEpochAttribute),
                                () => timelineRegistry.RegisterEpoch(type),
                                providedKeys: [$"RegisterEpoch:{type.FullName}"]));
                        });
                        break;
                    case RegisterStoryAttribute registerStory:
                        RegisterCase($"RegisterStory:{type.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.Timeline,
                                registerStory.Order,
                                $"RegisterStory:{type.FullName}", nameof(RegisterStoryAttribute),
                                () => timelineRegistry.RegisterStory(type),
                                providedKeys: [$"RegisterStory:{type.FullName}"]));
                        });
                        break;
                    case RegisterStoryEpochAttribute registerStoryEpoch:
                        RegisterCase($"RegisterStoryEpoch:{registerStoryEpoch.StoryType.FullName}<-{type.FullName}",
                            () =>
                            {
                                EnsureConcreteSubtype(registerStoryEpoch.StoryType, typeof(StoryModel),
                                    nameof(registerStoryEpoch.StoryType));
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.Timeline,
                                    registerStoryEpoch.Order,
                                    $"RegisterStoryEpoch:{registerStoryEpoch.StoryType.FullName}<-{type.FullName}",
                                    nameof(RegisterStoryEpochAttribute),
                                    () => timelineRegistry.RegisterStoryEpoch(registerStoryEpoch.StoryType, type),
                                    [
                                        $"RegisterStory:{registerStoryEpoch.StoryType.FullName}",
                                        $"RegisterEpoch:{type.FullName}",
                                    ]));
                            });
                        break;
                    case RequireEpochAttribute requireEpoch:
                        RegisterCase($"RequireEpoch:{type.FullName}->{requireEpoch.EpochType.FullName}", () =>
                        {
                            operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.Unlocks,
                                requireEpoch.Order,
                                $"RequireEpoch:{type.FullName}->{requireEpoch.EpochType.FullName}",
                                nameof(RequireEpochAttribute),
                                () => unlockRegistry.RequireEpoch(type, requireEpoch.EpochType),
                                [TypeDependencyKey(type), $"RegisterEpoch:{requireEpoch.EpochType.FullName}"]));
                        });
                        break;
                    case UnlockEpochAfterRunAsAttribute unlockAfterRun:
                        RegisterCase($"UnlockEpochAfterRunAs:{type.FullName}->{unlockAfterRun.EpochType.FullName}",
                            () =>
                            {
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.Unlocks,
                                    unlockAfterRun.Order,
                                    $"UnlockEpochAfterRunAs:{type.FullName}->{unlockAfterRun.EpochType.FullName}",
                                    nameof(UnlockEpochAfterRunAsAttribute),
                                    () => unlockRegistry.UnlockEpochAfterRunAs(type, unlockAfterRun.EpochType),
                                    [TypeDependencyKey(type), $"RegisterEpoch:{unlockAfterRun.EpochType.FullName}"]));
                            });
                        break;
                    case UnlockEpochAfterWinAsAttribute unlockAfterWin:
                        RegisterCase($"UnlockEpochAfterWinAs:{type.FullName}->{unlockAfterWin.EpochType.FullName}",
                            () =>
                            {
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.Unlocks,
                                    unlockAfterWin.Order,
                                    $"UnlockEpochAfterWinAs:{type.FullName}->{unlockAfterWin.EpochType.FullName}",
                                    nameof(UnlockEpochAfterWinAsAttribute),
                                    () => unlockRegistry.UnlockEpochAfterWinAs(type, unlockAfterWin.EpochType),
                                    [TypeDependencyKey(type), $"RegisterEpoch:{unlockAfterWin.EpochType.FullName}"]));
                            });
                        break;
                    case UnlockEpochAfterAscensionWinAttribute unlockAfterAscensionWin:
                        RegisterCase(
                            $"UnlockEpochAfterAscensionWin:{type.FullName}->{unlockAfterAscensionWin.EpochType.FullName}:A{unlockAfterAscensionWin.AscensionLevel}",
                            () =>
                            {
                                EnsurePositive(unlockAfterAscensionWin.AscensionLevel,
                                    nameof(unlockAfterAscensionWin.AscensionLevel));
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.Unlocks,
                                    unlockAfterAscensionWin.Order,
                                    $"UnlockEpochAfterAscensionWin:{type.FullName}->{unlockAfterAscensionWin.EpochType.FullName}:A{unlockAfterAscensionWin.AscensionLevel}",
                                    nameof(UnlockEpochAfterAscensionWinAttribute),
                                    () => unlockRegistry.UnlockEpochAfterAscensionWin(type,
                                        unlockAfterAscensionWin.EpochType,
                                        unlockAfterAscensionWin.AscensionLevel),
                                    [
                                        TypeDependencyKey(type),
                                        $"RegisterEpoch:{unlockAfterAscensionWin.EpochType.FullName}",
                                    ]));
                            });
                        break;
                    case UnlockEpochAfterEliteVictoriesAttribute unlockAfterEliteVictories:
                        RegisterCase(
                            $"UnlockEpochAfterEliteVictories:{type.FullName}->{unlockAfterEliteVictories.EpochType.FullName}:{unlockAfterEliteVictories.RequiredEliteWins}",
                            () =>
                            {
                                EnsurePositive(unlockAfterEliteVictories.RequiredEliteWins,
                                    nameof(unlockAfterEliteVictories.RequiredEliteWins));
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.Unlocks,
                                    unlockAfterEliteVictories.Order,
                                    $"UnlockEpochAfterEliteVictories:{type.FullName}->{unlockAfterEliteVictories.EpochType.FullName}:{unlockAfterEliteVictories.RequiredEliteWins}",
                                    nameof(UnlockEpochAfterEliteVictoriesAttribute),
                                    () => unlockRegistry.UnlockEpochAfterEliteVictories(type,
                                        unlockAfterEliteVictories.EpochType,
                                        unlockAfterEliteVictories.RequiredEliteWins),
                                    [
                                        TypeDependencyKey(type),
                                        $"RegisterEpoch:{unlockAfterEliteVictories.EpochType.FullName}",
                                    ]));
                            });
                        break;
                    case UnlockEpochAfterBossVictoriesAttribute unlockAfterBossVictories:
                        RegisterCase(
                            $"UnlockEpochAfterBossVictories:{type.FullName}->{unlockAfterBossVictories.EpochType.FullName}:{unlockAfterBossVictories.RequiredBossWins}",
                            () =>
                            {
                                EnsurePositive(unlockAfterBossVictories.RequiredBossWins,
                                    nameof(unlockAfterBossVictories.RequiredBossWins));
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.Unlocks,
                                    unlockAfterBossVictories.Order,
                                    $"UnlockEpochAfterBossVictories:{type.FullName}->{unlockAfterBossVictories.EpochType.FullName}:{unlockAfterBossVictories.RequiredBossWins}",
                                    nameof(UnlockEpochAfterBossVictoriesAttribute),
                                    () => unlockRegistry.UnlockEpochAfterBossVictories(type,
                                        unlockAfterBossVictories.EpochType,
                                        unlockAfterBossVictories.RequiredBossWins),
                                    [
                                        TypeDependencyKey(type),
                                        $"RegisterEpoch:{unlockAfterBossVictories.EpochType.FullName}",
                                    ]));
                            });
                        break;
                    case UnlockEpochAfterAscensionOneWinAttribute unlockAfterAscensionOne:
                        RegisterCase(
                            $"UnlockEpochAfterAscensionOneWin:{type.FullName}->{unlockAfterAscensionOne.EpochType.FullName}",
                            () =>
                            {
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.Unlocks,
                                    unlockAfterAscensionOne.Order,
                                    $"UnlockEpochAfterAscensionOneWin:{type.FullName}->{unlockAfterAscensionOne.EpochType.FullName}",
                                    nameof(UnlockEpochAfterAscensionOneWinAttribute),
                                    () => unlockRegistry.UnlockEpochAfterAscensionOneWin(type,
                                        unlockAfterAscensionOne.EpochType),
                                    [
                                        TypeDependencyKey(type),
                                        $"RegisterEpoch:{unlockAfterAscensionOne.EpochType.FullName}",
                                    ]));
                            });
                        break;
                    case RevealAscensionAfterEpochAttribute revealAscension:
                        RegisterCase($"RevealAscensionAfterEpoch:{type.FullName}->{revealAscension.EpochType.FullName}",
                            () =>
                            {
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.Unlocks,
                                    revealAscension.Order,
                                    $"RevealAscensionAfterEpoch:{type.FullName}->{revealAscension.EpochType.FullName}",
                                    nameof(RevealAscensionAfterEpochAttribute),
                                    () => unlockRegistry.RevealAscensionAfterEpoch(type, revealAscension.EpochType),
                                    [TypeDependencyKey(type), $"RegisterEpoch:{revealAscension.EpochType.FullName}"]));
                            });
                        break;
                    case UnlockCharacterAfterRunAsAttribute unlockCharacterAfterRun:
                        RegisterCase(
                            $"UnlockCharacterAfterRunAs:{type.FullName}->{unlockCharacterAfterRun.EpochType.FullName}",
                            () =>
                            {
                                operations.Add(CreateOperation(ownerModId, type, AutoRegistrationPhase.Unlocks,
                                    unlockCharacterAfterRun.Order,
                                    $"UnlockCharacterAfterRunAs:{type.FullName}->{unlockCharacterAfterRun.EpochType.FullName}",
                                    nameof(UnlockCharacterAfterRunAsAttribute),
                                    () => unlockRegistry.UnlockCharacterAfterRunAs(type,
                                        unlockCharacterAfterRun.EpochType),
                                    [
                                        TypeDependencyKey(type),
                                        $"RegisterEpoch:{unlockCharacterAfterRun.EpochType.FullName}",
                                    ]));
                            });
                        break;
                }

                return;

                void RegisterCase(string signature, Action addOperation)
                {
                    if (inherited && signatures.Contains(signature))
                        return;

                    addOperation();
                    signatures.Add(signature);
                }
            }
        }

        private static IEnumerable<Attribute> EnumerateInheritedRegistrationAttributes(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
                foreach (var attribute in baseType.GetCustomAttributes(false).OfType<Attribute>())
                    if (attribute is AutoRegistrationAttribute { Inherit: true })
                        yield return attribute;
        }

        private static AutoRegistrationOperation CreateOperation(string ownerModId, Type sourceType,
            AutoRegistrationPhase phase, int order, string signature, string attributeName, Action execute,
            IReadOnlyList<string>? dependencies = null, IReadOnlyList<string>? providedKeys = null)
        {
            return new(ownerModId, sourceType.Assembly, sourceType, phase, order, signature, attributeName, execute,
                dependencies, providedKeys);
        }

        private static IReadOnlyList<AutoRegistrationOperation> OrderOperations(
            IReadOnlyList<AutoRegistrationOperation> operations)
        {
            var ordered = operations.OrderBy(static op => op, AutoRegistrationOperationComparer.Instance).ToList();
            var providerIndexByKey = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < ordered.Count; i++)
            {
                var operation = ordered[i];
                providerIndexByKey[operation.Signature] = i;
                var providedKeys = operation.ProvidedKeys;
                if (providedKeys == null)
                    continue;

                foreach (var providedKey in providedKeys)
                    providerIndexByKey[providedKey] = i;
            }

            var adjacency = new List<int>[ordered.Count];
            var indegree = new int[ordered.Count];
            for (var i = 0; i < ordered.Count; i++)
                adjacency[i] = [];

            for (var i = 0; i < ordered.Count; i++)
            {
                var operation = ordered[i];
                if (operation.Dependencies == null || operation.Dependencies.Count == 0)
                    continue;

                foreach (var dependency in operation.Dependencies)
                {
                    if (!providerIndexByKey.TryGetValue(dependency, out var dependencyIndex) || dependencyIndex == i)
                        continue;

                    adjacency[dependencyIndex].Add(i);
                    indegree[i]++;
                }
            }

            var ready = new PriorityQueue<int, OperationPriority>();
            for (var i = 0; i < ordered.Count; i++)
                if (indegree[i] == 0)
                    ready.Enqueue(i, new(ordered[i]));

            var result = new List<AutoRegistrationOperation>(ordered.Count);
            while (ready.Count > 0)
            {
                var index = ready.Dequeue();
                result.Add(ordered[index]);
                foreach (var next in adjacency[index])
                {
                    indegree[next]--;
                    if (indegree[next] == 0)
                        ready.Enqueue(next, new(ordered[next]));
                }
            }

            if (result.Count != ordered.Count)
                RitsuLibFramework.Logger.Warn(
                    $"[AutoRegister] Dependency cycle detected among {ordered.Count} operation(s); falling back to stable sort.");

            return result.Count == ordered.Count ? result : ordered;
        }

        private static string TypeDependencyKey(Type type)
        {
            return $"RegisterType:{type.AssemblyQualifiedName}";
        }

        private static string? ResolveOwnerModId(Type type,
            IReadOnlyDictionary<string, Assembly> modAssembliesByManifestId)
        {
            var typeOverride = type.GetCustomAttribute<RitsuLibOwnedByAttribute>(false);
            if (typeOverride != null)
                return typeOverride.ModId;

            foreach (var pair in modAssembliesByManifestId)
                if (pair.Value == type.Assembly)
                    return pair.Key;

            RitsuLibFramework.Logger.Warn(
                $"[AutoRegister] Skipping '{type.FullName}' because no owning mod id could be resolved. Register the assembly through ModTypeDiscoveryHub.RegisterModAssembly(...) or add [RitsuLibOwnedBy(\"...\")].");
            return null;
        }

        private static ModelPublicEntryOptions ResolvePublicEntryOptions(
            ModelPublicEntryRegistrationAttributeBase attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);
            EnsureConcreteSubtype(attribute.PoolType, typeof(AbstractModel), nameof(attribute.PoolType));

            var hasStem = !string.IsNullOrWhiteSpace(attribute.StableEntryStem);
            var hasFull = !string.IsNullOrWhiteSpace(attribute.FullPublicEntry);

            return hasStem switch
            {
                true when hasFull => throw new InvalidOperationException(
                    "StableEntryStem and FullPublicEntry cannot both be specified."),
                true => ModelPublicEntryOptions.FromStem(attribute.StableEntryStem!),
                _ => hasFull
                    ? ModelPublicEntryOptions.FromFullPublicEntry(attribute.FullPublicEntry!)
                    : ModelPublicEntryOptions.FromTypeName,
            };
        }

        private static void EnsureConcreteSubtype(Type type, Type expectedBaseType, string paramName)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(expectedBaseType);

            if (type.ContainsGenericParameters)
                throw new ArgumentException($"Type '{type.FullName}' must be closed.", paramName);

            if (type.IsAbstract || type.IsInterface || !expectedBaseType.IsAssignableFrom(type))
                throw new ArgumentException(
                    $"Type '{type.FullName}' must be a concrete subtype of '{expectedBaseType.FullName}'.",
                    paramName);
        }

        private static Type[] ValidateTypeList(IReadOnlyList<Type> types, string paramName, Type expectedBaseType)
        {
            ArgumentNullException.ThrowIfNull(types);
            ArgumentNullException.ThrowIfNull(expectedBaseType);

            if (types.Count == 0)
                throw new ArgumentException("At least one type must be provided.", paramName);

            var validated = new Type[types.Count];
            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i] ??
                           throw new ArgumentException("Type list must not contain null entries.", paramName);
                EnsureConcreteSubtype(type, expectedBaseType, paramName);
                validated[i] = type;
            }

            return validated;
        }

        private static string ValidateNonEmpty(string value, string paramName)
        {
            return string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Value must not be null or whitespace.", paramName)
                : value.Trim();
        }

        private static void EnsurePositive(int value, string paramName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(paramName, value, "Value must be positive.");
        }

        private readonly record struct OperationPriority(AutoRegistrationOperation Operation)
            : IComparable<OperationPriority>
        {
            public int CompareTo(OperationPriority other)
            {
                return AutoRegistrationOperationComparer.CompareCore(Operation, other.Operation);
            }
        }
    }
}
