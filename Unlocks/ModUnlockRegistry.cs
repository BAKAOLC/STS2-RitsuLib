using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Content;
using STS2RitsuLib.Diagnostics;
using STS2RitsuLib.Timeline;

namespace STS2RitsuLib.Unlocks
{
    /// <summary>
    ///     Per-mod registration of epoch requirements and post-run / combat-derived unlock rules integrated via
    ///     Harmony patches.
    /// </summary>
    public sealed class ModUnlockRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModUnlockRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<ModelId, string> RequiredEpochsByModelId = [];
        private static readonly List<PostRunEpochUnlockRule> PostRunRules = [];
        private static readonly Dictionary<ModelId, EliteEpochUnlockRule> EliteEpochRulesByCharacterId = [];
        private static readonly Dictionary<ModelId, CountedEpochUnlockRule> BossEpochRulesByCharacterId = [];
        private static readonly Dictionary<ModelId, string> AscensionOneEpochsByCharacterId = [];
        private static readonly Dictionary<ModelId, string> AscensionRevealEpochsByCharacterId = [];
        private static readonly Dictionary<ModelId, string> PostRunCharacterUnlockEpochsByCharacterId = [];

        private static readonly HashSet<string> ModIdsIgnoringEpochRequirements =
            new(StringComparer.OrdinalIgnoreCase);

        private string? _freezeReason;

        private ModUnlockRegistry(string modId)
        {
            ModId = modId;
        }

