using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Rewards;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Process-wide registration surface for mod-owned dynamic enum values, centralizing deterministic
    ///         value allocation, ownership validation, and reverse lookup.
    ///     </para>
    ///     <para xml:lang="zh-CN">面向模组归属动态枚举值的进程级注册入口，集中处理确定性数值分配、归属校验和反向查找。</para>
    /// </summary>
    /// <typeparam name="TEnum">
    ///     <para xml:lang="en">The 32-bit-backed enum type to extend.</para>
    ///     <para xml:lang="zh-CN">要扩展的 32 位底层枚举类型。</para>
    /// </typeparam>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Prefer a type-specific registry such as <c>ModCardTagRegistry</c> when available, because it
    ///         may add metadata or lifecycle rules. Use this generic registry only for extension points that need a stable
    ///         value and ownership validation.
    ///     </para>
    ///     <para xml:lang="zh-CN">存在类型专用注册表时应优先使用，例如 <c>ModCardTagRegistry</c>，因为它可能附加元数据或生命周期规则。仅需要稳定值和归属校验的扩展点才使用此通用注册表。</para>
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
        ///     <para xml:lang="en">Returns the per-mod facade for <paramref name="modId" />, creating it on first use.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="modId" /> 的逐模组门面，并在首次使用时创建。</para>
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
        ///     <para xml:lang="en">Registers a mod-owned value using the enum type's configured category segment.</para>
        ///     <para xml:lang="zh-CN">使用该枚举类型配置的类别段注册模组归属值。</para>
        /// </summary>
        public static DynamicEnumValueDefinition<TEnum> RegisterOwned(string modId, string localStem)
        {
            var id = GetOwnedId(modId, localStem);
            return Register(modId, id);
        }

        /// <summary>
        ///     <para xml:lang="en">Builds the canonical owned ID for <paramref name="localStem" />.</para>
        ///     <para xml:lang="zh-CN">为 <paramref name="localStem" /> 构建规范的归属 ID。</para>
        /// </summary>
        public static string GetOwnedId(string modId, string localStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localStem);

            return ModContentRegistry.GetCompoundId(modId, CategoryStem, localStem);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a mod-owned value with an already qualified ID.</para>
        ///     <para xml:lang="zh-CN">使用已限定的 ID 注册模组归属值。</para>
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
        ///     <para xml:lang="en">Tries to resolve a registered definition by ID without minting a new value.</para>
        ///     <para xml:lang="zh-CN">尝试按 ID 解析已注册定义，不会生成新值。</para>
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
        ///     <para xml:lang="en">Returns the registered definition for <paramref name="id" /> or throws.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="id" /> 对应的已注册定义；不存在时抛出异常。</para>
        /// </summary>
        public static DynamicEnumValueDefinition<TEnum> Get(string id)
        {
            return TryGet(id, out var definition)
                ? definition
                : throw new KeyNotFoundException(
                    $"Dynamic enum value '{NormalizeId(id)}' for {typeof(TEnum).Name} is not registered.");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the registered dynamic-enum definition represented by <paramref name="value" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="value" /> 所表示的已注册动态枚举定义。
        ///     </para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The dynamic enum value to resolve.</para>
        ///     <para xml:lang="zh-CN">要解析的动态枚举值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The registered dynamic-enum definition.</para>
        ///     <para xml:lang="zh-CN">已注册的动态枚举定义。</para>
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        ///     <para xml:lang="en"><paramref name="value" /> is not registered for <typeparamref name="TEnum" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="value" /> 未在 <typeparamref name="TEnum" /> 注册表中注册。</para>
        /// </exception>
        public static DynamicEnumValueDefinition<TEnum> Get(TEnum value)
        {
            return TryGetByValue(value, out var definition)
                ? definition
                : throw new KeyNotFoundException(
                    $"{typeof(TEnum).Name} value '{value}' is not registered.");
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves which mod registered <paramref name="id" />, if any.</para>
        ///     <para xml:lang="zh-CN">解析 <paramref name="id" /> 由哪个模组注册（如果存在）。</para>
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
        ///     <para xml:lang="en">Resolves a registered definition by dynamic enum value.</para>
        ///     <para xml:lang="zh-CN">通过动态枚举值解析已注册定义。</para>
        /// </summary>
        public static bool TryGetByValue(TEnum value, out DynamicEnumValueDefinition<TEnum> definition)
        {
            lock (SyncRoot)
            {
                return DefinitionsByValue.TryGetValue(value, out definition!);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Whether <paramref name="value" /> is registered through this central registry.</para>
        ///     <para xml:lang="zh-CN"><paramref name="value" /> 是否通过此中央注册表注册。</para>
        /// </summary>
        public static bool IsRegistered(TEnum value)
        {
            lock (SyncRoot)
            {
                return DefinitionsByValue.ContainsKey(value);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the deterministic enum value for <paramref name="id" /> without registering a
        ///         definition. Unknown IDs are recorded by the minter; prefer <see cref="Register" /> or
        ///         <see cref="RegisterOwned" /> for new public extension values.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="id" /> 的确定性枚举值，但不注册定义。未知 ID 会由生成器登记；分配新的公开扩展值时优先使用
        ///         <see cref="Register" /> 或 <see cref="RegisterOwned" />。
        ///     </para>
        /// </summary>
        public static TEnum GetValue(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return GetValueWithMintKey(id, NormalizeId(id));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the deterministic enum value for <paramref name="id" /> without failing on hash
        ///         collisions. An unknown ID is computed without registering a definition or adding a minter reverse lookup.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="id" /> 的确定性枚举值，且不会因哈希碰撞失败。未知 ID 只计算值，不注册定义，也不加入生成器的反向查找。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         This is an explicit escape hatch for diagnostics and compatibility code that must recover the
        ///         raw value an ID would have produced after another ID minted the same numeric value.
        ///     </para>
        ///     <para xml:lang="zh-CN">这是面向诊断和兼容代码的显式旁路：即使其他 ID 已生成同一数值，也可取回该 ID 本应生成的原始值。</para>
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
        ///     <para xml:lang="en">Tries to return or mint the deterministic enum value for <paramref name="id" />.</para>
        ///     <para xml:lang="zh-CN">尝试返回或生成 <paramref name="id" /> 的确定性枚举值。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="false" /> only when an unknown ID collides with an ID already recorded by the
        ///         minter.
        ///     </para>
        ///     <para xml:lang="zh-CN">仅当未知 ID 与生成器已登记的 ID 发生哈希碰撞时返回 <see langword="false" />。</para>
        /// </returns>
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
        ///     <para xml:lang="en">
        ///         Resolves a registered dynamic ID, a vanilla enum name or numeric literal, or an otherwise
        ///         unknown ID by minting its deterministic value, in that order.
        ///     </para>
        ///     <para xml:lang="zh-CN">依次尝试将输入解析为已注册动态 ID、原版枚举名称或数字字面量；若均不匹配，则为未知 ID 生成确定性值。</para>
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
        ///     <para xml:lang="en">Tries to resolve the registered ID for <paramref name="value" />.</para>
        ///     <para xml:lang="zh-CN">尝试解析 <paramref name="value" /> 对应的已注册 ID。</para>
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
        ///     <para xml:lang="en">
        ///         Tries to resolve the normalized mint key that produced <paramref name="value" />, including
        ///         values minted by <see cref="GetValue" /> without registration.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试解析生成 <paramref name="value" /> 的规范化生成键，包括通过 <see cref="GetValue" /> 生成但未注册的值。</para>
        /// </summary>
        public static bool TryGetMintedId(TEnum value, out string id)
        {
            return Minter.TryGetId(value, out id);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Whether <paramref name="value" /> is recorded by this registry's minter, including values
        ///         minted by <see cref="GetValue" /> without a registered definition.
        ///     </para>
        ///     <para xml:lang="zh-CN"><paramref name="value" /> 是否已由此注册表的生成器登记，包括通过 <see cref="GetValue" /> 生成但未注册定义的值。</para>
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
        ///     <para xml:lang="en">Snapshot of all registered dynamic enum definitions, stably ordered by ID.</para>
        ///     <para xml:lang="zh-CN">所有已注册动态枚举定义的快照，按 ID 稳定排序。</para>
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
