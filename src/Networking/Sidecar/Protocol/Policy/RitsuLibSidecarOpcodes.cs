using System.IO.Hashing;
using System.Text;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Sidecar opcodes are 64-bit. Values <c>0</c> through <see cref="FixedProtocolOpcodeMaxInclusive" /> are
    ///     reserved for framework and shared-library fixed protocols; <see cref="For" /> only yields values in
    ///     <c>[<see cref="HashDerivedOpcodeMin" />, ulong.MaxValue]</c>.
    ///     Sidecar opcode 为 64 位。<c>0</c> 到 <see cref="FixedProtocolOpcodeMaxInclusive" /> 的值
    ///     保留给框架和共享库固定协议；<see cref="For" /> 只生成
    ///     <c>[<see cref="HashDerivedOpcodeMin" />, ulong.MaxValue]</c> 范围内的值。
    /// </summary>
    public static class RitsuLibSidecarOpcodes
    {
        private const string Separator = "\0";

        /// <summary>
        ///     Upper bound of the reserved range for fixed (non-hashed) framework / library opcodes.
        ///     框架 / 库固定（非哈希）opcode 保留范围的上界。
        /// </summary>
        public const ulong FixedProtocolOpcodeMaxInclusive = 0xFFFF;

        /// <summary>
        ///     Lower bound of opcodes returned by <see cref="For" /> (above the reserved range).
        ///     <see cref="For" /> 返回的 opcode 下界（高于保留范围）。
        /// </summary>
        public const ulong HashDerivedOpcodeMin = FixedProtocolOpcodeMaxInclusive + 1;

        private const ulong HashTag = HashDerivedOpcodeMin;

        /// <summary>
        ///     Returns a stable opcode for a mod-owned message kind. Input is UTF-8 concatenation
        ///     <c>modId + U+0000 + messageKind</c>. The value is always at least
        ///     <see cref="HashDerivedOpcodeMin" />, so it never falls in the reserved block
        ///     <c>0</c>–<see cref="FixedProtocolOpcodeMaxInclusive" />. Change <paramref name="messageKind" /> when the
        ///     payload contract changes.
        ///     payload contract changes.
        ///     为 mod 拥有的消息种类返回稳定 opcode。输入是 UTF-8 拼接
        ///     <c>modId + U+0000 + messageKind</c>。该值始终至少为
        ///     <see cref="HashDerivedOpcodeMin" />，因此永远不会落入保留块
        ///     <c>0</c>–<see cref="FixedProtocolOpcodeMaxInclusive" />。载荷契约变化时请更改 <paramref name="messageKind" />。
        ///     载荷契约变化时请更改。
        ///     载荷契约变化时请更改。
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
