using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Deterministically mints 32-bit integer values for string ids and casts them into the target
    ///     <typeparamref name="TEnum" />. Intended for safely extending vanilla enums (such as
    ///     <c>CardKeyword</c>, a future <c>CardPile</c>, etc.) with mod-defined members without ever colliding
    ///     with low vanilla enum members: minted values are forced into a reserved high-value band above
    ///     <see cref="ReservedFloor" />, leaving the low range untouched for vanilla.
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
    /// </remarks>
    public sealed class DynamicEnumValueMinter<TEnum> where TEnum : struct, Enum
    {
        /// <summary>
        ///     Default reserved floor. Minted values land in <c>[0x4000_0000, 0x7FFF_FFFF]</c>, which is safely
        ///     above any plausible future vanilla enum growth while remaining positive <see cref="int" />.
        /// </summary>
        public const int DefaultReservedFloor = 0x4000_0000;

        private readonly Dictionary<string, TEnum> _byId = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<TEnum, string> _byValue = [];
        private readonly Lock _sync = new();

        /// <summary>
        ///     Creates a minter using <see cref="DefaultReservedFloor" />.
        /// </summary>
        public DynamicEnumValueMinter() : this(DefaultReservedFloor)
        {
        }

        /// <summary>
        ///     Creates a minter whose produced integer values are always <c>&gt;= <paramref name="reservedFloor" /></c>,
        ///     so every minted value sits strictly above any low vanilla enum member.
        /// </summary>
        /// <param name="reservedFloor">
        ///     Lower bound (inclusive) for minted values. Must be <c>&gt;= 0</c>. Values in <c>[0, reservedFloor)</c>
        ///     are left for vanilla enum members.
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
        /// </summary>
        public int ReservedFloor { get; }

        /// <summary>
        ///     Returns the <typeparamref name="TEnum" /> value for <paramref name="id" />, registering it on first
        ///     call. Subsequent calls with the same id return the same value; ids are compared case-insensitively.
        /// </summary>
        /// <exception cref="ArgumentException">When <paramref name="id" /> is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">
        ///     When two distinct ids hash to the same <typeparamref name="TEnum" /> value (registration-time
        ///     collision detection; callers are expected to pick non-colliding ids).
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
        ///     Attempts to resolve the string id that minted <paramref name="value" />.
        /// </summary>
        /// <returns><c>true</c> when <paramref name="value" /> was produced by an earlier <see cref="Mint" /> call.</returns>
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
        /// </summary>
        public bool IsDynamic(TEnum value)
        {
            lock (_sync)
            {
                return _byValue.ContainsKey(value);
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
