using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Deterministically mints 32-bit integer values for string ids and casts them into the target
    ///     <typeparamref name="TEnum" />. Intended for safely extending vanilla enums (such as
    ///     <c>CardKeyword</c>, <c>CardPile</c>, <c>CardTag</c>, etc.) with mod-defined members without ever colliding
    ///     with low vanilla enum members: minted values are forced into a reserved high-value band above
    ///     <see cref="ReservedFloor" />, leaving the low range untouched for vanilla.
    ///     为字符串 id 确定性地铸造 32 位整数值，并将其转换为目标
    ///     <typeparamref name="TEnum" />。用于安全扩展原版枚举（例如
    ///     <c>CardKeyword</c>、<c>CardPile</c>、<c>CardTag</c> 等）的 mod 定义成员，且不会与
    ///     低位原版枚举成员冲突：铸造值会被强制放入高于
    ///     <see cref="ReservedFloor" /> 的保留高值区间，低值范围保留给原版。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Values are computed as <c>ReservedFloor + (XxHash32(utf8(id)) mod (int.MaxValue - ReservedFloor + 1))</c>,
    ///         yielding stable, cross-process, cross-run identical values for the same input id. The hash itself
    ///         is well-distributed, but collision protection between different mod ids is out of scope per project
    ///         convention; duplicate-id registration is rejected explicitly.
    ///     </para>
    ///     <para>
    ///         Only enums whose underlying storage is 32-bit (<c>int</c> or <c>uint</c>) are supported; larger or
    ///         smaller backings are rejected in the constructor. This matches how the vanilla multiplayer writer
    ///         treats enums (<c>PacketWriter.WriteEnum</c> passes them through <c>Convert.ToInt32</c>).
    ///     </para>
    ///     <para>
    ///         值按 <c>ReservedFloor + (XxHash32(utf8(id)) mod (int.MaxValue - ReservedFloor + 1))</c> 计算，
    ///         因此同一输入 id 会得到跨进程、跨跑局稳定相同的值。哈希本身
    ///         分布良好，但根据项目约定，不同 mod id 之间的碰撞保护不在范围内；
    ///         重复 id 注册会被显式拒绝。
    ///     </para>
    ///     <para>
    ///         仅支持底层存储为 32 位（<c>int</c> 或 <c>uint</c>）的枚举；更大或
    ///         更小的底层类型会在构造函数中被拒绝。这与原版多人写入器处理
    ///         枚举的方式一致（<c>PacketWriter.WriteEnum</c> 会通过 <c>Convert.ToInt32</c> 传递它们）。
    ///     </para>
    /// </remarks>
    public sealed class DynamicEnumValueMinter<TEnum> where TEnum : struct, Enum
    {
        /// <summary>
        ///     Default reserved floor. Minted values land in <c>[0x4000_0000, 0x7FFF_FFFF]</c>, which is safely
        ///     above any plausible future vanilla enum growth while remaining positive <see cref="int" />.
        ///     默认保留下界。铸造值落在 <c>[0x4000_0000, 0x7FFF_FFFF]</c>，该范围安全地
        ///     高于未来原版枚举可能增长到的值，同时仍保持为正 <see cref="int" />。
        /// </summary>
        public const int DefaultReservedFloor = 0x4000_0000;

        private readonly Dictionary<string, TEnum> _byId = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<TEnum, string> _byValue = [];
        private readonly Lock _sync = new();

        /// <summary>
        ///     Creates a minter using <see cref="DefaultReservedFloor" />.
        ///     使用 <see cref="DefaultReservedFloor" /> 创建铸造器。
        /// </summary>
        public DynamicEnumValueMinter() : this(DefaultReservedFloor)
        {
        }

        /// <summary>
        ///     Creates a minter whose produced integer values are always <c>&gt;= <paramref name="reservedFloor" /></c>,
        ///     so every minted value sits strictly above any low vanilla enum member.
        ///     创建一个铸造器，其生成的整数值始终 <c>&gt;= <paramref name="reservedFloor" /></c>，
        ///     因此每个铸造值都严格位于任何低位原版枚举成员之上。
        /// </summary>
        /// <param name="reservedFloor">
        ///     Lower bound (inclusive) for minted values. Must be <c>&gt;= 0</c>. Values in <c>[0, reservedFloor)</c>
        ///     are left for vanilla enum members.
        ///     铸造值的下界（含）。必须为 <c>&gt;= 0</c>。<c>[0, reservedFloor)</c> 中的值
        ///     保留给原版枚举成员。
        /// </param>
        public DynamicEnumValueMinter(int reservedFloor)
        {
            if (Unsafe.SizeOf<TEnum>() != sizeof(int))
                throw new NotSupportedException(
                    $"DynamicEnumValueMinter only supports 32-bit backed enums; '{typeof(TEnum).FullName}' is "
                    + $"{Unsafe.SizeOf<TEnum>() * 8}-bit.");

            if (reservedFloor < 0)
                throw new ArgumentOutOfRangeException(nameof(reservedFloor),
                    "Reserved floor must be non-negative.");

            ReservedFloor = reservedFloor;
        }

        /// <summary>
        ///     Lower bound (inclusive) for all minted values; vanilla enum members below this value never collide.
        ///     所有铸造值的下界（含）；低于此值的原版枚举成员永不碰撞。
        /// </summary>
        public int ReservedFloor { get; }

        /// <summary>
        ///     Returns the <typeparamref name="TEnum" /> value for <paramref name="id" />, registering it on first
        ///     call. Subsequent calls with the same id return the same value; ids are compared case-insensitively.
        ///     返回 <paramref name="id" /> 对应的 <typeparamref name="TEnum" /> 值，并在首次
        ///     调用时注册它。之后使用同一 id 调用会返回相同值；id 比较不区分大小写。
        /// </summary>
        /// <exception cref="ArgumentException">When <paramref name="id" /> is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">
        ///     When two distinct ids hash to the same <typeparamref name="TEnum" /> value (registration-time
        ///     当 two distinct ids hash to the same <c>TEnum</c> value (注册-time
        ///     collision detection; callers are expected to pick non-colliding ids).
        ///     中文说明：collision detection; callers are expected to pick non-colliding ids).
        /// </exception>
        public TEnum Mint(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            var normalized = Normalize(id);

            lock (_sync)
            {
                if (_byId.TryGetValue(normalized, out var existing))
                    return existing;

                var value = Compute(normalized);

                if (_byValue.TryGetValue(value, out var conflict))
                    throw new InvalidOperationException(
                        $"DynamicEnumValueMinter<{typeof(TEnum).Name}> hash collision: "
                        + $"'{normalized}' and '{conflict}' both map to the same numeric value. "
                        + "Change one of the ids to resolve the clash.");

                _byId[normalized] = value;
                _byValue[value] = normalized;
                return value;
            }
        }

        /// <summary>
        ///     Computes the deterministic <typeparamref name="TEnum" /> value for <paramref name="id" /> without
        ///     registering it or checking whether another id already owns the same numeric value.
        ///     计算 <paramref name="id" /> 对应的确定性 <typeparamref name="TEnum" /> 值，但不注册，
        ///     也不检查是否已有其它 ID 占用了相同数值。
        /// </summary>
        /// <remarks>
        ///     Use this only when a caller explicitly needs the raw value that <see cref="Mint" /> would have
        ///     produced before collision validation. Values returned by this method are not visible to
        ///     <see cref="TryGetId" /> or <see cref="IsDynamic" /> unless they were minted through <see cref="Mint" />.
        ///     仅当调用方明确需要 <see cref="Mint" /> 在碰撞校验前本应生成的原始值时使用。此方法返回的值
        ///     不会出现在 <see cref="TryGetId" /> 或 <see cref="IsDynamic" /> 中，除非它们也通过
        ///     <see cref="Mint" /> 生成过。
        /// </remarks>
        public TEnum ComputeValue(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return Compute(Normalize(id));
        }

        /// <summary>
        ///     Attempts to resolve the string id that minted <paramref name="value" />.
        ///     尝试解析铸造出 <paramref name="value" /> 的字符串 id。
        /// </summary>
        /// <returns>
        ///     <c>true</c> when <paramref name="value" /> was produced by an earlier <see cref="Mint" /> call.
        ///     当 <paramref name="value" /> 由先前的 <see cref="Mint" /> 调用生成时为 <c>true</c>。
        /// </returns>
        public bool TryGetId(TEnum value, out string id)
        {
            lock (_sync)
            {
                if (_byValue.TryGetValue(value, out var found))
                {
                    id = found;
                    return true;
                }
            }

            id = string.Empty;
            return false;
        }

        /// <summary>
        ///     Returns the <typeparamref name="TEnum" /> value currently bound to <paramref name="id" /> without
        ///     registering a new one.
        ///     返回当前绑定到 <paramref name="id" /> 的 <typeparamref name="TEnum" /> 值，不会
        ///     注册新值。
        /// </summary>
        public bool TryGetValue(string id, out TEnum value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            var normalized = Normalize(id);

            lock (_sync)
            {
                return _byId.TryGetValue(normalized, out value);
            }
        }

        /// <summary>
        ///     Whether <paramref name="value" /> was minted by this registry (i.e. represents a registered dynamic
        ///     member rather than a vanilla enum literal).
        ///     <paramref name="value" /> 是否由此注册表铸造（即表示已注册的动态
        ///     成员，而不是原版枚举字面值）。
        /// </summary>
        public bool IsDynamic(TEnum value)
        {
            lock (_sync)
            {
                return _byValue.ContainsKey(value);
            }
        }

        internal (string Id, TEnum Value)[] GetMintedValuesSnapshot()
        {
            lock (_sync)
            {
                return
                [
                    .. _byId
                        .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                        .Select(static pair => (pair.Key, pair.Value)),
                ];
            }
        }

        private TEnum Compute(string normalizedId)
        {
            var bytes = Encoding.UTF8.GetBytes(normalizedId);
            var hash = XxHash32.HashToUInt32(bytes);

            var floor = (uint)ReservedFloor;
            var range = int.MaxValue - floor + 1u;
            var raw = unchecked((int)(floor + hash % range));
            return Unsafe.As<int, TEnum>(ref raw);
        }

        private static string Normalize(string id)
        {
            return id.Trim().ToLowerInvariant();
        }
    }
}
