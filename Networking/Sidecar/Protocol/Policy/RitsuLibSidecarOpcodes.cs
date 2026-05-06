using System.IO.Hashing;
using System.Text;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Sidecar opcodes are 64-bit. Values <c>0</c> through <see cref="FixedProtocolOpcodeMaxInclusive" /> are
    ///     reserved for framework and shared-library fixed protocols; <see cref="For" /> only yields values in
    ///     <c>[<see cref="HashDerivedOpcodeMin" />, ulong.MaxValue]</c>.
    /// </summary>
    public static class RitsuLibSidecarOpcodes
    {
        private const string Separator = "\0";

        /// <summary>
        ///     Upper bound of the reserved range for fixed (non-hashed) framework / library opcodes.
        /// </summary>
        public const ulong FixedProtocolOpcodeMaxInclusive = 0xFFFF;

        /// <summary>
        ///     Lower bound of opcodes returned by <see cref="For" /> (above the reserved range).
        /// </summary>
        public const ulong HashDerivedOpcodeMin = FixedProtocolOpcodeMaxInclusive + 1;

        private const ulong HashTag = HashDerivedOpcodeMin;

        /// <summary>
        ///     Returns a stable opcode for a mod-owned message kind. Input is UTF-8 concatenation
        ///     <c>modId + U+0000 + messageKind</c>. The value is always at least
        ///     <see cref="HashDerivedOpcodeMin" />, so it never falls in the reserved block
        ///     <c>0</c>–<see cref="FixedProtocolOpcodeMaxInclusive" />. Change <paramref name="messageKind" /> when the
        ///     payload contract changes.
        /// </summary>
        public static ulong For(string modId, string messageKind)
        {
            ArgumentException.ThrowIfNullOrEmpty(modId);
            ArgumentException.ThrowIfNullOrEmpty(messageKind);
            var utf8 = Encoding.UTF8;
            var a = utf8.GetBytes(modId);
            var b = utf8.GetBytes(Separator);
            var c = utf8.GetBytes(messageKind);
            var total = a.Length + b.Length + c.Length;
            var buf = new byte[total];
            a.AsSpan().CopyTo(buf);
            b.AsSpan().CopyTo(buf.AsSpan(a.Length));
            c.AsSpan().CopyTo(buf.AsSpan(a.Length + b.Length));
            return XxHash64.HashToUInt64(buf) | HashTag;
        }
    }
}
