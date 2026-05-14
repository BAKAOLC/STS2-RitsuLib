using System.Reflection.Emit;
using HarmonyLib;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     Small, auditable IL helpers for Harmony transpilers. Only patterns that are easy to reason about
    ///     and match verified game / stub method shapes belong here — not a general instruction matcher.
    ///     供 Harmony transpiler 使用的小型、可审计 IL 辅助方法。这里只放易于推理
    ///     且匹配已验证游戏 / 存根方法形状的模式；这里不是通用指令匹配器。
    /// </summary>
    public static class HarmonyVerifiedIl
    {
        /// <summary>
        ///     Inserts <paramref name="prefix" /> immediately before the first <see cref="OpCodes.Ret" /> in
        ///     <paramref name="body" />. Suitable for empty or single-return stub methods (e.g. ModInterop shims).
        ///     将 <paramref name="prefix" /> 插入到 <paramref name="body" /> 中第一个 <see cref="OpCodes.Ret" /> 之前。
        ///     适用于空存根方法或单返回存根方法（例如 ModInterop shim）。
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         <b>Not supported:</b> methods with no <c>ret</c>, multiple returns, or filter/try regions
        ///         where the first <c>ret</c> is not the intended injection site.
        ///     </para>
        ///     <para>Prefer writing a dedicated transpiler when you need label preservation or multi-site edits.</para>
        ///     <para>
        ///         <b>不支持：</b>没有 <c>ret</c>、存在多个返回、或包含 filter/try 区域的方法，
        ///         其中第一个 <c>ret</c> 不是预期注入位置。
        ///     </para>
        ///     <para>需要保留标签或进行多位置编辑时，优先编写专用 transpiler。</para>
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="prefix" /> is empty.</exception>
        /// <exception cref="InvalidOperationException">No <c>ret</c> opcode was found.</exception>
        public static List<CodeInstruction> InsertBeforeFirstRet(
            IEnumerable<CodeInstruction> body,
            IReadOnlyList<CodeInstruction> prefix)
        {
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(prefix);
            if (prefix.Count == 0)
                throw new ArgumentException("Prefix must contain at least one instruction.", nameof(prefix));

            var list = body.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].opcode != OpCodes.Ret)
                    continue;
                list.InsertRange(i, prefix);
                return list;
            }

            throw new InvalidOperationException(
                "No ret opcode found in method body; InsertBeforeFirstRet only supports bodies with at least one ret.");
        }

        /// <summary>
        ///     Same as <see cref="InsertBeforeFirstRet" /> but returns false when no <c>ret</c> exists, instead of throwing.
        ///     与 <see cref="InsertBeforeFirstRet" /> 相同，但在不存在 <c>ret</c> 时返回 false，而不是抛出异常。
        /// </summary>
        public static bool TryInsertBeforeFirstRet(
            IEnumerable<CodeInstruction> body,
            IReadOnlyList<CodeInstruction> prefix,
            out List<CodeInstruction>? result)
        {
            result = null;
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(prefix);
            if (prefix.Count == 0)
                return false;

            var list = body.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].opcode != OpCodes.Ret)
                    continue;
                list.InsertRange(i, prefix);
                result = list;
                return true;
            }

            return false;
        }
    }
}
