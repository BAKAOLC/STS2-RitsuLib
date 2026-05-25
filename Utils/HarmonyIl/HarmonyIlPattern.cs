using HarmonyLib;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     A small sequential IL pattern used by RitsuLib transpiler wrappers.
    ///     RitsuLib transpiler 包装器使用的小型顺序 IL 模式。
    /// </summary>
    public sealed class HarmonyIlPattern
    {
        private readonly Func<CodeInstruction, bool>[] _parts;

        private HarmonyIlPattern(Func<CodeInstruction, bool>[] parts)
        {
            if (parts.Length == 0)
                throw new ArgumentException("Pattern must contain at least one matcher.", nameof(parts));
            if (parts.Any(static part => part == null))
                throw new ArgumentException("Pattern cannot contain null matchers.", nameof(parts));

            _parts = parts;
        }

        /// <summary>
        ///     Number of instructions matched by this pattern.
        ///     此模式匹配的指令数量。
        /// </summary>
        public int Length => _parts.Length;

        /// <summary>
        ///     Creates a sequential pattern.
        ///     创建顺序模式。
        /// </summary>
        public static HarmonyIlPattern Sequence(params Func<CodeInstruction, bool>[] parts)
        {
            ArgumentNullException.ThrowIfNull(parts);
            return new(parts);
        }

        /// <summary>
        ///     Finds the first occurrence of this pattern.
        ///     查找此模式第一次出现的位置。
        /// </summary>
        public bool TryFind(IReadOnlyList<CodeInstruction> code, out HarmonyIlMatch match)
        {
            return TryFind(code, 0, out match);
        }

        /// <summary>
        ///     Finds the first occurrence of this pattern at or after <paramref name="startIndex" />.
        ///     从 <paramref name="startIndex" /> 起查找此模式第一次出现的位置。
        /// </summary>
        public bool TryFind(IReadOnlyList<CodeInstruction> code, int startIndex, out HarmonyIlMatch match)
        {
            return TryFind(code, startIndex, code?.Count ?? 0, out match);
        }

        /// <summary>
        ///     Finds the first occurrence of this pattern inside a bounded range.
        ///     在给定边界内查找此模式第一次出现的位置。
        /// </summary>
        public bool TryFind(
            IReadOnlyList<CodeInstruction> code,
            int startIndex,
            int endExclusive,
            out HarmonyIlMatch match)
        {
            ArgumentNullException.ThrowIfNull(code);

            var start = Math.Max(0, startIndex);
            var end = Math.Min(code.Count, endExclusive);
            for (var i = start; i <= end - _parts.Length; i++)
            {
                if (!MatchesAt(code, i))
                    continue;

                match = new(i, _parts.Length);
                return true;
            }

            match = default;
            return false;
        }

        /// <summary>
        ///     Finds the last occurrence of this pattern.
        ///     查找此模式最后一次出现的位置。
        /// </summary>
        public bool TryFindLast(IReadOnlyList<CodeInstruction> code, out HarmonyIlMatch match)
        {
            ArgumentNullException.ThrowIfNull(code);

            for (var i = code.Count - _parts.Length; i >= 0; i--)
            {
                if (!MatchesAt(code, i))
                    continue;

                match = new(i, _parts.Length);
                return true;
            }

            match = default;
            return false;
        }

        /// <summary>
        ///     Finds all non-overlapping occurrences of this pattern.
        ///     查找此模式的所有非重叠匹配。
        /// </summary>
        public IReadOnlyList<HarmonyIlMatch> FindAll(IReadOnlyList<CodeInstruction> code)
        {
            ArgumentNullException.ThrowIfNull(code);

            var matches = new List<HarmonyIlMatch>();
            var index = 0;
            while (TryFind(code, index, out var match))
            {
                matches.Add(match);
                index = match.EndExclusive;
            }

            return matches;
        }

        /// <summary>
        ///     Returns true when this pattern matches at <paramref name="index" />.
        ///     当此模式在 <paramref name="index" /> 处匹配时返回 true。
        /// </summary>
        public bool MatchesAt(IReadOnlyList<CodeInstruction> code, int index)
        {
            ArgumentNullException.ThrowIfNull(code);
            return index >= 0 && index <= code.Count - _parts.Length && MatchesAtCore(code, index);
        }

        private bool MatchesAtCore(IReadOnlyList<CodeInstruction> code, int index)
        {
            return !_parts.Where((t, offset) => !t(code[index + offset])).Any();
        }
    }

    /// <summary>
    ///     A matched IL pattern span.
    ///     已匹配的 IL 模式区间。
    /// </summary>
    public readonly record struct HarmonyIlMatch(int Index, int Length)
    {
        /// <summary>
        ///     First index after the match.
        ///     匹配结束后的第一个索引。
        /// </summary>
        public int EndExclusive => Index + Length;

        /// <summary>
        ///     Returns the matched instruction at <paramref name="offset" />.
        ///     返回匹配区间中 <paramref name="offset" /> 处的指令。
        /// </summary>
        public CodeInstruction InstructionAt(IReadOnlyList<CodeInstruction> code, int offset)
        {
            ArgumentNullException.ThrowIfNull(code);
            if (offset < 0 || offset >= Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return code[Index + offset];
        }
    }
}
