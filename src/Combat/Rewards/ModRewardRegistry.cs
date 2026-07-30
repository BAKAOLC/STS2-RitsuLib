using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Content;
using STS2RitsuLib.Utils;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.Combat.Rewards
{
    /// <summary>
    ///     Per-mod registration surface for custom reward types. Prefer <see cref="RegisterOwned" /> so ids follow
    ///     the same <c>MODID_REWARD_LOCAL</c> convention as other RitsuLib dynamic ids.
    ///     自定义 reward type 的逐 mod 注册入口。优先使用 <see cref="RegisterOwned" />，使 id 遵循与其它
    ///     RitsuLib 动态 id 相同的 <c>MODID_REWARD_LOCAL</c> 约定。
    /// </summary>
    public sealed class ModRewardRegistry
    {
        /// <summary>
        ///     Factory used to rebuild a custom reward from a saved reward and optional mod-owned JSON payload.
        ///     用保存的 reward 与可选的 mod JSON 载荷重建自定义 reward 的工厂。
        /// </summary>
        public delegate Reward ModRewardFactory(SerializableReward save, Player player, string? json);

        /// <summary>
        ///     Factory used to rebuild a custom reward from a saved reward and a typed mod-owned payload.
        ///     用保存的 reward 和已解析的 mod 载荷重建自定义 reward 的工厂。
        /// </summary>
        // ReSharper disable once TypeParameterCanBeVariant
        public delegate Reward ModRewardFactory<TPayload>(
            SerializableReward save,
            Player player,
            TPayload? payload);

        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModRewardRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ModRewardDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<RewardType, ModRewardDefinition> DefinitionsByRewardType = [];
        private static readonly Dictionary<RewardType, RewardRegistration> RegistrationsByType = [];
        private readonly Logger _logger;
        private readonly string _modId;

        private ModRewardRegistry(string modId)
        {
            _modId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

        /// <summary>
        ///     Returns the singleton registry for <paramref name="modId" />, creating it on first use.
        ///     返回 <paramref name="modId" /> 对应的单例注册表，首次使用时创建。
        /// </summary>
        public static ModRewardRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var existing))
                    return existing;

                var created = new ModRewardRegistry(modId);
                Registries[modId] = created;
                return created;
            }
        }

        /// <summary>
        ///     Registers a reward owned by this registry's mod using
        ///     <see cref="ModContentRegistry.GetQualifiedRewardId" />.
        ///     使用 <see cref="ModContentRegistry.GetQualifiedRewardId" /> 生成归属当前 mod 的 reward id。
        /// </summary>
        public ModRewardDefinition RegisterOwned(string localRewardStem, ModRewardFactory factory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localRewardStem);
            ArgumentNullException.ThrowIfNull(factory);

            var id = ModContentRegistry.GetQualifiedRewardId(_modId, localRewardStem);
            return RegisterCore(_modId, id, factory);
        }

        /// <summary>
        ///     Registers a reward owned by this registry's mod and lets RitsuLib parse the mod-owned JSON payload
        ///     with a source-generated JSON contract before calling <paramref name="factory" />.
        ///     注册归属当前 mod 的 reward。读档时，RitsuLib 会先用传入的 JSON 协定解析载荷，
        ///     再调用 <paramref name="factory" />。
        /// </summary>
        public ModRewardDefinition RegisterOwned<TPayload>(
            string localRewardStem,
            JsonTypeInfo<TPayload> jsonTypeInfo,
            ModRewardFactory<TPayload> factory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localRewardStem);
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);
            ArgumentNullException.ThrowIfNull(factory);

            return RegisterOwned(localRewardStem,
                (save, player, json) => factory(save, player, DeserializePayload(json, jsonTypeInfo)));
        }

        /// <summary>
        ///     Registers a reward with a raw global id. Prefer <see cref="RegisterOwned" /> for mod-scoped ids.
        ///     使用原始全局 id 注册 reward。mod 作用域 id 推荐优先使用 <see cref="RegisterOwned" />。
        /// </summary>
        public static ModRewardDefinition Register(string id, ModRewardFactory factory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(factory);

            return RegisterCore(string.Empty, id, factory);
        }

        /// <summary>
        ///     Registers a reward with a raw global id and lets RitsuLib parse the mod-owned JSON payload with a
        ///     source-generated JSON contract before calling <paramref name="factory" />.
        ///     使用原始全局 id 注册 reward。读档时，RitsuLib 会先用传入的 JSON 协定解析 mod 载荷，
        ///     再调用 <paramref name="factory" />。
        /// </summary>
        public static ModRewardDefinition Register<TPayload>(
            string id,
            JsonTypeInfo<TPayload> jsonTypeInfo,
            ModRewardFactory<TPayload> factory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);
            ArgumentNullException.ThrowIfNull(factory);

            return Register(id,
                (save, player, json) => factory(save, player, DeserializePayload(json, jsonTypeInfo)));
        }

        /// <summary>
        ///     Registers or replaces a custom reward factory for an already defined <see cref="RewardType" />.
        ///     为已经定义好的 <see cref="RewardType" /> 注册或替换自定义 reward 工厂。
        /// </summary>
        public static void Register(RewardType rewardType, ModRewardFactory factory)
        {
            ArgumentNullException.ThrowIfNull(factory);

            lock (SyncRoot)
            {
                RegistrationsByType[rewardType] = new(null, rewardType, factory);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get a registered reward definition by ID.</para>
        ///     <para xml:lang="zh-CN">尝试按 ID 获取已注册的奖励定义。</para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">The reward ID to resolve.</para>
        ///     <para xml:lang="zh-CN">要解析的奖励 ID。</para>
        /// </param>
        /// <param name="definition">
        ///     <para xml:lang="en">The registered definition when found.</para>
        ///     <para xml:lang="zh-CN">找到时返回已注册的定义。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the reward is registered; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">奖励已注册时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryGet(string id, out ModRewardDefinition definition)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            lock (SyncRoot)
            {
                return Definitions.TryGetValue(NormalizeId(id), out definition!);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a registered reward definition by ID.</para>
        ///     <para xml:lang="zh-CN">按 ID 获取已注册的奖励定义。</para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">The reward ID to resolve.</para>
        ///     <para xml:lang="zh-CN">要解析的奖励 ID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The registered reward definition.</para>
        ///     <para xml:lang="zh-CN">已注册的奖励定义。</para>
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        ///     <para xml:lang="en">No reward is registered under <paramref name="id" />.</para>
        ///     <para xml:lang="zh-CN">没有使用 <paramref name="id" /> 注册的奖励。</para>
        /// </exception>
        public static ModRewardDefinition Get(string id)
        {
            return TryGet(id, out var definition)
                ? definition
                : throw new KeyNotFoundException($"Reward '{NormalizeId(id)}' is not registered.");
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get a registered reward definition by <see cref="RewardType" />.</para>
        ///     <para xml:lang="zh-CN">尝试按 <see cref="RewardType" /> 获取已注册的奖励定义。</para>
        /// </summary>
        /// <param name="rewardType">
        ///     <para xml:lang="en">The dynamic reward type to resolve.</para>
        ///     <para xml:lang="zh-CN">要解析的动态奖励类型。</para>
        /// </param>
        /// <param name="definition">
        ///     <para xml:lang="en">The registered definition when found.</para>
        ///     <para xml:lang="zh-CN">找到时返回已注册的定义。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the reward type is registered; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">奖励类型已注册时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryGetByRewardType(RewardType rewardType, out ModRewardDefinition definition)
        {
            lock (SyncRoot)
            {
                return DefinitionsByRewardType.TryGetValue(rewardType, out definition!);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a registered reward definition by <see cref="RewardType" />.</para>
        ///     <para xml:lang="zh-CN">按 <see cref="RewardType" /> 获取已注册的奖励定义。</para>
        /// </summary>
        /// <param name="rewardType">
        ///     <para xml:lang="en">The dynamic reward type to resolve.</para>
        ///     <para xml:lang="zh-CN">要解析的动态奖励类型。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The registered reward definition.</para>
        ///     <para xml:lang="zh-CN">已注册的奖励定义。</para>
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        ///     <para xml:lang="en"><paramref name="rewardType" /> is not a registered mod reward.</para>
        ///     <para xml:lang="zh-CN"><paramref name="rewardType" /> 不是已注册的模组奖励。</para>
        /// </exception>
        public static ModRewardDefinition Get(RewardType rewardType)
        {
            return TryGetByRewardType(rewardType, out var definition)
                ? definition
                : throw new KeyNotFoundException(
                    $"RewardType '0x{(int)rewardType:X8}' is not a registered mod reward.");
        }

        /// <summary>
        ///     Returns the deterministic dynamic <see cref="RewardType" /> for a registered or raw reward id.
        ///     返回已注册或原始 reward id 对应的确定性动态 <see cref="RewardType" />。
        /// </summary>
        public static RewardType GetRewardType(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            var normalized = NormalizeId(id);
            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(normalized, out var definition))
                    return definition.RewardType;
            }

            return DynamicEnumValueRegistry<RewardType>.GetValueWithMintKey(normalized, GetMintKey(normalized));
        }

        /// <summary>
        ///     Returns the deterministic dynamic <see cref="RewardType" /> for a registered or raw reward id without
        ///     failing on hash collisions. Unknown ids are computed but not registered.
        ///     返回已注册或原始 reward id 对应的确定性动态 <see cref="RewardType" />，且不会因哈希碰撞失败。
        ///     未知 ID 只计算值，不会注册。
        /// </summary>
        public static RewardType GetRewardTypeIgnoringCollisions(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            var normalized = NormalizeId(id);
            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(normalized, out var definition))
                    return definition.RewardType;
            }

            return DynamicEnumValueRegistry<RewardType>
                .GetValueWithMintKeyIgnoringCollisions(normalized, GetMintKey(normalized));
        }

        /// <summary>
        ///     Resolves the reward id that minted <paramref name="rewardType" />, if any.
        ///     解析生成 <paramref name="rewardType" /> 的 reward id，如果存在。
        /// </summary>
        public static bool TryGetId(RewardType rewardType, out string id)
        {
            lock (SyncRoot)
            {
                if (DefinitionsByRewardType.TryGetValue(rewardType, out var definition))
                {
                    id = definition.Id;
                    return true;
                }
            }

            id = string.Empty;
            return false;
        }

        /// <summary>
        ///     Resolves which mod registered <paramref name="id" />, if any.
        ///     解析 <c>id</c> 是由哪个 mod 注册的（如果存在）。
        /// </summary>
        public static bool TryGetOwnerModId(string id, out string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(NormalizeId(id), out var definition)
                    && !string.IsNullOrEmpty(definition.ModId))
                {
                    modId = definition.ModId;
                    return true;
                }
            }

            modId = string.Empty;
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a snapshot of all registered reward definitions, ordered by ID.</para>
        ///     <para xml:lang="zh-CN">获取所有已注册奖励定义的快照，并按 ID 排序。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">A new array containing the registered definitions in ordinal ID order.</para>
        ///     <para xml:lang="zh-CN">按 ID 序数顺序包含已注册定义的新数组。</para>
        /// </returns>
        public static ModRewardDefinition[] GetDefinitionsSnapshot()
        {
            lock (SyncRoot)
            {
                return
                [
                    .. Definitions.Values
                        .OrderBy(definition => definition.Id, StringComparer.Ordinal),
                ];
            }
        }

        internal static bool TryCreate(
            RewardType rewardType,
            SerializableReward save,
            Player player,
            string? json,
            out Reward? reward)
        {
            RewardRegistration? registration;
            lock (SyncRoot)
            {
                RegistrationsByType.TryGetValue(rewardType, out registration);
            }

            if (registration == null)
            {
                reward = null;
                return false;
            }

            reward = registration.Factory(save, player, json)
                     ?? throw new InvalidOperationException(
                         $"The custom reward factory for RewardType '0x{(int)rewardType:X8}' returned null.");
            return true;
        }

        private static ModRewardDefinition RegisterCore(string modId, string id, ModRewardFactory factory)
        {
            var normalized = NormalizeId(id);
            var rewardType = DynamicEnumValueRegistry<RewardType>
                .RegisterWithMintKey(modId, normalized, GetMintKey(normalized))
                .Value;
            var definition = new ModRewardDefinition(modId, normalized, rewardType);
            ModRewardRegistry? registry;

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(normalized, out var existing))
                {
                    if (!string.Equals(existing.ModId, definition.ModId, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            $"Reward '{normalized}' is already registered by mod '{existing.ModId}'; "
                            + $"mod '{definition.ModId}' cannot re-register it.");

                    RegistrationsByType[existing.RewardType] = new(existing.Id, existing.RewardType, factory);
                    return existing;
                }

                Definitions[normalized] = definition;
                DefinitionsByRewardType[rewardType] = definition;
                RegistrationsByType[rewardType] = new(normalized, rewardType, factory);
                Registries.TryGetValue(modId, out registry);
            }

            if (!string.IsNullOrEmpty(modId) && registry != null)
                registry._logger.Info($"[Rewards] Registered reward: {normalized} (RewardType=0x{(int)rewardType:X8})");

            return definition;
        }

        private static string NormalizeId(string id)
        {
            return id.Trim();
        }

        private static string GetMintKey(string normalizedId)
        {
            return $"reward:{normalizedId}";
        }

        private static TPayload? DeserializePayload<TPayload>(
            string? json,
            JsonTypeInfo<TPayload> jsonTypeInfo)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;

            try
            {
                return JsonSerializer.Deserialize(json, jsonTypeInfo);
            }
            catch (JsonException ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RitsuLib] Custom reward payload JSON deserialize failed: {ex.Message}");
                return default;
            }
            catch (NotSupportedException ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RitsuLib] Custom reward payload JSON deserialize not supported: {ex.Message}");
                return default;
            }
        }

        private sealed record RewardRegistration(
            string? RewardId,
            RewardType RewardType,
            ModRewardFactory Factory);
    }
}
