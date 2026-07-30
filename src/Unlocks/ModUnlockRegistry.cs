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
using STS2RitsuLib.Compat;
using STS2RitsuLib.Content;
using STS2RitsuLib.Diagnostics;
using STS2RitsuLib.Timeline;

namespace STS2RitsuLib.Unlocks
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers a mod's epoch requirements and its post-run or combat-derived unlock rules.
    ///     </para>
    ///     <para xml:lang="zh-CN">注册模组的纪元要求，以及一局游戏结束后或由战斗结果触发的解锁规则。</para>
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
        private static readonly Dictionary<ModelId, List<string>> PostRunCharacterUnlockEpochsByCharacterId = [];

        private static readonly HashSet<string> ModIdsIgnoringEpochRequirements =
            new(StringComparer.OrdinalIgnoreCase);

        private string? _freezeReason;

        private ModUnlockRegistry(string modId)
        {
            ModId = modId;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the mod that owns this registry.</para>
        ///     <para xml:lang="zh-CN">获取拥有此注册表的模组 ID。</para>
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the framework has frozen further unlock-rule registration.</para>
        ///     <para xml:lang="zh-CN">获取框架是否已冻结后续解锁规则注册。</para>
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Returns the unlock registry for <paramref name="modId" />.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="modId" /> 对应的解锁注册表。</para>
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
        ///     <para xml:lang="en">
        ///         Configures whether models owned by <paramref name="modId" /> bypass
        ///         <see cref="RequireEpoch(Type,string)" /> requirements at runtime. Ascension reveal rules for those
        ///         characters honor the same bypass.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         配置 <paramref name="modId" /> 所属模型是否在运行时跳过
        ///         <see cref="RequireEpoch(Type,string)" /> 要求。相应角色的进阶显示规则也会遵循此设置。
        ///     </para>
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

        internal static string[] GetEffectiveRequiredEpochIds(ProgressState progress)
        {
            ArgumentNullException.ThrowIfNull(progress);

            KeyValuePair<ModelId, string>[] requirements;
            lock (SyncRoot)
            {
                requirements = [.. RequiredEpochsByModelId];
            }

            return
            [
                .. requirements
                    .Where(entry => progress.IsEpochObtained(entry.Value))
                    .Select(static entry => entry.Value)
                    .Distinct(StringComparer.Ordinal),
            ];
        }

        internal static UnlockState IncludeObtainedRequiredEpochs(UnlockState unlockState, ProgressState progress)
        {
            ArgumentNullException.ThrowIfNull(unlockState);
            ArgumentNullException.ThrowIfNull(progress);

            var obtainedRequiredEpochIds = GetEffectiveRequiredEpochIds(progress);
            if (obtainedRequiredEpochIds.Length == 0)
                return unlockState;

            var serializable = unlockState.ToSerializable();
            var unlockedEpochs = serializable.UnlockedEpochs.ToHashSet(StringComparer.Ordinal);
            var changed =
                obtainedRequiredEpochIds.Aggregate(false, (current, epochId) => current | unlockedEpochs.Add(epochId));

            return changed
                ? new(unlockedEpochs, serializable.EncountersSeen, serializable.NumberOfRuns)
                : unlockState;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Keeps <typeparamref name="TModel" /> locked until <typeparamref name="TEpoch" /> is obtained or revealed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在获得或揭示 <typeparamref name="TEpoch" /> 前保持 <typeparamref name="TModel" /> 锁定。
        ///     </para>
        /// </summary>
        public void RequireEpoch<TModel, TEpoch>()
            where TModel : AbstractModel
            where TEpoch : EpochModel, new()
        {
            RequireEpoch(typeof(TModel), typeof(TEpoch));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Keeps <paramref name="modelType" /> locked until <paramref name="epochType" /> is obtained or revealed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在获得或揭示 <paramref name="epochType" /> 前保持 <paramref name="modelType" /> 锁定。
        ///     </para>
        /// </summary>
        public void RequireEpoch(Type modelType, Type epochType)
        {
            RequireEpoch(modelType, ModTimelineRegistry.GetEpochId(epochType));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Keeps <paramref name="modelType" /> locked until <paramref name="epochId" /> is obtained or revealed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在获得或揭示 <paramref name="epochId" /> 前保持 <paramref name="modelType" /> 锁定。
        ///     </para>
        /// </summary>
        public void RequireEpoch(Type modelType, string epochId)
        {
            RequireEpochCore(modelType, epochId, true);
        }

        internal void RequireEpochIfUnset(Type modelType, string epochId)
        {
            RequireEpochCore(modelType, epochId, false);
        }

        private void RequireEpochCore(Type modelType, string epochId, bool overwrite)
        {
            ArgumentNullException.ThrowIfNull(modelType);
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);
            EnsureMutable($"register unlock requirement for '{modelType.Name}'");

            RegistrationConflictDetector.ThrowIfModelIdConflicts(modelType);
            var modelId = ModelDb.GetId(modelType);

            lock (SyncRoot)
            {
                if (!overwrite && RequiredEpochsByModelId.ContainsKey(modelId))
                    return;

                RequiredEpochsByModelId[modelId] = epochId;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Obtains <typeparamref name="TEpoch" /> after any non-abandoned run as
        ///         <typeparamref name="TCharacter" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         以 <typeparamref name="TCharacter" /> 完成任何未放弃的一局游戏后获得
        ///         <typeparamref name="TEpoch" />。
        ///     </para>
        /// </summary>
        public void UnlockEpochAfterRunAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            UnlockEpochAfterRunAs(typeof(TCharacter), typeof(TEpoch));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Obtains <paramref name="epochType" /> after any non-abandoned run as
        ///         <paramref name="characterType" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         以 <paramref name="characterType" /> 完成任何未放弃的一局游戏后获得
        ///         <paramref name="epochType" />。
        ///     </para>
        /// </summary>
        public void UnlockEpochAfterRunAs(Type characterType, Type epochType)
        {
            ArgumentNullException.ThrowIfNull(characterType);
            ArgumentNullException.ThrowIfNull(epochType);
            RegisterPostRunRule(
                PostRunEpochUnlockRule.Create(
                    ModTimelineRegistry.GetEpochId(epochType),
                    $"Unlock {epochType.Name} after finishing a run as {characterType.Name}",
                    context => !context.IsAbandoned &&
                               context.CharacterId == ModelDb.GetId(characterType)));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Obtains <typeparamref name="TEpoch" /> after winning a run as <typeparamref name="TCharacter" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         以 <typeparamref name="TCharacter" /> 赢得一局游戏后获得 <typeparamref name="TEpoch" />。
        ///     </para>
        /// </summary>
        public void UnlockEpochAfterWinAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            UnlockEpochAfterWinAs(typeof(TCharacter), typeof(TEpoch));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Obtains <paramref name="epochType" /> after winning a run as <paramref name="characterType" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         以 <paramref name="characterType" /> 赢得一局游戏后获得 <paramref name="epochType" />。
        ///     </para>
        /// </summary>
        public void UnlockEpochAfterWinAs(Type characterType, Type epochType)
        {
            ArgumentNullException.ThrowIfNull(characterType);
            ArgumentNullException.ThrowIfNull(epochType);
            RegisterPostRunRule(
                PostRunEpochUnlockRule.Create(
                    ModTimelineRegistry.GetEpochId(epochType),
                    $"Unlock {epochType.Name} after winning as {characterType.Name}",
                    context => context.IsVictory && context.CharacterId == ModelDb.GetId(characterType)));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Obtains <typeparamref name="TEpoch" /> after winning as <typeparamref name="TCharacter" /> at
        ///         <paramref name="ascensionLevel" /> or higher.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         以 <typeparamref name="TCharacter" /> 在 <paramref name="ascensionLevel" /> 或更高进阶等级获胜后获得
        ///         <typeparamref name="TEpoch" />。
        ///     </para>
        /// </summary>
        public void UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(int ascensionLevel)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            UnlockEpochAfterAscensionWin(typeof(TCharacter), typeof(TEpoch), ascensionLevel);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Obtains <paramref name="epochType" /> after winning as <paramref name="characterType" /> at
        ///         <paramref name="ascensionLevel" /> or higher.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         以 <paramref name="characterType" /> 在 <paramref name="ascensionLevel" /> 或更高进阶等级获胜后获得
        ///         <paramref name="epochType" />。
        ///     </para>
        /// </summary>
        public void UnlockEpochAfterAscensionWin(Type characterType, Type epochType, int ascensionLevel)
        {
            ArgumentNullException.ThrowIfNull(characterType);
            ArgumentNullException.ThrowIfNull(epochType);
            ArgumentOutOfRangeException.ThrowIfNegative(ascensionLevel);
            RegisterPostRunRule(
                PostRunEpochUnlockRule.Create(
                    ModTimelineRegistry.GetEpochId(epochType),
                    $"Unlock {epochType.Name} after winning at ascension {ascensionLevel} as {characterType.Name}",
                    context => context.IsVictory &&
                               context.CharacterId == ModelDb.GetId(characterType) &&
                               context.AscensionLevel >= ascensionLevel));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Obtains <typeparamref name="TEpoch" /> once the profile has recorded
        ///         <paramref name="requiredRuns" /> runs. When <paramref name="requireVictory" /> is
        ///         <see langword="true" />, the run that reaches or exceeds the threshold must be a victory.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         档案记录的游戏局数达到 <paramref name="requiredRuns" /> 后获得 <typeparamref name="TEpoch" />。
        ///         <paramref name="requireVictory" /> 为 <see langword="true" /> 时，触发判断的当前一局必须获胜。
        ///     </para>
        /// </summary>
        public void UnlockEpochAfterRunCount<TEpoch>(int requiredRuns, bool requireVictory = false)
            where TEpoch : EpochModel, new()
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(requiredRuns, 1);
            RegisterPostRunRule(
                PostRunEpochUnlockRule.Create(
                    ModTimelineRegistry.GetEpochId(typeof(TEpoch)),
                    $"Unlock {typeof(TEpoch).Name} after {requiredRuns} run(s)",
                    context => !context.IsAbandoned &&
                               context.TotalRuns >= requiredRuns &&
                               (!requireVictory || context.IsVictory)));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a custom post-run epoch unlock rule.</para>
        ///     <para xml:lang="zh-CN">注册自定义的一局游戏结束后纪元解锁规则。</para>
        /// </summary>
        public void RegisterPostRunRule(PostRunEpochUnlockRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnsureMutable($"register post-run epoch rule '{rule.Description}'");
            ValidateRule(rule.EpochId, rule.Description);
            ArgumentNullException.ThrowIfNull(rule.ShouldUnlock);

            lock (SyncRoot)
            {
                PostRunRules.Add(rule);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Obtains <typeparamref name="TEpoch" /> after <typeparamref name="TCharacter" /> records the required
        ///         number of elite victories.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <typeparamref name="TCharacter" /> 记录的精英战胜利数达到要求后获得
        ///         <typeparamref name="TEpoch" />。
        ///     </para>
        /// </summary>
        public void UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(int requiredEliteWins = 15)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            UnlockEpochAfterEliteVictories(typeof(TCharacter), typeof(TEpoch), requiredEliteWins);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Obtains <paramref name="epochType" /> after <paramref name="characterType" /> records the required
        ///         number of elite victories.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <paramref name="characterType" /> 记录的精英战胜利数达到要求后获得
        ///         <paramref name="epochType" />。
        ///     </para>
        /// </summary>
        public void UnlockEpochAfterEliteVictories(Type characterType, Type epochType, int requiredEliteWins = 15)
        {
            ArgumentNullException.ThrowIfNull(characterType);
            ArgumentNullException.ThrowIfNull(epochType);
            RegisterEliteEpochRule(
                EliteEpochUnlockRule.Create(
                    ModelDb.GetId(characterType),
                    ModTimelineRegistry.GetEpochId(epochType),
                    requiredEliteWins,
                    $"Unlock {epochType.Name} after defeating {requiredEliteWins} elite(s) as {characterType.Name}"));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a custom elite-victory epoch rule for a character.</para>
        ///     <para xml:lang="zh-CN">为角色注册自定义精英战胜利纪元规则。</para>
        /// </summary>
        public void RegisterEliteEpochRule(EliteEpochUnlockRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnsureMutable($"register elite epoch rule '{rule.Description}'");
            ValidateRule(rule.EpochId, rule.Description);
            ArgumentOutOfRangeException.ThrowIfLessThan(rule.RequiredEliteWins, 1);

            lock (SyncRoot)
            {
                EliteEpochRulesByCharacterId[rule.CharacterId] = rule;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Obtains <typeparamref name="TEpoch" /> after <typeparamref name="TCharacter" /> records the required
        ///         number of boss victories.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <typeparamref name="TCharacter" /> 记录的首领战胜利数达到要求后获得
        ///         <typeparamref name="TEpoch" />。
        ///     </para>
        /// </summary>
        public void UnlockEpochAfterBossVictories<TCharacter, TEpoch>(int requiredBossWins = 15)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            UnlockEpochAfterBossVictories(typeof(TCharacter), typeof(TEpoch), requiredBossWins);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Obtains <paramref name="epochType" /> after <paramref name="characterType" /> records the required
        ///         number of boss victories.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <paramref name="characterType" /> 记录的首领战胜利数达到要求后获得
        ///         <paramref name="epochType" />。
        ///     </para>
        /// </summary>
        public void UnlockEpochAfterBossVictories(Type characterType, Type epochType, int requiredBossWins = 15)
        {
            ArgumentNullException.ThrowIfNull(characterType);
            ArgumentNullException.ThrowIfNull(epochType);
            RegisterBossEpochRule(
                CountedEpochUnlockRule.Create(
                    ModelDb.GetId(characterType),
                    ModTimelineRegistry.GetEpochId(epochType),
                    requiredBossWins,
                    $"Unlock {epochType.Name} after defeating {requiredBossWins} boss(es) as {characterType.Name}"));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a custom boss-victory epoch rule for a character.</para>
        ///     <para xml:lang="zh-CN">为角色注册自定义首领战胜利纪元规则。</para>
        /// </summary>
        public void RegisterBossEpochRule(CountedEpochUnlockRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            EnsureMutable($"register boss epoch rule '{rule.Description}'");
            ValidateRule(rule.EpochId, rule.Description);
            ArgumentOutOfRangeException.ThrowIfLessThan(rule.RequiredWins, 1);

            lock (SyncRoot)
            {
                BossEpochRulesByCharacterId[rule.CharacterId] = rule;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Obtains <typeparamref name="TEpoch" /> after an Ascension 1 victory as
        ///         <typeparamref name="TCharacter" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         以 <typeparamref name="TCharacter" /> 赢得进阶 1 后获得 <typeparamref name="TEpoch" />。
        ///     </para>
        /// </summary>
        public void UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            UnlockEpochAfterAscensionOneWin(typeof(TCharacter), typeof(TEpoch));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Obtains <paramref name="epochType" /> after an Ascension 1 victory as
        ///         <paramref name="characterType" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         以 <paramref name="characterType" /> 赢得进阶 1 后获得 <paramref name="epochType" />。
        ///     </para>
        /// </summary>
        public void UnlockEpochAfterAscensionOneWin(Type characterType, Type epochType)
        {
            ArgumentNullException.ThrowIfNull(characterType);
            ArgumentNullException.ThrowIfNull(epochType);
            RegisterAscensionOneEpoch(ModelDb.GetId(characterType), ModTimelineRegistry.GetEpochId(epochType));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers the epoch granted after an Ascension 1 victory by <paramref name="characterId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="characterId" /> 赢得进阶 1 后授予的纪元。</para>
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
        ///     <para xml:lang="en">
        ///         Reveals the Ascension UI for <typeparamref name="TCharacter" /> after
        ///         <typeparamref name="TEpoch" /> is revealed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         揭示 <typeparamref name="TEpoch" /> 后显示 <typeparamref name="TCharacter" /> 的进阶界面。
        ///     </para>
        /// </summary>
        public void RevealAscensionAfterEpoch<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            RevealAscensionAfterEpoch(typeof(TCharacter), typeof(TEpoch));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Reveals the Ascension UI for <paramref name="characterType" /> after
        ///         <paramref name="epochType" /> is revealed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         揭示 <paramref name="epochType" /> 后显示 <paramref name="characterType" /> 的进阶界面。
        ///     </para>
        /// </summary>
        public void RevealAscensionAfterEpoch(Type characterType, Type epochType)
        {
            ArgumentNullException.ThrowIfNull(characterType);
            ArgumentNullException.ThrowIfNull(epochType);
            RegisterAscensionRevealEpoch(ModelDb.GetId(characterType), ModTimelineRegistry.GetEpochId(epochType));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers the epoch that must be revealed before Ascension is shown for
        ///         <paramref name="characterId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册 <paramref name="characterId" /> 显示进阶界面前必须先揭示的纪元。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Registers a base-game-style character-unlock epoch obtained after a run as
        ///         <typeparamref name="TCharacter" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册以 <typeparamref name="TCharacter" /> 完成一局游戏后获得的游戏本体式角色解锁纪元。
        ///     </para>
        /// </summary>
        public void UnlockCharacterAfterRunAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            UnlockCharacterAfterRunAs(typeof(TCharacter), typeof(TEpoch));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a base-game-style character-unlock epoch obtained after a run as
        ///         <paramref name="characterType" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册以 <paramref name="characterType" /> 完成一局游戏后获得的游戏本体式角色解锁纪元。
        ///     </para>
        /// </summary>
        public void UnlockCharacterAfterRunAs(Type characterType, Type epochType)
        {
            ArgumentNullException.ThrowIfNull(characterType);
            ArgumentNullException.ThrowIfNull(epochType);
            RegisterPostRunCharacterUnlockEpoch(ModelDb.GetId(characterType),
                ModTimelineRegistry.GetEpochId(epochType));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers an epoch granted by the post-run character-unlock check for
        ///         <paramref name="characterId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一局游戏结束后角色解锁检查为 <paramref name="characterId" /> 授予的纪元。
        ///     </para>
        /// </summary>
        public void RegisterPostRunCharacterUnlockEpoch(ModelId characterId, string epochId)
        {
            EnsureMutable($"register post-run character unlock epoch '{epochId}'");
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);

            lock (SyncRoot)
            {
                if (!PostRunCharacterUnlockEpochsByCharacterId.TryGetValue(characterId, out var epochIds))
                {
                    epochIds = [];
                    PostRunCharacterUnlockEpochsByCharacterId[characterId] = epochIds;
                }

                if (!epochIds.Contains(epochId, StringComparer.Ordinal))
                    epochIds.Add(epochId);
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
        ///     <para xml:lang="en">
        ///         Returns whether <paramref name="model" /> satisfies its epoch requirement in
        ///         <paramref name="unlockState" />. Content generation may use a multiplayer-merged
        ///         <see cref="UnlockState" />, so this check does not read the local profile.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="model" /> 是否满足 <paramref name="unlockState" /> 中的纪元要求。内容生成可能使用
        ///         多人游戏合并后的 <see cref="UnlockState" />，因此此检查不会直接读取本机档案。
        ///     </para>
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

                return unlockState.ToSerializable().UnlockedEpochs.Contains(epochId);
            }
        }

        internal static IEnumerable<TModel> FilterUnlocked<TModel>(IEnumerable<TModel> source, UnlockState unlockState)
            where TModel : AbstractModel
        {
            return [.. source.Where(model => IsUnlocked(model, unlockState))];
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
                if (PostRunCharacterUnlockEpochsByCharacterId.TryGetValue(characterId, out var epochIds) &&
                    epochIds.Count > 0)
                {
                    epochId = epochIds[0];
                    return true;
                }
            }

            epochId = string.Empty;
            return false;
        }

        internal static string[] GetPostRunCharacterUnlockEpochs(ModelId characterId)
        {
            lock (SyncRoot)
            {
                return PostRunCharacterUnlockEpochsByCharacterId.TryGetValue(characterId, out var epochIds)
                    ? [.. epochIds]
                    : [];
            }
        }

        internal static void ProcessRunEnded(RunManager runManager, SerializableRun serializableRun, bool isVictory,
            bool isAbandoned)
        {
            ArgumentNullException.ThrowIfNull(runManager);
            ArgumentNullException.ThrowIfNull(serializableRun);

            if (!Sts2RunGameModeCompat.IsStandardSerializableRunForEpochUnlocks(serializableRun))
                return;

            var localPlayer = LocalContext.GetMe(serializableRun);
            if (localPlayer == null)
                return;

            PostRunEpochUnlockRule[] rules;
            lock (SyncRoot)
            {
                rules = [.. PostRunRules];
            }

            if (rules.Length == 0)
                return;

            if (localPlayer.CharacterId == null)
                return;
            var context = new PostRunUnlockContext(
                serializableRun,
                localPlayer,
                isVictory,
                isAbandoned,
                SaveManager.Instance.Progress.NumberOfRuns,
                SaveManager.Instance.Progress.Wins,
                localPlayer.CharacterId,
                serializableRun.Ascension);

            var obtainedAnyEpoch = false;
            foreach (var rule in rules)
            {
                if (SaveManager.Instance.Progress.IsEpochObtained(rule.EpochId))
                    continue;

                try
                {
                    if (!rule.ShouldUnlock(context))
                        continue;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Unlocks] Post-run rule '{rule.Description}' failed and was skipped: {ex}");
                    continue;
                }

                if (!EpochRuntimeCompatibility.CanUseEpochId(
                        rule.EpochId,
                        $"post-run epoch rule '{rule.Description}'"))
                    continue;

                obtainedAnyEpoch = true;
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

            if (obtainedAnyEpoch)
                SaveManager.Instance.SaveProgressFile();
        }

        private void EnsureMutable(string operation)
        {
            if (!IsFrozen)
                return;

            throw new InvalidOperationException(
                $"Cannot {operation} after unlock registration has been frozen ({_freezeReason ?? "unknown"}). " +
                "Register unlock rules from your mod initializer before model initialization.");
        }

        private static void ValidateRule(string epochId, string description)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Contains the run and profile statistics passed to post-run unlock predicates.</para>
    ///     <para xml:lang="zh-CN">包含传给一局游戏结束后解锁谓词的本局与档案统计信息。</para>
    /// </summary>
    /// <param name="Run">
    ///     <para xml:lang="en">The serializable run being finalized.</para>
    ///     <para xml:lang="zh-CN">正在完成结算的可序列化游戏局数据。</para>
    /// </param>
    /// <param name="LocalPlayer">
    ///     <para xml:lang="en">The local player's state in the run.</para>
    ///     <para xml:lang="zh-CN">本局游戏中的本地玩家状态。</para>
    /// </param>
    /// <param name="IsVictory">
    ///     <para xml:lang="en">Whether the run ended in victory.</para>
    ///     <para xml:lang="zh-CN">本局游戏是否以胜利结束。</para>
    /// </param>
    /// <param name="IsAbandoned">
    ///     <para xml:lang="en">Whether the run was abandoned.</para>
    ///     <para xml:lang="zh-CN">本局游戏是否被放弃。</para>
    /// </param>
    /// <param name="TotalRuns">
    ///     <para xml:lang="en">The total number of runs recorded in progression data at evaluation time.</para>
    ///     <para xml:lang="zh-CN">判断时进度数据中记录的游戏总局数。</para>
    /// </param>
    /// <param name="TotalWins">
    ///     <para xml:lang="en">The total number of wins recorded in progression data at evaluation time.</para>
    ///     <para xml:lang="zh-CN">判断时进度数据中记录的总胜利数。</para>
    /// </param>
    /// <param name="CharacterId">
    ///     <para xml:lang="en">The character played in this run.</para>
    ///     <para xml:lang="zh-CN">本局游戏使用的角色。</para>
    /// </param>
    /// <param name="AscensionLevel">
    ///     <para xml:lang="en">The run's Ascension level.</para>
    ///     <para xml:lang="zh-CN">本局游戏的进阶等级。</para>
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
    ///     <para xml:lang="en">Describes an epoch granted when a post-run predicate returns <see langword="true" />.</para>
    ///     <para xml:lang="zh-CN">描述一局游戏结束后谓词返回 <see langword="true" /> 时授予的纪元。</para>
    /// </summary>
    /// <param name="EpochId">
    ///     <para xml:lang="en">The ID of the epoch to obtain.</para>
    ///     <para xml:lang="zh-CN">要获得的纪元 ID。</para>
    /// </param>
    /// <param name="Description">
    ///     <para xml:lang="en">A human-readable label used in logs.</para>
    ///     <para xml:lang="zh-CN">日志中使用的可读标签。</para>
    /// </param>
    /// <param name="ShouldUnlock">
    ///     <para xml:lang="en">The predicate evaluated after each run ends.</para>
    ///     <para xml:lang="zh-CN">每局游戏结束后判断的谓词。</para>
    /// </param>
    public sealed record PostRunEpochUnlockRule(
        string EpochId,
        string Description,
        Func<PostRunUnlockContext, bool> ShouldUnlock)
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a rule after validating its inputs.</para>
        ///     <para xml:lang="zh-CN">验证输入后创建规则。</para>
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
    ///     <para xml:lang="en">Describes an epoch obtained after a character records enough elite victories.</para>
    ///     <para xml:lang="zh-CN">描述角色记录足够精英战胜利后获得的纪元。</para>
    /// </summary>
    /// <param name="CharacterId">
    ///     <para xml:lang="en">The ID of the character whose elite victories are counted.</para>
    ///     <para xml:lang="zh-CN">要统计精英战胜利的角色 ID。</para>
    /// </param>
    /// <param name="EpochId">
    ///     <para xml:lang="en">The ID of the epoch to obtain.</para>
    ///     <para xml:lang="zh-CN">要获得的纪元 ID。</para>
    /// </param>
    /// <param name="RequiredEliteWins">
    ///     <para xml:lang="en">The minimum number of elite victories required.</para>
    ///     <para xml:lang="zh-CN">所需的最少精英战胜利数。</para>
    /// </param>
    /// <param name="Description">
    ///     <para xml:lang="en">A human-readable label used in logs.</para>
    ///     <para xml:lang="zh-CN">日志中使用的可读标签。</para>
    /// </param>
    public sealed record EliteEpochUnlockRule(
        ModelId CharacterId,
        string EpochId,
        int RequiredEliteWins,
        string Description)
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a rule after validating its inputs.</para>
        ///     <para xml:lang="zh-CN">验证输入后创建规则。</para>
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
    ///     <para xml:lang="en">Describes a character-specific counted-victory rule, used for boss epochs.</para>
    ///     <para xml:lang="zh-CN">描述按角色统计胜利次数的规则，用于首领纪元。</para>
    /// </summary>
    /// <param name="CharacterId">
    ///     <para xml:lang="en">The ID of the character whose victories are counted.</para>
    ///     <para xml:lang="zh-CN">要统计胜利次数的角色 ID。</para>
    /// </param>
    /// <param name="EpochId">
    ///     <para xml:lang="en">The ID of the epoch to obtain.</para>
    ///     <para xml:lang="zh-CN">要获得的纪元 ID。</para>
    /// </param>
    /// <param name="RequiredWins">
    ///     <para xml:lang="en">The minimum number of victories required.</para>
    ///     <para xml:lang="zh-CN">所需的最少胜利次数。</para>
    /// </param>
    /// <param name="Description">
    ///     <para xml:lang="en">A human-readable label used in logs.</para>
    ///     <para xml:lang="zh-CN">日志中使用的可读标签。</para>
    /// </param>
    public sealed record CountedEpochUnlockRule(
        ModelId CharacterId,
        string EpochId,
        int RequiredWins,
        string Description)
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a rule after validating its inputs.</para>
        ///     <para xml:lang="zh-CN">验证输入后创建规则。</para>
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
