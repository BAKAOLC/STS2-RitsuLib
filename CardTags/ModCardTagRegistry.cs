using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Utils;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.CardTags
{
    /// <summary>
    ///     Per-mod registration surface for custom <see cref="CardTag" /> values. Ids follow
    ///     <see cref="ModContentRegistry.GetQualifiedCardTagId" />; numeric values are minted with
    ///     <see cref="DynamicEnumValueMinter{TEnum}" /> in the same reserved band as keywords and card piles.
    ///     自定义 <see cref="CardTag" /> 值的逐 mod 注册入口。ID 遵循
    ///     <see cref="ModContentRegistry.GetQualifiedCardTagId" />；数值会通过
    ///     <see cref="DynamicEnumValueMinter{TEnum}" /> 在与关键词和卡牌牌堆相同的保留区间内生成。
    /// </summary>
    public sealed class ModCardTagRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModCardTagRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ModCardTagDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<CardTag, ModCardTagDefinition> DefinitionsByCardTag = [];

        private static readonly DynamicEnumValueMinter<CardTag> CardTagMinter = new();

        private readonly Logger _logger;
        private readonly string _modId;
        private string? _freezeReason;

        private ModCardTagRegistry(string modId)
        {
            _modId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

        /// <summary>
        ///     True after the framework freezes tag registration (at <c>ModelDb.Init</c>).
        ///     框架冻结标签注册后为 true（发生在 <c>ModelDb.Init</c>）。
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     Returns the singleton registry for <paramref name="modId" />, creating it on first use.
        ///     返回 <paramref name="modId" /> 对应的单例注册表，并在首次使用时创建。
        /// </summary>
        public static ModCardTagRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var existing))
                    return existing;

                var created = new ModCardTagRegistry(modId);
                Registries[modId] = created;
                return created;
            }
        }

        internal static void FreezeRegistrations(string reason)
        {
            ModCardTagRegistry[] snapshot;
            lock (SyncRoot)
            {
                if (IsFrozen)
                    return;

                IsFrozen = true;
                foreach (var registry in Registries.Values)
                    registry._freezeReason = reason;

                snapshot = [.. Registries.Values];
            }

            foreach (var registry in snapshot)
                registry._logger.Info($"[CardTags] Card tag registration is now frozen ({reason}).");
        }

        /// <summary>
        ///     Registers a tag owned by this registry’s mod using <see cref="ModContentRegistry.GetQualifiedCardTagId" />.
        ///     使用 <see cref="ModContentRegistry.GetQualifiedCardTagId" /> 注册归属此注册表 mod 的标签。
        /// </summary>
        public ModCardTagDefinition RegisterOwned(string localTagStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localTagStem);

            var id = ModContentRegistry.GetQualifiedCardTagId(_modId, localTagStem);
            return RegisterCore(id);
        }

        /// <summary>
        ///     Registers a tag with a raw global id. Prefer <see cref="RegisterOwned" /> for mod-scoped ids.
        ///     使用原始全局 ID 注册标签。mod 作用域 ID 推荐优先使用 <see cref="RegisterOwned" />。
        /// </summary>
        public ModCardTagDefinition Register(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            return RegisterCore(id);
        }

        /// <summary>
        ///     Resolves which mod registered <paramref name="tagId" />, if any.
        ///     解析 <paramref name="tagId" /> 是由哪个 mod 注册的（如果存在）。
        /// </summary>
        public static bool TryGetOwnerModId(string tagId, out string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tagId);

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(NormalizeId(tagId), out var def))
                {
                    modId = def.ModId;
                    return true;
                }
            }

            modId = string.Empty;
            return false;
        }

        /// <summary>
        ///     Tries to resolve a definition by qualified or raw id.
        ///     尝试通过限定 ID 或原始 ID 解析定义。
        /// </summary>
        public static bool TryGet(string id, out ModCardTagDefinition definition)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            lock (SyncRoot)
            {
                return Definitions.TryGetValue(NormalizeId(id), out definition!);
            }
        }

        /// <summary>
        ///     Returns the definition for <paramref name="id" /> or throws <see cref="KeyNotFoundException" />.
        ///     返回 <paramref name="id" /> 对应的定义；不存在时抛出 <see cref="KeyNotFoundException" />。
        /// </summary>
        public static ModCardTagDefinition Get(string id)
        {
            return TryGet(id, out var definition)
                ? definition
                : throw new KeyNotFoundException($"Card tag '{NormalizeId(id)}' is not registered.");
        }

        /// <summary>
        ///     Reverse lookup for a minted <see cref="CardTag" /> value.
        ///     对已生成的 <see cref="CardTag" /> 值执行反向查找。
        /// </summary>
        public static bool TryGetByCardTag(CardTag value, out ModCardTagDefinition definition)
        {
            lock (SyncRoot)
            {
                return DefinitionsByCardTag.TryGetValue(value, out definition!);
            }
        }

        /// <summary>
        ///     Whether <paramref name="value" /> was minted by this registry (not a vanilla literal).
        ///     判断 <paramref name="value" /> 是否由此注册表生成（而不是原版字面值）。
        /// </summary>
        public static bool IsModCardTag(CardTag value)
        {
            lock (SyncRoot)
            {
                return DefinitionsByCardTag.ContainsKey(value);
            }
        }

        /// <summary>
        ///     Resolves the deterministic <see cref="CardTag" /> value minted for <paramref name="id" />.
        ///     The id does not need to be registered.
        ///     解析为 <paramref name="id" /> 确定性 minted 的 <see cref="CardTag" /> 值。
        ///     该 id 不需要已注册。
        /// </summary>
        public static bool TryGetCardTag(string id, out CardTag value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            try
            {
                value = CardTagMinter.Mint(id);
                return true;
            }
            catch (InvalidOperationException)
            {
                value = CardTag.None;
                return false;
            }
        }

        /// <summary>
        ///     Resolves either a registered mod card-tag id or a vanilla <see cref="CardTag" /> enum name.
        ///     Mod ids take precedence when a string could match both.
        ///     解析已注册的 mod 卡牌标签 ID 或原版 <see cref="CardTag" /> 枚举名。
        ///     当字符串同时可能匹配两者时，mod ID 优先。
        /// </summary>
        public static bool TryResolveCardTag(string idOrEnumName, out CardTag value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(idOrEnumName);

            if (!TryGet(idOrEnumName, out var definition))
                return Enum.TryParse(idOrEnumName.Trim(), true, out value) || TryGetCardTag(idOrEnumName, out value);
            value = definition.CardTagValue;
            return true;
        }

        /// <summary>
        ///     Returns the deterministic <see cref="CardTag" /> minted for <paramref name="id" />.
        ///     The id does not need to be registered.
        ///     返回为 <paramref name="id" /> 确定性 minted 的 <see cref="CardTag" />。
        ///     该 id 不需要已注册。
        /// </summary>
        public static CardTag GetCardTag(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return CardTagMinter.Mint(id);
        }

        /// <summary>
        ///     Tries to resolve the string id that minted <paramref name="value" />.
        ///     尝试解析生成 <paramref name="value" /> 的字符串 ID。
        /// </summary>
        public static bool TryGetId(CardTag value, out string id)
        {
            lock (SyncRoot)
            {
                if (DefinitionsByCardTag.TryGetValue(value, out var def))
                {
                    id = def.Id;
                    return true;
                }
            }

            id = string.Empty;
            return false;
        }

        /// <summary>
        ///     Snapshot of all registered card-tag definitions, stable-ordered by id.
        ///     获取所有已注册卡牌标签定义的快照，并按 ID 稳定排序。
        /// </summary>
        public static ModCardTagDefinition[] GetDefinitionsSnapshot()
        {
            lock (SyncRoot)
            {
                return Definitions.Values
                    .OrderBy(def => def.Id, StringComparer.Ordinal)
                    .ToArray();
            }
        }

        private ModCardTagDefinition RegisterCore(string id)
        {
            EnsureMutable("register card tags");

            var normalizedId = NormalizeId(id);
            var cardTagValue = CardTagMinter.Mint(normalizedId);
            var definition = new ModCardTagDefinition(_modId, normalizedId, cardTagValue);

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(normalizedId, out var existing))
                {
                    if (!StringComparer.OrdinalIgnoreCase.Equals(existing.ModId, definition.ModId))
                        throw new InvalidOperationException(
                            $"Card tag '{normalizedId}' is already registered by mod '{existing.ModId}'; "
                            + $"mod '{definition.ModId}' cannot re-register it.");

                    return existing;
                }

                Definitions[normalizedId] = definition;
                DefinitionsByCardTag[cardTagValue] = definition;
            }

            _logger.Info($"[CardTags] Registered tag: {normalizedId} (CardTag=0x{(int)cardTagValue:X8})");
            return definition;
        }

        private void EnsureMutable(string operation)
        {
            if (!IsFrozen)
                return;

            throw new InvalidOperationException(
                $"Cannot {operation} after card tag registration has been frozen ({_freezeReason ?? "unknown"}). "
                + "Register tags from your mod initializer before model initialization.");
        }

        private static string NormalizeId(string id)
        {
            return id.Trim();
        }
    }
}
