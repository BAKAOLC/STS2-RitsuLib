using System.IO.Hashing;
using System.Text;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides 64-bit sidecar opcodes. Values from <c>0</c> through
    ///         <see cref="FixedProtocolOpcodeMaxInclusive" /> are reserved for fixed framework and shared-library
    ///         protocols; <see cref="For" /> returns only values above that range.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供 64 位 sidecar 操作码。<c>0</c> 至 <see cref="FixedProtocolOpcodeMaxInclusive" /> 保留给框架和
    ///         共享库的固定协议；<see cref="For" /> 只返回高于该范围的值。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarOpcodes
    {
        private const string Separator = "\0";

        /// <summary>
        ///     <para xml:lang="en">
        ///         The inclusive upper bound of the range reserved for fixed, non-hashed opcodes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         固定非哈希操作码保留范围的上界（含）。
        ///     </para>
        /// </summary>
        public const ulong FixedProtocolOpcodeMaxInclusive = 0xFFFF;

        /// <summary>
        ///     <para xml:lang="en">
        ///         The inclusive lower bound of opcodes returned by <see cref="For" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <see cref="For" /> 返回的操作码下界（含）。
        ///     </para>
        /// </summary>
        public const ulong HashDerivedOpcodeMin = FixedProtocolOpcodeMaxInclusive + 1;

        private const ulong HashTag = HashDerivedOpcodeMin;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a stable opcode for a mod-owned message kind by hashing the UTF-8 bytes of
        ///         <c>modId + U+0000 + messageKind</c>. Change <paramref name="messageKind" /> whenever the payload
        ///         contract changes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过哈希 <c>modId + U+0000 + messageKind</c> 的 UTF-8 字节，为模组消息类型返回稳定操作码。
        ///         载荷契约发生变化时，应同时更改 <paramref name="messageKind" />。
        ///     </para>
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
