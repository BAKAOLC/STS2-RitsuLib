using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Rewards;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Process-wide registration surface for mod-owned dynamic enum values. It gives mods a single allocation path
    ///     for enum values minted from stable string ids, while keeping owner validation and reverse lookups centralized.
    ///     面向 mod 所有动态枚举值的进程级注册入口。它为从稳定字符串 ID 生成的枚举值提供统一分配路径，
    ///     并集中处理归属校验与反向查找。
    /// </summary>
    /// <typeparam name="TEnum">
    ///     The 32-bit-backed enum type to extend.
    ///     要扩展的 32 位底层枚举类型。
    /// </typeparam>
    /// <remarks>
    ///     <para>
    ///         Prefer type-specific registries when they exist, such as <c>ModCardTagRegistry</c>, because those
    ///         registries may attach metadata or lifecycle rules. Use this generic registry for enum extension points
    ///         that only need a stable value and owner-aware validation.
    ///     </para>
    ///     <para>
    ///         存在类型专用注册器时应优先使用它们，例如 <c>ModCardTagRegistry</c>，因为这些注册器可能附加元数据或生命周期规则。
    ///         此通用注册器适用于只需要稳定枚举值和归属校验的扩展点。
    ///     </para>
    /// </remarks>
    public static class DynamicEnumValueRegistry<TEnum> where TEnum : struct, Enum
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Lock SyncRoot = new();
        private static readonly DynamicEnumValueMinter<TEnum> Minter = new();

        private static readonly Dictionary<string, ModDynamicEnumValueRegistry<TEnum>> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, DynamicEnumValueDefinition<TEnum>> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<TEnum, DynamicEnumValueDefinition<TEnum>> DefinitionsByValue = [];

        // ReSharper disable once StaticMemberInGenericType
        private static string CategoryStem { get; } = ResolveCategoryStem();

        /// <summary>
        ///     Returns the per-mod facade for <paramref name="modId" />, creating it on first use.
        ///     返回 <paramref name="modId" /> 对应的逐 mod facade，并在首次使用时创建。
        /// </summary>
        public static ModDynamicEnumValueRegistry<TEnum> For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var existing))
                    return existing;

                var created = new ModDynamicEnumValueRegistry<TEnum>(modId);
                Registries[modId] = created;
                return created;
            }
        }

        /// <summary>
        ///     Registers a mod-owned value using the enum type's configured category segment.
        ///     使用此枚举类型配置的 category 段注册一个 mod 所有的值。
        /// </summary>
        public static DynamicEnumValueDefinition<TEnum> RegisterOwned(string modId, string localStem)
        {
            var id = GetOwnedId(modId, localStem);
            return Register(modId, id);
        }

        /// <summary>
        ///     Builds the canonical owned id for <paramref name="localStem" />.
        ///     为 <paramref name="localStem" /> 构建规范 owned ID。
        /// </summary>
        public static string GetOwnedId(string modId, string localStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localStem);

            return ModContentRegistry.GetCompoundId(modId, CategoryStem, localStem);
        }

        /// <summary>
        ///     Registers a mod-owned value with an already qualified id.
        ///     使用已经限定好的 ID 注册一个 mod 所有的值。
        /// </summary>
        public static DynamicEnumValueDefinition<TEnum> Register(string modId, string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return RegisterWithMintKey(modId, id, NormalizeId(id));
        }

        internal static DynamicEnumValueDefinition<TEnum> RegisterWithMintKey(string modId, string id, string mintKey)
        {
            ArgumentNullException.ThrowIfNull(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(mintKey);

            var normalizedId = NormalizeId(id);
            var value = Minter.Mint(mintKey);
            var definition = new DynamicEnumValueDefinition<TEnum>(modId.Trim(), normalizedId, value);

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(normalizedId, out var existing))
                {
                    if (!StringComparer.OrdinalIgnoreCase.Equals(existing.ModId, definition.ModId))
                        throw new InvalidOperationException(
                            $"Dynamic enum value '{normalizedId}' for {typeof(TEnum).Name} is already registered by "
                            + $"mod '{existing.ModId}'; mod '{definition.ModId}' cannot re-register it.");

                    return existing;
                }

                Definitions[normalizedId] = definition;
                DefinitionsByValue[value] = definition;
            }

            return definition;
        }

        /// <summary>
        ///     Tries to resolve a registered definition by id. This does not mint a new value.
        ///     尝试按 ID 解析已注册定义。此方法不会生成新值。
        /// </summary>
        public static bool TryGet(string id, out DynamicEnumValueDefinition<TEnum> definition)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            lock (SyncRoot)
            {
                return Definitions.TryGetValue(NormalizeId(id), out definition!);
            }
        }

        /// <summary>
        ///     Returns the registered definition for <paramref name="id" /> or throws.
        ///     返回 <paramref name="id" /> 对应的已注册定义；不存在时抛出异常。
        /// </summary>
        public static DynamicEnumValueDefinition<TEnum> Get(string id)
        {
            return TryGet(id, out var definition)
                ? definition
                : throw new KeyNotFoundException(
                    $"Dynamic enum value '{NormalizeId(id)}' for {typeof(TEnum).Name} is not registered.");
        }

        /// <summary>
        ///     Resolves which mod registered <paramref name="id" />, if any.
        ///     解析 <paramref name="id" /> 是由哪个 mod 注册的（如果存在）。
        /// </summary>
        public static bool TryGetOwnerModId(string id, out string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(NormalizeId(id), out var definition))
                {
                    modId = definition.ModId;
                    return true;
                }
            }

            modId = string.Empty;
            return false;
        }

        /// <summary>
        ///     Resolves a registered definition by dynamic enum value.
        ///     通过动态枚举值解析已注册定义。
        /// </summary>
        public static bool TryGetByValue(TEnum value, out DynamicEnumValueDefinition<TEnum> definition)
        {
            lock (SyncRoot)
            {
                return DefinitionsByValue.TryGetValue(value, out definition!);
            }
        }

        /// <summary>
        ///     Whether <paramref name="value" /> is registered through this central registry.
        ///     <paramref name="value" /> 是否通过此中心注册器注册。
        /// </summary>
        public static bool IsRegistered(TEnum value)
        {
            lock (SyncRoot)
            {
                return DefinitionsByValue.ContainsKey(value);
            }
        }

        /// <summary>
        ///     Returns the deterministic enum value for <paramref name="id" />. The id does not need to be registered.
        ///     Prefer <see cref="Register" /> or <see cref="RegisterOwned" /> when allocating new public extension values.
        ///     返回 <paramref name="id" /> 对应的确定性枚举值。该 ID 不需要已注册。
        ///     分配新的公开扩展值时优先使用 <see cref="Register" /> 或 <see cref="RegisterOwned" />。
        /// </summary>
        public static TEnum GetValue(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return GetValueWithMintKey(id, NormalizeId(id));
        }

        /// <summary>
        ///     Returns the deterministic enum value for <paramref name="id" /> without failing on hash collisions.
        ///     The value is computed but not registered when the id is unknown.
        ///     返回 <paramref name="id" /> 对应的确定性枚举值，且不会因哈希碰撞失败。ID 未知时只计算值，
        ///     不会注册。
        /// </summary>
        /// <remarks>
        ///     This is an explicit escape hatch for diagnostics and compatibility code that must recover the raw value
        ///     an id would have produced even after another id has already minted the same numeric value.
        ///     这是供诊断与兼容代码使用的显式旁路：即使已有其它 ID 生成了相同数值，也能取回该 ID 本应生成的原始值。
        /// </remarks>
        public static TEnum GetValueIgnoringCollisions(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return GetValueWithMintKeyIgnoringCollisions(id, NormalizeId(id));
        }

        internal static TEnum GetValueWithMintKey(string id, string mintKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(mintKey);

            var normalizedId = NormalizeId(id);
            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(normalizedId, out var definition))
                    return definition.Value;
            }

            return Minter.Mint(mintKey);
        }

        internal static TEnum GetValueWithMintKeyIgnoringCollisions(string id, string mintKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(mintKey);

            var normalizedId = NormalizeId(id);
            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(normalizedId, out var definition))
                    return definition.Value;
            }

            return Minter.ComputeValue(mintKey);
        }

        /// <summary>
        ///     Tries to return the deterministic enum value for <paramref name="id" />.
        ///     尝试返回 <paramref name="id" /> 对应的确定性枚举值。
        /// </summary>
        public static bool TryGetValue(string id, out TEnum value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            try
            {
                value = GetValue(id);
                return true;
            }
            catch (InvalidOperationException)
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        ///     Resolves either a registered dynamic id or a vanilla enum name. Dynamic ids take precedence.
        ///     解析已注册动态 ID 或原版枚举名。动态 ID 优先。
        /// </summary>
        public static bool TryResolve(string idOrEnumName, out TEnum value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(idOrEnumName);

            if (!TryGet(idOrEnumName, out var definition))
                return Enum.TryParse(idOrEnumName.Trim(), true, out value) || TryGetValue(idOrEnumName, out value);
            value = definition.Value;
            return true;
        }

        /// <summary>
        ///     Tries to resolve the registered id for <paramref name="value" />.
        ///     尝试解析 <paramref name="value" /> 对应的已注册 ID。
        /// </summary>
        public static bool TryGetId(TEnum value, out string id)
        {
            lock (SyncRoot)
            {
                if (DefinitionsByValue.TryGetValue(value, out var definition))
                {
                    id = definition.Id;
                    return true;
                }
            }

            id = string.Empty;
            return false;
        }

        /// <summary>
        ///     Tries to resolve the id that minted <paramref name="value" />, including values minted by
        ///     <see cref="GetValue" /> without registration.
        ///     尝试解析生成 <paramref name="value" /> 的 ID，包括通过 <see cref="GetValue" /> 生成但未注册的值。
        /// </summary>
        public static bool TryGetMintedId(TEnum value, out string id)
        {
            return Minter.TryGetId(value, out id);
        }

        /// <summary>
        ///     Whether <paramref name="value" /> was minted by this central registry.
        ///     <paramref name="value" /> 是否由此中心注册器生成。
        /// </summary>
        public static bool IsMinted(TEnum value)
        {
            return Minter.IsDynamic(value);
        }

        internal static (string Id, TEnum Value)[] GetMintedValuesSnapshot()
        {
            return Minter.GetMintedValuesSnapshot();
        }

        /// <summary>
        ///     Snapshot of all registered dynamic enum definitions, stable-ordered by id.
        ///     获取所有已注册动态枚举定义的快照，并按 ID 稳定排序。
        /// </summary>
        public static DynamicEnumValueDefinition<TEnum>[] GetDefinitionsSnapshot()
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

        private static string NormalizeId(string id)
        {
            return id.Trim();
        }

        // ReSharper disable ConvertIfStatementToReturnStatement
        private static string ResolveCategoryStem()
        {
            var enumType = typeof(TEnum);

            if (enumType == typeof(CardKeyword))
                return "KEYWORD";

            if (enumType == typeof(PileType))
                return "CARDPILE";

            if (enumType == typeof(CardTag))
                return "CARDTAG";

            if (enumType == typeof(RewardType))
                return "REWARD";

            if (enumType == typeof(TargetType))
                return "TARGETTYPE";

            return ModContentRegistry.NormalizePublicStem(enumType.Name);
        }
        // ReSharper restore ConvertIfStatementToReturnStatement
    }
}