        /// <summary>
        ///     Owning mod identifier for this registry instance.
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     True after the framework freezes further unlock rule registration.
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     Returns the unlock registry singleton for <paramref name="modId" />.
        /// </summary>
        public static ModUnlockRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var registry))
                    return registry;

                registry = new(modId);
                Registries[modId] = registry;
                return registry;
            }
        }

        /// <summary>
        ///     When <paramref name="ignored" /> is true, models registered by <paramref name="modId" /> skip
        ///     <see cref="RequireEpoch(Type,string)" /> gating at runtime (cards/relics/characters appear as if every epoch were
        ///     met).
        ///     Ascension reveal rules tied to that character still consult this bypass via patch integration.
        /// </summary>
        public static void SetEpochRequirementsIgnoredForMod(string modId, bool ignored = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (ignored)
                    ModIdsIgnoringEpochRequirements.Add(modId);
                else
                    ModIdsIgnoringEpochRequirements.Remove(modId);
            }
        }

        internal static bool IsEpochRequirementIgnoredForModelType(Type modelType)
        {
            ArgumentNullException.ThrowIfNull(modelType);

            if (!ModContentRegistry.TryGetOwnerModId(modelType, out var owner))
                return false;

            lock (SyncRoot)
            {
                return ModIdsIgnoringEpochRequirements.Contains(owner);
            }
        }

        /// <summary>
        ///     Requires <typeparamref name="TModel" /> content to remain locked until <typeparamref name="TEpoch" />
        ///     is obtained or revealed.
        /// </summary>
        public void RequireEpoch<TModel, TEpoch>()
            where TModel : AbstractModel
            where TEpoch : EpochModel, new()
        {
            RequireEpoch(typeof(TModel), typeof(TEpoch));
        }

        /// <summary>
        ///     Requires <paramref name="modelType" /> content to remain locked until <paramref name="epochType" /> is
        ///     obtained or revealed.
        /// </summary>
        public void RequireEpoch(Type modelType, Type epochType)
        {
            RequireEpoch(modelType, ModTimelineRegistry.GetEpochId(epochType));
        }

        /// <summary>
        ///     Requires <paramref name="modelType" /> content to remain locked until <paramref name="epochId" /> is
        ///     obtained or revealed.
        /// </summary>
        public void RequireEpoch(Type modelType, string epochId)
        {
            EnsureMutable($"register unlock requirement for '{modelType.Name}'");
            ArgumentNullException.ThrowIfNull(modelType);
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);

            RegistrationConflictDetector.ThrowIfModelIdConflicts(modelType);
            var modelId = ModelDb.GetId(modelType);

            lock (SyncRoot)
            {
                RequiredEpochsByModelId[modelId] = epochId;
            }
        }

        /// <summary>
        ///     Registers a rule that obtains <typeparamref name="TEpoch" /> after any completed run as
        ///     <typeparamref name="TCharacter" />.
        /// </summary>
        public void UnlockEpochAfterRunAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            UnlockEpochAfterRunAs(typeof(TCharacter), typeof(TEpoch));
        }

        /// <summary>
        ///     Registers a rule that obtains <paramref name="epochType" /> after any completed run as
        ///     <paramref name="characterType" />.
        /// </summary>
        public void UnlockEpochAfterRunAs(Type characterType, Type epochType)
        {
            RegisterPostRunRule(
                PostRunEpochUnlockRule.Create(
                    ModTimelineRegistry.GetEpochId(epochType),
                    $"Unlock {epochType.Name} after finishing a run as {characterType.Name}",
                    context => context.CharacterId == ModelDb.GetId(characterType)));
        }

        /// <summary>
        ///     Registers a rule that obtains <typeparamref name="TEpoch" /> after a victorious run as
        ///     <typeparamref name="TCharacter" />.
        /// </summary>
        public void UnlockEpochAfterWinAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            UnlockEpochAfterWinAs(typeof(TCharacter), typeof(TEpoch));
        }

        /// <summary>
        ///     Registers a rule that obtains <paramref name="epochType" /> after a victorious run as
        ///     <paramref name="characterType" />.
        /// </summary>
        public void UnlockEpochAfterWinAs(Type characterType, Type epochType)
        {
            RegisterPostRunRule(
                PostRunEpochUnlockRule.Create(
                    ModTimelineRegistry.GetEpochId(epochType),
                    $"Unlock {epochType.Name} after winning as {characterType.Name}",
                    context => context.IsVictory && context.CharacterId == ModelDb.GetId(characterType)));
        }

        /// <summary>
        ///     Registers a rule that obtains <typeparamref name="TEpoch" /> after a win at or above
        ///     <paramref name="ascensionLevel" /> as <typeparamref name="TCharacter" />.
        /// </summary>
        public void UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(int ascensionLevel)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            UnlockEpochAfterAscensionWin(typeof(TCharacter), typeof(TEpoch), ascensionLevel);
        }

        /// <summary>
        ///     Registers a rule that obtains <paramref name="epochType" /> after a win at or above
        ///     <paramref name="ascensionLevel" /> as <paramref name="characterType" />.
        /// </summary>
        public void UnlockEpochAfterAscensionWin(Type characterType, Type epochType, int ascensionLevel)
        {
            RegisterPostRunRule(
                PostRunEpochUnlockRule.Create(
                    ModTimelineRegistry.GetEpochId(epochType),
                    $"Unlock {epochType.Name} after winning at ascension {ascensionLevel} as {characterType.Name}",
                    context => context.IsVictory &&
                               context.CharacterId == ModelDb.GetId(characterType) &&
                               context.AscensionLevel >= ascensionLevel));
        }

        /// <summary>
        ///     Registers a rule that obtains <typeparamref name="TEpoch" /> after <paramref name="requiredRuns" />
        ///     runs, optionally requiring a win on each qualifying run.
        /// </summary>
        public void UnlockEpochAfterRunCount<TEpoch>(int requiredRuns, bool requireVictory = false)
            where TEpoch : EpochModel, new()
        {
            RegisterPostRunRule(
                PostRunEpochUnlockRule.Create(
                    new TEpoch().Id,
                    $"Unlock {typeof(TEpoch).Name} after {requiredRuns} run(s)",
                    context => context.TotalRuns >= requiredRuns && (!requireVictory || context.IsVictory)));
        }

        /// <summary>
        ///     Registers a custom post-run epoch unlock rule.
        /// </summary>
        public void RegisterPostRunRule(PostRunEpochUnlockRule rule)
        {
            EnsureMutable($"register post-run epoch rule '{rule.Description}'");
            ArgumentNullException.ThrowIfNull(rule);

            lock (SyncRoot)
            {
                PostRunRules.Add(rule);
            }
        }

        /// <summary>
        ///     Registers elite-win counting for <typeparamref name="TCharacter" /> toward obtaining
        ///     <typeparamref name="TEpoch" />.
        /// </summary>
        public void UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(int requiredEliteWins = 15)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            UnlockEpochAfterEliteVictories(typeof(TCharacter), typeof(TEpoch), requiredEliteWins);
        }

        /// <summary>
        ///     Registers elite-win counting for <paramref name="characterType" /> toward obtaining
        ///     <paramref name="epochType" />.
        /// </summary>
        public void UnlockEpochAfterEliteVictories(Type characterType, Type epochType, int requiredEliteWins = 15)
        {
            RegisterEliteEpochRule(
                EliteEpochUnlockRule.Create(
                    ModelDb.GetId(characterType),
                    ModTimelineRegistry.GetEpochId(epochType),
                    requiredEliteWins,
                    $"Unlock {epochType.Name} after defeating {requiredEliteWins} elite(s) as {characterType.Name}"));
        }

        /// <summary>
        ///     Registers a custom elite-win epoch rule for a character.
        /// </summary>
        public void RegisterEliteEpochRule(EliteEpochUnlockRule rule)
        {
            EnsureMutable($"register elite epoch rule '{rule.Description}'");
            ArgumentNullException.ThrowIfNull(rule);

            lock (SyncRoot)
            {
                EliteEpochRulesByCharacterId[rule.CharacterId] = rule;
            }
        }

        /// <summary>
        ///     Registers boss-win counting for <typeparamref name="TCharacter" /> toward obtaining
        ///     <typeparamref name="TEpoch" />.
        /// </summary>
        public void UnlockEpochAfterBossVictories<TCharacter, TEpoch>(int requiredBossWins = 15)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            UnlockEpochAfterBossVictories(typeof(TCharacter), typeof(TEpoch), requiredBossWins);
        }

        /// <summary>
        ///     Registers boss-win counting for <paramref name="characterType" /> toward obtaining
        ///     <paramref name="epochType" />.
        /// </summary>
        public void UnlockEpochAfterBossVictories(Type characterType, Type epochType, int requiredBossWins = 15)
        {
            RegisterBossEpochRule(
                CountedEpochUnlockRule.Create(
                    ModelDb.GetId(characterType),
                    ModTimelineRegistry.GetEpochId(epochType),
                    requiredBossWins,
                    $"Unlock {epochType.Name} after defeating {requiredBossWins} boss(es) as {characterType.Name}"));
        }

        /// <summary>
        ///     Registers a custom boss-win epoch rule for a character.
        /// </summary>
        public void RegisterBossEpochRule(CountedEpochUnlockRule rule)
        {
            EnsureMutable($"register boss epoch rule '{rule.Description}'");
            ArgumentNullException.ThrowIfNull(rule);

            lock (SyncRoot)
            {
                BossEpochRulesByCharacterId[rule.CharacterId] = rule;
            }
        }

        /// <summary>
        ///     Maps ascension-level-one completion for <typeparamref name="TCharacter" /> to obtaining
        ///     <typeparamref name="TEpoch" />.
        /// </summary>
        public void UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            UnlockEpochAfterAscensionOneWin(typeof(TCharacter), typeof(TEpoch));
        }

        /// <summary>
        ///     Maps ascension-level-one completion for <paramref name="characterType" /> to obtaining
        ///     <paramref name="epochType" />.
        /// </summary>
        public void UnlockEpochAfterAscensionOneWin(Type characterType, Type epochType)
        {
            RegisterAscensionOneEpoch(ModelDb.GetId(characterType), ModTimelineRegistry.GetEpochId(epochType));
        }

        /// <summary>
        ///     Registers which epoch is granted after an ascension-one win for <paramref name="characterId" />.
        /// </summary>
        public void RegisterAscensionOneEpoch(ModelId characterId, string epochId)
        {
            EnsureMutable($"register ascension-one epoch '{epochId}'");
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);

            lock (SyncRoot)
            {
                AscensionOneEpochsByCharacterId[characterId] = epochId;
            }
        }

        /// <summary>
        ///     Configures ascension UI reveal for <typeparamref name="TCharacter" /> to depend on
        ///     <typeparamref name="TEpoch" /> being revealed.
        /// </summary>
        public void RevealAscensionAfterEpoch<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            RevealAscensionAfterEpoch(typeof(TCharacter), typeof(TEpoch));
        }

        /// <summary>
        ///     Configures ascension UI reveal for <paramref name="characterType" /> to depend on
        ///     <paramref name="epochType" /> being revealed.
        /// </summary>
        public void RevealAscensionAfterEpoch(Type characterType, Type epochType)
        {
            RegisterAscensionRevealEpoch(ModelDb.GetId(characterType), ModTimelineRegistry.GetEpochId(epochType));
        }

        /// <summary>
        ///     Registers which epoch must be revealed before ascension is shown for <paramref name="characterId" />.
        /// </summary>
        public void RegisterAscensionRevealEpoch(ModelId characterId, string epochId)
        {
            EnsureMutable($"register ascension reveal epoch '{epochId}'");
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);

            lock (SyncRoot)
            {
                AscensionRevealEpochsByCharacterId[characterId] = epochId;
            }
        }

        /// <summary>
        ///     Registers the vanilla-style character-unlock epoch obtained after a run as
        ///     <typeparamref name="TCharacter" />.
        /// </summary>
        public void UnlockCharacterAfterRunAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            UnlockCharacterAfterRunAs(typeof(TCharacter), typeof(TEpoch));
        }

        /// <summary>
        ///     Registers the vanilla-style character-unlock epoch obtained after a run as
        ///     <paramref name="characterType" />.
        /// </summary>
        public void UnlockCharacterAfterRunAs(Type characterType, Type epochType)
        {
            RegisterPostRunCharacterUnlockEpoch(ModelDb.GetId(characterType),
                ModTimelineRegistry.GetEpochId(epochType));
        }

        /// <summary>
        ///     Registers which epoch is granted by the post-run character-unlock check for
        ///     <paramref name="characterId" />.
        /// </summary>
        public void RegisterPostRunCharacterUnlockEpoch(ModelId characterId, string epochId)
        {
            EnsureMutable($"register post-run character unlock epoch '{epochId}'");
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);

            lock (SyncRoot)
            {
                PostRunCharacterUnlockEpochsByCharacterId[characterId] = epochId;
            }
        }

        internal static void FreezeRegistrations(string reason)
        {
            lock (SyncRoot)
            {
                if (IsFrozen)
                    return;

                IsFrozen = true;
                foreach (var registry in Registries.Values)
                    registry._freezeReason = reason;
            }
        }

        /// <summary>
        ///     Whether <paramref name="model" /> passes epoch gating for <paramref name="unlockState" />.
        ///     Vanilla <see cref="UnlockState" /> built from progress only lists <see cref="EpochState.Revealed" /> epochs in
        ///     <c>UnlockedEpochs</c>, while <see cref="SaveManager.ObtainEpoch" /> sets <see cref="EpochState.Obtained" /> /
        ///     <see cref="EpochState.ObtainedNoSlot" /> until the timeline reveals the slot. Mod unlock rules call
        ///     <c>ObtainEpoch</c>, so we also treat <see cref="ProgressState.IsEpochObtained" /> as satisfying
        ///     <see cref="RequireEpoch(Type,string)" />.
        /// </summary>
        internal static bool IsUnlocked(AbstractModel model, UnlockState unlockState)
        {
            lock (SyncRoot)
            {
                if (!RequiredEpochsByModelId.TryGetValue(model.Id, out var epochId))
                    return true;

                var modelType = model.GetType();
                if (ModContentRegistry.TryGetOwnerModId(modelType, out var modOwner) &&
                    ModIdsIgnoringEpochRequirements.Contains(modOwner))
                    return true;

                if (unlockState.ToSerializable().UnlockedEpochs.Contains(epochId))
                    return true;

                var save = SaveManager.Instance;
                return save != null && save.Progress.IsEpochObtained(epochId);
            }
        }

        internal static IEnumerable<TModel> FilterUnlocked<TModel>(IEnumerable<TModel> source, UnlockState unlockState)
            where TModel : AbstractModel
        {
            return source.Where(model => IsUnlocked(model, unlockState)).ToArray();
        }

        internal static bool TryGetEliteEpochRule(ModelId characterId, out EliteEpochUnlockRule rule)
        {
            lock (SyncRoot)
            {
                return EliteEpochRulesByCharacterId.TryGetValue(characterId, out rule!);
            }
        }

        internal static bool TryGetBossEpochRule(ModelId characterId, out CountedEpochUnlockRule rule)
        {
            lock (SyncRoot)
            {
                return BossEpochRulesByCharacterId.TryGetValue(characterId, out rule!);
            }
        }

        internal static bool TryGetAscensionOneEpoch(ModelId characterId, out string epochId)
        {
            lock (SyncRoot)
            {
                return AscensionOneEpochsByCharacterId.TryGetValue(characterId, out epochId!);
            }
        }

        internal static bool TryGetAscensionRevealEpoch(ModelId characterId, out string epochId)
        {
            lock (SyncRoot)
            {
                return AscensionRevealEpochsByCharacterId.TryGetValue(characterId, out epochId!);
            }
        }

        internal static bool TryGetPostRunCharacterUnlockEpoch(ModelId characterId, out string epochId)
        {
            lock (SyncRoot)
            {
                return PostRunCharacterUnlockEpochsByCharacterId.TryGetValue(characterId, out epochId!);
            }
        }

        internal static void ProcessRunEnded(RunManager runManager, SerializableRun serializableRun, bool isVictory,
            bool isAbandoned)
        {
            ArgumentNullException.ThrowIfNull(runManager);
            ArgumentNullException.ThrowIfNull(serializableRun);

            var localPlayer = LocalContext.GetMe(serializableRun);
            if (localPlayer == null)
                return;

            PostRunEpochUnlockRule[] rules;
            lock (SyncRoot)
            {
                rules = PostRunRules.ToArray();
            }

            if (rules.Length == 0)
                return;

            if (localPlayer.CharacterId == null) return;
            var context = new PostRunUnlockContext(
                serializableRun,
                localPlayer,
                isVictory,
                isAbandoned,
                SaveManager.Instance.Progress.NumberOfRuns,
                SaveManager.Instance.Progress.Wins,
                localPlayer.CharacterId,
                serializableRun.Ascension);

            foreach (var rule in rules)
            {
                if (SaveManager.Instance.Progress.IsEpochObtained(rule.EpochId))
                    continue;

                if (!rule.ShouldUnlock(context))
                    continue;

                if (!EpochRuntimeCompatibility.CanUseEpochId(
                        rule.EpochId,
                        $"post-run epoch rule '{rule.Description}'"))
                    continue;

                SaveManager.Instance.ObtainEpoch(rule.EpochId);
                if (!localPlayer.DiscoveredEpochs.Contains(rule.EpochId, StringComparer.Ordinal))
                    localPlayer.DiscoveredEpochs.Add(rule.EpochId);

                var livePlayer = LocalContext.GetMe(runManager.State);
                if (livePlayer != null && !livePlayer.DiscoveredEpochs.Contains(rule.EpochId, StringComparer.Ordinal))
                    livePlayer.DiscoveredEpochs.Add(rule.EpochId);

                RitsuLibFramework.Logger.Info(
                    $"[Unlocks] Obtained epoch '{rule.EpochId}' via post-run rule: {rule.Description}");
            }
        }

        private void EnsureMutable(string operation)
        {
            if (!IsFrozen)
                return;

            throw new InvalidOperationException(
                $"Cannot {operation} after unlock registration has been frozen ({_freezeReason ?? "unknown"}). " +
                "Register unlock rules from your mod initializer before model initialization.");
        }
    }

    /// <summary>
    ///     Snapshot of run and profile statistics passed to post-run unlock predicates.
    /// </summary>
    /// <param name="Run">Serializable run being finalized.</param>
    /// <param name="LocalPlayer">Local player state from the run.</param>
    /// <param name="IsVictory">True if the run ended in victory.</param>
    /// <param name="IsAbandoned">True if the run was abandoned.</param>
    /// <param name="TotalRuns">Total runs recorded in progress at evaluation time.</param>
    /// <param name="TotalWins">Total wins recorded in progress at evaluation time.</param>
    /// <param name="CharacterId">Character played for this run.</param>
    /// <param name="AscensionLevel">Ascension level of the run.</param>
    public sealed record PostRunUnlockContext(
        SerializableRun Run,
        SerializablePlayer LocalPlayer,
        bool IsVictory,
        bool IsAbandoned,
        int TotalRuns,
        int TotalWins,
        ModelId CharacterId,
        int AscensionLevel);

    /// <summary>
    ///     Describes an epoch granted when a post-run predicate returns true.
    /// </summary>
    /// <param name="EpochId">Epoch identifier to obtain.</param>
    /// <param name="Description">Human-readable label used in logs.</param>
    /// <param name="ShouldUnlock">Predicate evaluated after each run ends.</param>
    public sealed record PostRunEpochUnlockRule(
        string EpochId,
        string Description,
        Func<PostRunUnlockContext, bool> ShouldUnlock)
    {
        /// <summary>
        ///     Creates a <see cref="PostRunEpochUnlockRule" /> with validated inputs.
        /// </summary>
        public static PostRunEpochUnlockRule Create(string epochId, string description,
            Func<PostRunUnlockContext, bool> shouldUnlock)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentNullException.ThrowIfNull(shouldUnlock);
            return new(epochId, description, shouldUnlock);
        }
    }

    /// <summary>
    ///     Elite-win counting rule that obtains an epoch after enough elite victories as a character.
    /// </summary>
    /// <param name="CharacterId">Character whose elite wins are counted.</param>
    /// <param name="EpochId">Epoch identifier to obtain.</param>
    /// <param name="RequiredEliteWins">Minimum elite wins required.</param>
    /// <param name="Description">Human-readable label used in logs.</param>
    public sealed record EliteEpochUnlockRule(
        ModelId CharacterId,
        string EpochId,
        int RequiredEliteWins,
        string Description)
    {
        /// <summary>
        ///     Creates an <see cref="EliteEpochUnlockRule" /> with validated inputs.
        /// </summary>
        public static EliteEpochUnlockRule Create(
            ModelId characterId,
            string epochId,
            int requiredEliteWins,
            string description)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);
            ArgumentOutOfRangeException.ThrowIfLessThan(requiredEliteWins, 1);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            return new(characterId, epochId, requiredEliteWins, description);
        }
    }

    /// <summary>
    ///     Generic counted encounter-win rule (used for boss epochs) for a character.
    /// </summary>
    /// <param name="CharacterId">Character whose wins are counted.</param>
    /// <param name="EpochId">Epoch identifier to obtain.</param>
    /// <param name="RequiredWins">Minimum wins required.</param>
    /// <param name="Description">Human-readable label used in logs.</param>
    public sealed record CountedEpochUnlockRule(
        ModelId CharacterId,
        string EpochId,
        int RequiredWins,
        string Description)
    {
        /// <summary>
        ///     Creates a <see cref="CountedEpochUnlockRule" /> with validated inputs.
        /// </summary>
        public static CountedEpochUnlockRule Create(
            ModelId characterId,
            string epochId,
            int requiredWins,
            string description)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);
            ArgumentOutOfRangeException.ThrowIfLessThan(requiredWins, 1);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            return new(characterId, epochId, requiredWins, description);
        }
    }
}
