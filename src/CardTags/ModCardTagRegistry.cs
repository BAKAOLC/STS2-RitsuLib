using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Utils;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.CardTags
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers custom card tags for one mod and resolves their definitions and dynamic
    ///         <see cref="CardTag" /> values.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为单个模组注册自定义卡牌标签，并解析其定义与动态 <see cref="CardTag" /> 值。
    ///     </para>
    /// </summary>
    public sealed class ModCardTagRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModCardTagRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ModCardTagDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<CardTag, ModCardTagDefinition> DefinitionsByCardTag = [];

        private readonly Logger _logger;
        private readonly string _modId;
        private string? _freezeReason;

        private ModCardTagRegistry(string modId)
        {
            _modId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets whether card-tag registration has been frozen.</para>
        ///     <para xml:lang="zh-CN">获取卡牌标签注册是否已冻结。</para>
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the registry for <paramref name="modId" />, creating it on first use.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="modId" /> 的注册表，并在首次使用时创建。</para>
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
        ///     <para xml:lang="en">
        ///         Registers a card tag owned by this registry's mod using a qualified ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用限定 ID 注册由此注册表所属模组拥有的卡牌标签。</para>
        /// </summary>
        public ModCardTagDefinition RegisterOwned(string localTagStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localTagStem);

            var id = ModContentRegistry.GetQualifiedCardTagId(_modId, localTagStem);
            return RegisterCore(id);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a card tag with a global ID. Prefer <see cref="RegisterOwned" /> for mod-qualified
        ///         IDs.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用全局 ID 注册卡牌标签。需要模组限定 ID 时优先使用 <see cref="RegisterOwned" />。
        ///     </para>
        /// </summary>
        public ModCardTagDefinition Register(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            return RegisterCore(id);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get the ID of the mod that registered <paramref name="tagId" />.</para>
        ///     <para xml:lang="zh-CN">尝试获取注册 <paramref name="tagId" /> 的模组 ID。</para>
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
        ///     <para xml:lang="en">Tries to get a registered card-tag definition by ID.</para>
        ///     <para xml:lang="zh-CN">尝试按 ID 获取已注册的卡牌标签定义。</para>
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
        ///     <para xml:lang="en">Gets the registered card-tag definition for <paramref name="id" />.</para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="id" /> 的已注册卡牌标签定义。</para>
        /// </summary>
        public static ModCardTagDefinition Get(string id)
        {
            return TryGet(id, out var definition)
                ? definition
                : throw new KeyNotFoundException($"Card tag '{NormalizeId(id)}' is not registered.");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the registered mod card-tag definition represented by a <see cref="CardTag" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <see cref="CardTag" /> 所表示的已注册模组卡牌标签定义。
        ///     </para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The dynamic card-tag value to resolve.</para>
        ///     <para xml:lang="zh-CN">要解析的动态卡牌标签值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The registered mod card-tag definition.</para>
        ///     <para xml:lang="zh-CN">已注册的模组卡牌标签定义。</para>
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        ///     <para xml:lang="en"><paramref name="value" /> is not a registered mod card tag.</para>
        ///     <para xml:lang="zh-CN"><paramref name="value" /> 不是已注册的模组卡牌标签。</para>
        /// </exception>
        public static ModCardTagDefinition Get(CardTag value)
        {
            return TryGetByCardTag(value, out var definition)
                ? definition
                : throw new KeyNotFoundException(
                    $"CardTag '0x{(int)value:X8}' is not a registered mod card tag.");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get the registered definition represented by <paramref name="value" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取 <paramref name="value" /> 所表示的已注册定义。
        ///     </para>
        /// </summary>
        public static bool TryGetByCardTag(CardTag value, out ModCardTagDefinition definition)
        {
            lock (SyncRoot)
            {
                return DefinitionsByCardTag.TryGetValue(value, out definition!);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether <paramref name="value" /> represents a registered mod card tag.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="value" /> 是否表示已注册的模组卡牌标签。
        ///     </para>
        /// </summary>
        public static bool IsModCardTag(CardTag value)
        {
            lock (SyncRoot)
            {
                return DefinitionsByCardTag.ContainsKey(value);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to return the deterministic <see cref="CardTag" /> for <paramref name="id" />. The ID
        ///         does not need to be registered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试返回 <paramref name="id" /> 对应的确定性 <see cref="CardTag" />。该 ID 无需已注册。
        ///     </para>
        /// </summary>
        public static bool TryGetCardTag(string id, out CardTag value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            try
            {
                value = DynamicEnumValueRegistry<CardTag>.GetValue(id);
                return true;
            }
            catch (InvalidOperationException)
            {
                value = CardTag.None;
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to resolve a registered tag ID, vanilla <see cref="CardTag" /> name, or deterministic
        ///         dynamic value. Registered tag IDs take precedence.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试解析已注册标签 ID、原版 <see cref="CardTag" /> 名称或确定性动态值。已注册标签 ID 优先。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Returns the deterministic <see cref="CardTag" /> for <paramref name="id" />. The ID does
        ///         not need to be registered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="id" /> 对应的确定性 <see cref="CardTag" />。该 ID 无需已注册。
        ///     </para>
        /// </summary>
        public static CardTag GetCardTag(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return DynamicEnumValueRegistry<CardTag>.GetValue(id);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get the registered tag ID represented by <paramref name="value" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取 <paramref name="value" /> 所表示的已注册标签 ID。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Returns a snapshot of all registered card-tag definitions ordered by ID using ordinal
        ///         comparison.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回所有已注册卡牌标签定义的快照，并按 ID 使用序号比较排序。
        ///     </para>
        /// </summary>
        public static ModCardTagDefinition[] GetDefinitionsSnapshot()
        {
            lock (SyncRoot)
            {
                return
                [
                    .. Definitions.Values
                        .OrderBy(def => def.Id, StringComparer.Ordinal),
                ];
            }
        }

        private ModCardTagDefinition RegisterCore(string id)
        {
            EnsureMutable("register card tags");

            var normalizedId = NormalizeId(id);
            var cardTagValue = DynamicEnumValueRegistry<CardTag>.Register(_modId, normalizedId).Value;
            var definition = new ModCardTagDefinition(_modId, normalizedId, cardTagValue);

            lock (SyncRoot)
            {
                EnsureMutable("register card tags");

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
