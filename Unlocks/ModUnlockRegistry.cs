using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Diagnostics;

namespace STS2RitsuLib.Unlocks
{
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

        private string? _freezeReason;

        private ModUnlockRegistry(string modId)
        {
            ModId = modId;
        }

        public string ModId { get; }
        public static bool IsFrozen { get; private set; }

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

        public void RequireEpoch<TModel, TEpoch>()
            where TModel : AbstractModel
            where TEpoch : EpochModel, new()
        {
            RequireEpoch(typeof(TModel), new TEpoch().Id);
        }

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

        public void UnlockEpochAfterRunAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            RegisterPostRunRule(
                PostRunEpochUnlockRule.Create(
                    new TEpoch().Id,
                    $"Unlock {typeof(TEpoch).Name} after finishing a run as {typeof(TCharacter).Name}",
                    context => context.CharacterId == ModelDb.GetId<TCharacter>()));
        }

        public void UnlockEpochAfterWinAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            RegisterPostRunRule(
                PostRunEpochUnlockRule.Create(
                    new TEpoch().Id,
                    $"Unlock {typeof(TEpoch).Name} after winning as {typeof(TCharacter).Name}",
                    context => context.IsVictory && context.CharacterId == ModelDb.GetId<TCharacter>()));
        }

        public void UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(int ascensionLevel)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            RegisterPostRunRule(
                PostRunEpochUnlockRule.Create(
                    new TEpoch().Id,
                    $"Unlock {typeof(TEpoch).Name} after winning at ascension {ascensionLevel} as {typeof(TCharacter).Name}",
                    context => context.IsVictory &&
                               context.CharacterId == ModelDb.GetId<TCharacter>() &&
                               context.AscensionLevel >= ascensionLevel));
        }

        public void UnlockEpochAfterRunCount<TEpoch>(int requiredRuns, bool requireVictory = false)
            where TEpoch : EpochModel, new()
        {
            RegisterPostRunRule(
                PostRunEpochUnlockRule.Create(
                    new TEpoch().Id,
                    $"Unlock {typeof(TEpoch).Name} after {requiredRuns} run(s)",
                    context => context.TotalRuns >= requiredRuns && (!requireVictory || context.IsVictory)));
        }

        public void RegisterPostRunRule(PostRunEpochUnlockRule rule)
        {
            EnsureMutable($"register post-run epoch rule '{rule.Description}'");
            ArgumentNullException.ThrowIfNull(rule);

            lock (SyncRoot)
            {
                PostRunRules.Add(rule);
            }
        }

        public void UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(int requiredEliteWins = 15)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            RegisterEliteEpochRule(
                EliteEpochUnlockRule.Create(
                    ModelDb.GetId<TCharacter>(),
                    new TEpoch().Id,
                    requiredEliteWins,
                    $"Unlock {typeof(TEpoch).Name} after defeating {requiredEliteWins} elite(s) as {typeof(TCharacter).Name}"));
        }

        public void RegisterEliteEpochRule(EliteEpochUnlockRule rule)
        {
            EnsureMutable($"register elite epoch rule '{rule.Description}'");
            ArgumentNullException.ThrowIfNull(rule);

            lock (SyncRoot)
            {
                EliteEpochRulesByCharacterId[rule.CharacterId] = rule;
            }
        }

        public void UnlockEpochAfterBossVictories<TCharacter, TEpoch>(int requiredBossWins = 15)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            RegisterBossEpochRule(
                CountedEpochUnlockRule.Create(
                    ModelDb.GetId<TCharacter>(),
                    new TEpoch().Id,
                    requiredBossWins,
                    $"Unlock {typeof(TEpoch).Name} after defeating {requiredBossWins} boss(es) as {typeof(TCharacter).Name}"));
        }

        public void RegisterBossEpochRule(CountedEpochUnlockRule rule)
        {
            EnsureMutable($"register boss epoch rule '{rule.Description}'");
            ArgumentNullException.ThrowIfNull(rule);

            lock (SyncRoot)
            {
                BossEpochRulesByCharacterId[rule.CharacterId] = rule;
            }
        }

        public void UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            RegisterAscensionOneEpoch(ModelDb.GetId<TCharacter>(), new TEpoch().Id);
        }

        public void RegisterAscensionOneEpoch(ModelId characterId, string epochId)
        {
            EnsureMutable($"register ascension-one epoch '{epochId}'");
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);

            lock (SyncRoot)
            {
                AscensionOneEpochsByCharacterId[characterId] = epochId;
            }
        }

        public void RevealAscensionAfterEpoch<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            RegisterAscensionRevealEpoch(ModelDb.GetId<TCharacter>(), new TEpoch().Id);
        }

        public void RegisterAscensionRevealEpoch(ModelId characterId, string epochId)
        {
            EnsureMutable($"register ascension reveal epoch '{epochId}'");
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);

            lock (SyncRoot)
            {
                AscensionRevealEpochsByCharacterId[characterId] = epochId;
            }
        }

        public void UnlockCharacterAfterRunAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            RegisterPostRunCharacterUnlockEpoch(ModelDb.GetId<TCharacter>(), new TEpoch().Id);
        }

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

        internal static bool IsUnlocked(AbstractModel model, UnlockState unlockState)
        {
            lock (SyncRoot)
            {
                return !RequiredEpochsByModelId.TryGetValue(model.Id, out var epochId) ||
                       unlockState.ToSerializable().UnlockedEpochs.Contains(epochId) ||
                       SaveManager.Instance.IsEpochRevealed(epochId);
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

    public sealed record PostRunUnlockContext(
        SerializableRun Run,
        SerializablePlayer LocalPlayer,
        bool IsVictory,
        bool IsAbandoned,
        int TotalRuns,
        int TotalWins,
        ModelId CharacterId,
        int AscensionLevel);

    public sealed record PostRunEpochUnlockRule(
        string EpochId,
        string Description,
        Func<PostRunUnlockContext, bool> ShouldUnlock)
    {
        public static PostRunEpochUnlockRule Create(string epochId, string description,
            Func<PostRunUnlockContext, bool> shouldUnlock)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentNullException.ThrowIfNull(shouldUnlock);
            return new(epochId, description, shouldUnlock);
        }
    }

    public sealed record EliteEpochUnlockRule(
        ModelId CharacterId,
        string EpochId,
        int RequiredEliteWins,
        string Description)
    {
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

    public sealed record CountedEpochUnlockRule(
        ModelId CharacterId,
        string EpochId,
        int RequiredWins,
        string Description)
    {
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
