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
        private static readonly DynamicEnumValueMinter<RewardType> RewardTypeMinter = new();

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

            return RewardTypeMinter.Mint($"reward:{normalized}");
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

            reward = registration.Factory(save, player, json);
            return true;
        }

        private static ModRewardDefinition RegisterCore(string modId, string id, ModRewardFactory factory)
        {
            var normalized = NormalizeId(id);
            var rewardType = RewardTypeMinter.Mint($"reward:{normalized}");
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
