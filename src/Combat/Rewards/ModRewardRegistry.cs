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
    ///     <para xml:lang="en">
    ///         Registers custom reward types for a mod. Prefer <see cref="RegisterOwned" /> so reward IDs follow the
    ///         same <c>MODID_REWARD_LOCAL</c> convention as other RitsuLib dynamic IDs.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为模组注册自定义奖励类型。建议使用 <see cref="RegisterOwned" />，使奖励 ID 与其他 RitsuLib
    ///         动态 ID 一样遵循 <c>MODID_REWARD_LOCAL</c> 约定。
    ///     </para>
    /// </summary>
    public sealed class ModRewardRegistry
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Rebuilds a custom reward from saved reward data and an optional mod-owned JSON payload.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         根据已保存的奖励数据和可选的模组 JSON 载荷重建自定义奖励。
        ///     </para>
        /// </summary>
        /// <param name="save">
        ///     <para xml:lang="en">The base game's saved reward data.</para>
        ///     <para xml:lang="zh-CN">原版保存的奖励数据。</para>
        /// </param>
        /// <param name="player">
        ///     <para xml:lang="en">The player who owns the reward.</para>
        ///     <para xml:lang="zh-CN">拥有该奖励的玩家。</para>
        /// </param>
        /// <param name="json">
        ///     <para xml:lang="en">The optional mod-owned JSON payload.</para>
        ///     <para xml:lang="zh-CN">由模组维护的可选 JSON 载荷。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The rebuilt reward. The factory must not return <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN">重建后的奖励。工厂不得返回 <see langword="null" />。</para>
        /// </returns>
        public delegate Reward ModRewardFactory(SerializableReward save, Player player, string? json);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Rebuilds a custom reward from saved reward data and a deserialized mod-owned payload.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         根据已保存的奖励数据和反序列化后的模组载荷重建自定义奖励。
        ///     </para>
        /// </summary>
        /// <typeparam name="TPayload">
        ///     <para xml:lang="en">The payload type.</para>
        ///     <para xml:lang="zh-CN">载荷类型。</para>
        /// </typeparam>
        /// <param name="save">
        ///     <para xml:lang="en">The base game's saved reward data.</para>
        ///     <para xml:lang="zh-CN">原版保存的奖励数据。</para>
        /// </param>
        /// <param name="player">
        ///     <para xml:lang="en">The player who owns the reward.</para>
        ///     <para xml:lang="zh-CN">拥有该奖励的玩家。</para>
        /// </param>
        /// <param name="payload">
        ///     <para xml:lang="en">
        ///         The deserialized payload, or its default value when no payload was saved.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         反序列化后的载荷；未保存载荷时为该类型的默认值。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The rebuilt reward. The factory must not return <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN">重建后的奖励。工厂不得返回 <see langword="null" />。</para>
        /// </returns>
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
        ///     <para xml:lang="en">
        ///         Gets the singleton registry for <paramref name="modId" />, creating it on first use.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="modId" /> 的单例注册表；首次使用时创建。
        ///     </para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The owning mod ID.</para>
        ///     <para xml:lang="zh-CN">所属模组的 ID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The registry for <paramref name="modId" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="modId" /> 的注册表。</para>
        /// </returns>
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
        ///     <para xml:lang="en">
        ///         Registers a reward owned by this registry's mod, using
        ///         <see cref="ModContentRegistry.GetQualifiedRewardId" /> to create its qualified ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册归属当前注册表模组的奖励，并使用 <see cref="ModContentRegistry.GetQualifiedRewardId" />
        ///         创建其限定 ID。
        ///     </para>
        /// </summary>
        /// <param name="localRewardStem">
        ///     <para xml:lang="en">The mod-local reward ID stem.</para>
        ///     <para xml:lang="zh-CN">模组内使用的奖励 ID 主体。</para>
        /// </param>
        /// <param name="factory">
        ///     <para xml:lang="en">The factory used to restore the reward.</para>
        ///     <para xml:lang="zh-CN">用于恢复奖励的工厂。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The registered reward definition.</para>
        ///     <para xml:lang="zh-CN">已注册的奖励定义。</para>
        /// </returns>
        public ModRewardDefinition RegisterOwned(string localRewardStem, ModRewardFactory factory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localRewardStem);
            ArgumentNullException.ThrowIfNull(factory);

            var id = ModContentRegistry.GetQualifiedRewardId(_modId, localRewardStem);
            return RegisterCore(_modId, id, factory);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a reward owned by this registry's mod. Before calling <paramref name="factory" />,
        ///         RitsuLib deserializes the mod-owned payload with source-generated JSON metadata.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册归属当前注册表模组的奖励。调用 <paramref name="factory" /> 前，RitsuLib 会使用源生成的
        ///         JSON 元数据反序列化由模组维护的载荷。
        ///     </para>
        /// </summary>
        /// <typeparam name="TPayload">
        ///     <para xml:lang="en">The payload type.</para>
        ///     <para xml:lang="zh-CN">载荷类型。</para>
        /// </typeparam>
        /// <param name="localRewardStem">
        ///     <para xml:lang="en">The mod-local reward ID stem.</para>
        ///     <para xml:lang="zh-CN">模组内使用的奖励 ID 主体。</para>
        /// </param>
        /// <param name="jsonTypeInfo">
        ///     <para xml:lang="en">The source-generated JSON metadata for <typeparamref name="TPayload" />.</para>
        ///     <para xml:lang="zh-CN"><typeparamref name="TPayload" /> 的源生成 JSON 元数据。</para>
        /// </param>
        /// <param name="factory">
        ///     <para xml:lang="en">The factory used to restore the reward.</para>
        ///     <para xml:lang="zh-CN">用于恢复奖励的工厂。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The registered reward definition.</para>
        ///     <para xml:lang="zh-CN">已注册的奖励定义。</para>
        /// </returns>
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
        ///     <para xml:lang="en">
        ///         Registers a reward under a raw global ID. Prefer <see cref="RegisterOwned" /> for mod-owned IDs.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用原始全局 ID 注册奖励。模组自有 ID 应优先使用 <see cref="RegisterOwned" />。
        ///     </para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">The global reward ID.</para>
        ///     <para xml:lang="zh-CN">全局奖励 ID。</para>
        /// </param>
        /// <param name="factory">
        ///     <para xml:lang="en">The factory used to restore the reward.</para>
        ///     <para xml:lang="zh-CN">用于恢复奖励的工厂。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The registered reward definition.</para>
        ///     <para xml:lang="zh-CN">已注册的奖励定义。</para>
        /// </returns>
        public static ModRewardDefinition Register(string id, ModRewardFactory factory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(factory);

            return RegisterCore(string.Empty, id, factory);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a reward under a raw global ID. Before calling <paramref name="factory" />, RitsuLib
        ///         deserializes the mod-owned payload with source-generated JSON metadata.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用原始全局 ID 注册奖励。调用 <paramref name="factory" /> 前，RitsuLib 会使用源生成的
        ///         JSON 元数据反序列化由模组维护的载荷。
        ///     </para>
        /// </summary>
        /// <typeparam name="TPayload">
        ///     <para xml:lang="en">The payload type.</para>
        ///     <para xml:lang="zh-CN">载荷类型。</para>
        /// </typeparam>
        /// <param name="id">
        ///     <para xml:lang="en">The global reward ID.</para>
        ///     <para xml:lang="zh-CN">全局奖励 ID。</para>
        /// </param>
        /// <param name="jsonTypeInfo">
        ///     <para xml:lang="en">The source-generated JSON metadata for <typeparamref name="TPayload" />.</para>
        ///     <para xml:lang="zh-CN"><typeparamref name="TPayload" /> 的源生成 JSON 元数据。</para>
        /// </param>
        /// <param name="factory">
        ///     <para xml:lang="en">The factory used to restore the reward.</para>
        ///     <para xml:lang="zh-CN">用于恢复奖励的工厂。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The registered reward definition.</para>
        ///     <para xml:lang="zh-CN">已注册的奖励定义。</para>
        /// </returns>
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
        ///     <para xml:lang="en">
        ///         Registers or replaces the custom reward factory for an existing <see cref="RewardType" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为已有的 <see cref="RewardType" /> 注册或替换自定义奖励工厂。
        ///     </para>
        /// </summary>
        /// <param name="rewardType">
        ///     <para xml:lang="en">The reward type handled by the factory.</para>
        ///     <para xml:lang="zh-CN">由该工厂处理的奖励类型。</para>
        /// </param>
        /// <param name="factory">
        ///     <para xml:lang="en">The factory used to restore the reward.</para>
        ///     <para xml:lang="zh-CN">用于恢复奖励的工厂。</para>
        /// </param>
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
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when the reward type is registered; otherwise, <see langword="false" />
        ///         .
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Gets the deterministic dynamic <see cref="RewardType" /> for a registered or raw reward ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已注册或原始奖励 ID 对应的确定性动态 <see cref="RewardType" />。
        ///     </para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">The registered or raw reward ID.</para>
        ///     <para xml:lang="zh-CN">已注册或原始奖励 ID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The dynamic reward type derived from <paramref name="id" />.</para>
        ///     <para xml:lang="zh-CN">根据 <paramref name="id" /> 派生的动态奖励类型。</para>
        /// </returns>
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
        ///     <para xml:lang="en">
        ///         Gets the deterministic dynamic <see cref="RewardType" /> for a registered or raw reward ID without
        ///         rejecting hash collisions. Unknown IDs are computed but not registered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已注册或原始奖励 ID 对应的确定性动态 <see cref="RewardType" />，且不因哈希碰撞而失败。
        ///         未知 ID 只会计算其值，不会注册。
        ///     </para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">The registered or raw reward ID.</para>
        ///     <para xml:lang="zh-CN">已注册或原始奖励 ID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The dynamic reward type derived from <paramref name="id" />.</para>
        ///     <para xml:lang="zh-CN">根据 <paramref name="id" /> 派生的动态奖励类型。</para>
        /// </returns>
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
        ///     <para xml:lang="en">
        ///         Tries to get the reward ID that minted <paramref name="rewardType" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取生成 <paramref name="rewardType" /> 的奖励 ID。
        ///     </para>
        /// </summary>
        /// <param name="rewardType">
        ///     <para xml:lang="en">The reward type to resolve.</para>
        ///     <para xml:lang="zh-CN">要解析的奖励类型。</para>
        /// </param>
        /// <param name="id">
        ///     <para xml:lang="en">The registered reward ID when found.</para>
        ///     <para xml:lang="zh-CN">找到时返回已注册的奖励 ID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when the reward type belongs to a registered definition; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         该奖励类型属于已注册定义时为 <see langword="true" />；否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
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
        ///     <para xml:lang="en">Tries to get the ID of the mod that registered <paramref name="id" />.</para>
        ///     <para xml:lang="zh-CN">尝试获取注册 <paramref name="id" /> 的模组 ID。</para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">The registered reward ID.</para>
        ///     <para xml:lang="zh-CN">已注册的奖励 ID。</para>
        /// </param>
        /// <param name="modId">
        ///     <para xml:lang="en">The owning mod ID when found.</para>
        ///     <para xml:lang="zh-CN">找到时返回所属模组的 ID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when the reward has a non-empty owner ID; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         该奖励具有非空所属模组 ID 时为 <see langword="true" />；否则为
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
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
                throw;
            }
            catch (NotSupportedException ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RitsuLib] Custom reward payload JSON deserialize not supported: {ex.Message}");
                throw;
            }
        }

        private sealed record RewardRegistration(
            string? RewardId,
            RewardType RewardType,
            ModRewardFactory Factory);
    }
}
