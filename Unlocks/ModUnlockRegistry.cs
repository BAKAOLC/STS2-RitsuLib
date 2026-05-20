using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
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
    ///     按 mod 注册纪元要求，以及通过
    ///     Harmony 补丁集成的跑局后 / 战斗衍生解锁规则。
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
        ///     此注册表实例所属的 mod 标识符。
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     True after the framework freezes further unlock rule registration.
        ///     框架冻结后续解锁规则注册后为 true。
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     Returns the unlock registry singleton for <paramref name="modId" />.
        ///     返回 <paramref name="modId" /> 的解锁注册表单例。
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
        ///     当 <paramref name="ignored" /> 为 true 时，由 <paramref name="modId" /> 注册的模型会跳过
        ///     运行时的 <see cref="RequireEpoch(Type,string)" /> 门控（卡牌 / 遗物 / 角色会表现得像已满足所有纪元
        ///     要求）。
        ///     与该角色绑定的进阶显示规则仍会通过补丁集成检查此旁路。
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
        ///     要求 <typeparamref name="TModel" /> 内容保持锁定，直到 <typeparamref name="TEpoch" />
        ///     被获得或显示。
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
        ///     要求 <paramref name="modelType" /> 内容保持锁定，直到 <paramref name="epochType" />
        ///     被获得或显示。
        /// </summary>
        public void RequireEpoch(Type modelType, Type epochType)
        {
            RequireEpoch(modelType, ModTimelineRegistry.GetEpochId(epochType));
        }

        /// <summary>
        ///     Requires <paramref name="modelType" /> content to remain locked until <paramref name="epochId" /> is
        ///     obtained or revealed.
        ///     要求 <paramref name="modelType" /> 内容保持锁定，直到 <paramref name="epochId" />
        ///     被获得或显示。
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
        ///     注册一条规则：以
        ///     <typeparamref name="TCharacter" /> 完成任意跑局后获得 <typeparamref name="TEpoch" />。
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
        ///     注册一条规则：以
        ///     <paramref name="characterType" /> 完成任意跑局后获得 <paramref name="epochType" />。
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
        ///     注册一条规则：以
        ///     <typeparamref name="TCharacter" /> 赢得跑局后获得 <typeparamref name="TEpoch" />。
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
        ///     注册一条规则：以
        ///     <paramref name="characterType" /> 赢得跑局后获得 <paramref name="epochType" />。
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
        ///     注册一条规则：以 <typeparamref name="TCharacter" /> 在指定进阶等级或更高等级获胜后获得
        ///     <typeparamref name="TEpoch" />；等级由 <paramref name="ascensionLevel" /> 指定。
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
        ///     注册一条规则：以 <paramref name="characterType" /> 在指定进阶等级或更高等级获胜后获得
        ///     <paramref name="epochType" />；等级由 <paramref name="ascensionLevel" /> 指定。
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
        ///     注册一条规则：在 <paramref name="requiredRuns" /> 次
        ///     跑局后获得 <typeparamref name="TEpoch" />，并可要求每次计入条件的跑局都必须获胜。
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
        ///     注册自定义跑局后纪元解锁规则。
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
        ///     注册 <typeparamref name="TCharacter" /> 的精英胜利计数，用于获得
        ///     <typeparamref name="TEpoch" />。
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
        ///     注册 <paramref name="characterType" /> 的精英胜利计数，用于获得
        ///     <paramref name="epochType" />。
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
        ///     为角色注册自定义精英胜利纪元规则。
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
        ///     注册 <typeparamref name="TCharacter" /> 的 Boss 胜利计数，用于获得
        ///     <typeparamref name="TEpoch" />。
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
        ///     注册 <paramref name="characterType" /> 的 Boss 胜利计数，用于获得
        ///     <paramref name="epochType" />。
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
        ///     为角色注册自定义 Boss 胜利纪元规则。
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
        ///     将 <typeparamref name="TCharacter" /> 的进阶等级一完成映射为获得
        ///     <typeparamref name="TEpoch" />。
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
        ///     将 <paramref name="characterType" /> 的进阶等级一完成映射为获得
        ///     <paramref name="epochType" />。
        /// </summary>
        public void UnlockEpochAfterAscensionOneWin(Type characterType, Type epochType)
        {
            RegisterAscensionOneEpoch(ModelDb.GetId(characterType), ModTimelineRegistry.GetEpochId(epochType));
        }

        /// <summary>
        ///     Registers which epoch is granted after an ascension-one win for <paramref name="characterId" />.
        ///     注册 <paramref name="characterId" /> 在进阶一胜利后授予的纪元。
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
        ///     配置 <typeparamref name="TCharacter" /> 的进阶 UI 显示，使其依赖于
        ///     <typeparamref name="TEpoch" /> 已显示。
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
        ///     配置 <paramref name="characterType" /> 的进阶 UI 显示，使其依赖于
        ///     <paramref name="epochType" /> 已显示。
        /// </summary>
        public void RevealAscensionAfterEpoch(Type characterType, Type epochType)
        {
            RegisterAscensionRevealEpoch(ModelDb.GetId(characterType), ModTimelineRegistry.GetEpochId(epochType));
        }

        /// <summary>
        ///     Registers which epoch must be revealed before ascension is shown for <paramref name="characterId" />.
        ///     注册 <paramref name="characterId" /> 显示进阶前必须先显示的纪元。
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
        ///     注册以
        ///     <typeparamref name="TCharacter" /> 完成跑局后获得的原版风格角色解锁纪元。
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
        ///     注册以
        ///     <paramref name="characterType" /> 完成跑局后获得的原版风格角色解锁纪元。
        /// </summary>
        public void UnlockCharacterAfterRunAs(Type characterType, Type epochType)
        {
            RegisterPostRunCharacterUnlockEpoch(ModelDb.GetId(characterType),
                ModTimelineRegistry.GetEpochId(epochType));
        }

        /// <summary>
        ///     Registers which epoch is granted by the post-run character-unlock check for
        ///     <paramref name="characterId" />.
        ///     注册跑局后角色解锁检查为
        ///     <paramref name="characterId" /> 授予的纪元。
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

        internal static void ValidateFrozenModelReferences()
        {
            ModelId[] requiredModelIds;
            ModelId[] eliteCharacterIds;
            ModelId[] bossCharacterIds;
            ModelId[] ascensionOneCharacterIds;
            ModelId[] ascensionRevealCharacterIds;
            ModelId[] postRunCharacterUnlockIds;
            lock (SyncRoot)
            {
                requiredModelIds = [.. RequiredEpochsByModelId.Keys];
                eliteCharacterIds = [.. EliteEpochRulesByCharacterId.Keys];
                bossCharacterIds = [.. BossEpochRulesByCharacterId.Keys];
                ascensionOneCharacterIds = [.. AscensionOneEpochsByCharacterId.Keys];
                ascensionRevealCharacterIds = [.. AscensionRevealEpochsByCharacterId.Keys];
                postRunCharacterUnlockIds = [.. PostRunCharacterUnlockEpochsByCharacterId.Keys];
            }

            foreach (var modelId in requiredModelIds)
                RegistrationFreezeDiagnostics.WarnMissingModelId(
                    "Unlocks",
                    null,
                    "RequireEpoch model",
                    modelId,
                    typeof(AbstractModel));

            foreach (var characterId in eliteCharacterIds)
                WarnMissingCharacterId("elite epoch rule character", characterId);

            foreach (var characterId in bossCharacterIds)
                WarnMissingCharacterId("boss epoch rule character", characterId);

            foreach (var characterId in ascensionOneCharacterIds)
                WarnMissingCharacterId("ascension-one epoch character", characterId);

            foreach (var characterId in ascensionRevealCharacterIds)
                WarnMissingCharacterId("ascension reveal character", characterId);

            foreach (var characterId in postRunCharacterUnlockIds)
                WarnMissingCharacterId("post-run character unlock character", characterId);
            return;

            static void WarnMissingCharacterId(string description, ModelId characterId)
            {
                RegistrationFreezeDiagnostics.WarnMissingModelId(
                    "Unlocks",
                    null,
                    description,
                    characterId,
                    typeof(CharacterModel));
            }
        }

        /// <summary>
        ///     Whether <paramref name="model" /> passes epoch gating for <paramref name="unlockState" />.
        ///     Vanilla <see cref="UnlockState" /> built from progress only lists <see cref="EpochState.Revealed" /> epochs in
        ///     <c>UnlockedEpochs</c>, while <see cref="SaveManager.ObtainEpoch" /> sets <see cref="EpochState.Obtained" /> /
        ///     <see cref="EpochState.ObtainedNoSlot" /> until the timeline reveals the slot. Mod unlock rules call
        ///     <c>ObtainEpoch</c>, so we also treat <see cref="ProgressState.IsEpochObtained" /> as satisfying
        ///     <see cref="RequireEpoch(Type,string)" />.
        ///     <see cref="RequireEpoch(Type,string)" />。
        ///     <paramref name="model" /> 是否通过 <paramref name="unlockState" /> 的纪元门控。
        ///     从进度构建的原版 <see cref="UnlockState" /> 只会在 <c>UnlockedEpochs</c> 中列出 <see cref="EpochState.Revealed" /> 纪元，
        ///     而 <see cref="SaveManager.ObtainEpoch" /> 会设置 <see cref="EpochState.Obtained" /> /
        ///     <see cref="EpochState.ObtainedNoSlot" />，直到时间线显示该槽位。mod 解锁规则会调用
        ///     <c>ObtainEpoch</c>，因此也将 <see cref="ProgressState.IsEpochObtained" /> 视为满足
        ///     <see cref="RequireEpoch(Type,string)" />。
        ///     <see cref="RequireEpoch(Type,string)" />。
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
                NGame.Instance?.AddChildSafely(NGainEpochVfx.Create(EpochModel.Get(rule.EpochId)));
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
    ///     传递给跑局后解锁谓词的跑局和档案统计快照。
    /// </summary>
    /// <param name="Run">
    ///     Serializable run being finalized.
    ///     正在完成结算的可序列化跑局。
    /// </param>
    /// <param name="LocalPlayer">
    ///     Local player state from the run.
    ///     跑局中的本地玩家状态。
    /// </param>
    /// <param name="IsVictory">
    ///     True if the run ended in victory.
    ///     如果跑局以胜利结束，则为 true。
    /// </param>
    /// <param name="IsAbandoned">
    ///     True if the run was abandoned.
    ///     如果跑局被放弃，则为 true。
    /// </param>
    /// <param name="TotalRuns">
    ///     Total runs recorded in progress at evaluation time.
    ///     评估时进度中记录的总跑局数。
    /// </param>
    /// <param name="TotalWins">
    ///     Total wins recorded in progress at evaluation time.
    ///     评估时进度中记录的总胜利数。
    /// </param>
    /// <param name="CharacterId">
    ///     Character played for this run.
    ///     本次跑局使用的角色。
    /// </param>
    /// <param name="AscensionLevel">
    ///     Ascension level of the run.
    ///     本次跑局的进阶等级。
    /// </param>
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
    ///     描述跑局后谓词返回 true 时授予的纪元。
    /// </summary>
    /// <param name="EpochId">
    ///     Epoch identifier to obtain.
    ///     要获得的纪元标识符。
    /// </param>
    /// <param name="Description">
    ///     Human-readable label used in logs.
    ///     日志中使用的人类可读标签。
    /// </param>
    /// <param name="ShouldUnlock">
    ///     Predicate evaluated after each run ends.
    ///     每次跑局结束后评估的谓词。
    /// </param>
    public sealed record PostRunEpochUnlockRule(
        string EpochId,
        string Description,
        Func<PostRunUnlockContext, bool> ShouldUnlock)
    {
        /// <summary>
        ///     Creates a <see cref="PostRunEpochUnlockRule" /> with validated inputs.
        ///     使用已验证的输入创建 <see cref="PostRunEpochUnlockRule" />。
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
    ///     以某个角色取得足够精英胜利后获得纪元的计数规则。
    /// </summary>
    /// <param name="CharacterId">
    ///     Character whose elite wins are counted.
    ///     要统计精英战胜利的角色。
    /// </param>
    /// <param name="EpochId">
    ///     Epoch identifier to obtain.
    ///     要获得的纪元标识符。
    /// </param>
    /// <param name="RequiredEliteWins">
    ///     Minimum elite wins required.
    ///     所需的最少精英战胜利数。
    /// </param>
    /// <param name="Description">
    ///     Human-readable label used in logs.
    ///     日志中使用的人类可读标签。
    /// </param>
    public sealed record EliteEpochUnlockRule(
        ModelId CharacterId,
        string EpochId,
        int RequiredEliteWins,
        string Description)
    {
        /// <summary>
        ///     Creates an <see cref="EliteEpochUnlockRule" /> with validated inputs.
        ///     使用已验证的输入创建 <see cref="EliteEpochUnlockRule" />。
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
    ///     角色的通用遭遇胜利计数规则（用于 Boss 纪元）。
    /// </summary>
    /// <param name="CharacterId">
    ///     Character whose wins are counted.
    ///     要统计胜利的角色。
    /// </param>
    /// <param name="EpochId">
    ///     Epoch identifier to obtain.
    ///     要获得的纪元标识符。
    /// </param>
    /// <param name="RequiredWins">
    ///     Minimum wins required.
    ///     所需的最少胜利数。
    /// </param>
    /// <param name="Description">
    ///     Human-readable label used in logs.
    ///     日志中使用的人类可读标签。
    /// </param>
    public sealed record CountedEpochUnlockRule(
        ModelId CharacterId,
        string EpochId,
        int RequiredWins,
        string Description)
    {
        /// <summary>
        ///     Creates a <see cref="CountedEpochUnlockRule" /> with validated inputs.
        ///     使用已验证的输入创建 <see cref="CountedEpochUnlockRule" />。
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
