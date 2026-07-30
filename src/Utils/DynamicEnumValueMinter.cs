using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     <para xml:lang="en">Deterministically mints 32-bit integer values for string IDs and casts them into <typeparamref name="TEnum" />. Minted values occupy the reserved high-value band at or above <see cref="ReservedFloor" />, leaving the low range for vanilla enum members.</para>
    ///     <para xml:lang="zh-CN">为字符串 ID 确定性地生成 32 位整数值并将其转换为 <typeparamref name="TEnum" />。生成值位于不低于 <see cref="ReservedFloor" /> 的保留高值区间，低值范围留给原版枚举成员。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">Values use <c>ReservedFloor + (XxHash32(utf8(id)) mod (int.MaxValue - ReservedFloor + 1))</c>, so the same ID has a stable value across processes and runs. Recording a distinct ID that collides with an existing one is rejected.</para>
    ///     <para xml:lang="zh-CN">值使用 <c>ReservedFloor + (XxHash32(utf8(id)) mod (int.MaxValue - ReservedFloor + 1))</c> 计算，因此同一 ID 在不同进程和运行中保持稳定。登记不同 ID 时若与现有 ID 发生哈希碰撞，该操作会被拒绝。</para>
    ///     <para xml:lang="en">Only 32-bit-backed <c>int</c> or <c>uint</c> enums are supported.</para>
    ///     <para xml:lang="zh-CN">仅支持底层类型为 32 位 <c>int</c> 或 <c>uint</c> 的枚举。</para>
    /// </remarks>
    public sealed class DynamicEnumValueMinter<TEnum> where TEnum : struct, Enum
    {
        /// <summary>
        ///     <para xml:lang="en">Default reserved floor. Minted values land in <c>[0x4000_0000, 0x7FFF_FFFF]</c>, reserving the lower positive range for vanilla enum members.</para>
        ///     <para xml:lang="zh-CN">默认保留下界。生成值落在 <c>[0x4000_0000, 0x7FFF_FFFF]</c>，为原版枚举成员保留较低的正值范围。</para>
        /// </summary>
        public const int DefaultReservedFloor = 0x4000_0000;

        private readonly Dictionary<string, TEnum> _byId = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<TEnum, string> _byValue = [];
        private readonly Lock _sync = new();

        /// <summary>
        ///     <para xml:lang="en">Creates a minter using <see cref="DefaultReservedFloor" />.</para>
        ///     <para xml:lang="zh-CN">使用 <see cref="DefaultReservedFloor" /> 创建生成器。</para>
        /// </summary>
        public DynamicEnumValueMinter() : this(DefaultReservedFloor)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a minter whose integer values are always <c>&gt;= <paramref name="reservedFloor" /></c>.</para>
        ///     <para xml:lang="zh-CN">创建一个生成器，其整数值始终 <c>&gt;= <paramref name="reservedFloor" /></c>。</para>
        /// </summary>
        /// <param name="reservedFloor">
        ///     <para xml:lang="en">Inclusive lower bound for minted values. It must be <c>&gt;= 0</c>; <c>[0, reservedFloor)</c> remains available to vanilla enum members.</para>
        ///     <para xml:lang="zh-CN">生成值的包含下界。它必须为 <c>&gt;= 0</c>；<c>[0, reservedFloor)</c> 留给原版枚举成员。</para>
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
        ///     <para xml:lang="en">Inclusive lower bound for all minted values; vanilla members below it never collide.</para>
        ///     <para xml:lang="zh-CN">所有生成值的包含下界；低于该值的原版成员永不碰撞。</para>
        /// </summary>
        public int ReservedFloor { get; }

        /// <summary>
        ///     <para xml:lang="en">Returns the <typeparamref name="TEnum" /> value for <paramref name="id" />, recording it on first call. Subsequent case-insensitive uses of the same ID return the same value.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="id" /> 对应的 <typeparamref name="TEnum" /> 值，并在首次调用时登记。之后以不区分大小写的相同 ID 调用会返回同一值。</para>
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en"><paramref name="id" /> is null or whitespace.</para>
        ///     <para xml:lang="zh-CN"><paramref name="id" /> 为 null 或空白。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">Two distinct IDs hash to the same <typeparamref name="TEnum" /> value; collision detection occurs while recording the ID, and callers must choose non-colliding IDs.</para>
        ///     <para xml:lang="zh-CN">两个不同 ID 哈希到同一个 <typeparamref name="TEnum" /> 值；登记 ID 时会检测此碰撞，调用方必须改用不冲突的 ID。</para>
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
        ///     <para xml:lang="en">Computes the deterministic <typeparamref name="TEnum" /> value for <paramref name="id" /> without registering it or checking whether another ID already owns the numeric value.</para>
        ///     <para xml:lang="zh-CN">计算 <paramref name="id" /> 的确定性 <typeparamref name="TEnum" /> 值，但不注册，也不检查其他 ID 是否已占用该数值。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">Use this only when the raw pre-validation value is required. Its result is not visible to <see cref="TryGetId" /> or <see cref="IsDynamic" /> unless <see cref="Mint" /> also records it.</para>
        ///     <para xml:lang="zh-CN">仅在需要碰撞校验前的原始值时使用。除非也通过 <see cref="Mint" /> 登记，否则结果不会出现在 <see cref="TryGetId" /> 或 <see cref="IsDynamic" /> 中。</para>
        /// </remarks>
        public TEnum ComputeValue(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return Compute(Normalize(id));
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to resolve the string ID that minted <paramref name="value" />.</para>
        ///     <para xml:lang="zh-CN">尝试解析生成 <paramref name="value" /> 的字符串 ID。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><c>true</c> when <paramref name="value" /> was produced by an earlier <see cref="Mint" /> call.</para>
        ///     <para xml:lang="zh-CN"><paramref name="value" /> 由先前的 <see cref="Mint" /> 调用生成时为 <c>true</c>。</para>
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
        ///     <para xml:lang="en">Returns the <typeparamref name="TEnum" /> value currently bound to <paramref name="id" /> without registering a new one.</para>
        ///     <para xml:lang="zh-CN">返回当前绑定到 <paramref name="id" /> 的 <typeparamref name="TEnum" /> 值，不注册新值。</para>
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
        ///     <para xml:lang="en">Whether <paramref name="value" /> is present in this minter's reverse-lookup table.</para>
        ///     <para xml:lang="zh-CN"><paramref name="value" /> 是否存在于此生成器的反向查找表中。</para>
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
