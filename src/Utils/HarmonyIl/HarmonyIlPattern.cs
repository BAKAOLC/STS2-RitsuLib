using System.Reflection;
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
        ///     Finds all non-overlapping occurrences of this pattern and returns assertion helpers.
        ///     查找此模式的所有非重叠匹配并返回断言辅助对象。
        /// </summary>
        public HarmonyIlMatches FindMatches(IReadOnlyList<CodeInstruction> code, string description = "IL pattern")
        {
            return new(description, FindAll(code));
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
    ///     A collection of IL matches with assertion helpers.
    ///     带断言辅助方法的 IL 匹配集合。
    /// </summary>
    public sealed class HarmonyIlMatches
    {
        /// <summary>
        ///     Creates a match collection.
        ///     创建匹配集合。
        /// </summary>
        public HarmonyIlMatches(string description, IEnumerable<HarmonyIlMatch> matches)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentNullException.ThrowIfNull(matches);
            Description = description;
            Items = [.. matches];
        }

        /// <summary>
        ///     Human-readable description used in assertion errors.
        ///     断言错误中使用的可读描述。
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///     Number of matches.
        ///     匹配数量。
        /// </summary>
        public int Count => Items.Count;

        /// <summary>
        ///     Matched spans.
        ///     已匹配区间。
        /// </summary>
        public IReadOnlyList<HarmonyIlMatch> Items { get; }

        /// <summary>
        ///     Returns true when at least one match exists.
        ///     存在至少一个匹配时返回 true。
        /// </summary>
        public bool Any => Items.Count > 0;

        /// <summary>
        ///     Returns the first match and throws when none exist.
        ///     返回第一个匹配；不存在匹配时抛出异常。
        /// </summary>
        public HarmonyIlMatch First()
        {
            return Items.Count > 0 ? Items[0] : throw NewCountException("at least 1");
        }

        /// <summary>
        ///     Returns the last match and throws when none exist.
        ///     返回最后一个匹配；不存在匹配时抛出异常。
        /// </summary>
        public HarmonyIlMatch Last()
        {
            return Items.Count > 0 ? Items[^1] : throw NewCountException("at least 1");
        }

        /// <summary>
        ///     Requires exactly one match and returns it.
        ///     要求恰好一个匹配并返回它。
        /// </summary>
        public HarmonyIlMatch RequireSingle()
        {
            return Items.Count == 1 ? Items[0] : throw NewCountException("exactly 1");
        }

        /// <summary>
        ///     Requires an exact match count.
        ///     要求精确匹配数量。
        /// </summary>
        public HarmonyIlMatches RequireExactly(int count)
        {
            return Items.Count == count ? this : throw NewCountException($"exactly {count}");
        }

        /// <summary>
        ///     Requires at least <paramref name="count" /> matches.
        ///     要求至少 <paramref name="count" /> 个匹配。
        /// </summary>
        public HarmonyIlMatches RequireAtLeast(int count)
        {
            return Items.Count >= count ? this : throw NewCountException($"at least {count}");
        }

        /// <summary>
        ///     Requires no matches.
        ///     要求没有匹配。
        /// </summary>
        public HarmonyIlMatches RequireNone()
        {
            return RequireExactly(0);
        }

        /// <summary>
        ///     Returns a compact diagnostic string.
        ///     返回紧凑诊断字符串。
        /// </summary>
        public string Describe()
        {
            return
                $"{Description}: count={Items.Count}, indexes=[{string.Join(", ", Items.Select(static match => match.Index))}]";
        }

        private InvalidOperationException NewCountException(string expected)
        {
            return new($"{Description} matched {Items.Count} span(s), expected {expected}. " +
                       $"indexes=[{string.Join(", ", Items.Select(static match => match.Index))}].");
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

        /// <summary>
        ///     Reads a local-load reference from the matched instruction at <paramref name="offset" />.
        ///     从匹配区间中 <paramref name="offset" /> 处的指令读取本地变量读取引用。
        /// </summary>
        public HarmonyIlLocalRef GetLocalLoad(IReadOnlyList<CodeInstruction> code, int offset)
        {
            var instruction = InstructionAt(code, offset);
            if (HarmonyIl.TryGetLocalLoad(instruction, out var local))
                return local;

            throw new InvalidOperationException(
                $"Matched instruction at offset {offset} is not a local-load instruction.");
        }

        /// <summary>
        ///     Reads a local-store reference from the matched instruction at <paramref name="offset" />.
        ///     从匹配区间中 <paramref name="offset" /> 处的指令读取本地变量存储引用。
        /// </summary>
        public HarmonyIlLocalRef GetLocalStore(IReadOnlyList<CodeInstruction> code, int offset)
        {
            var instruction = InstructionAt(code, offset);
            if (HarmonyIl.TryGetLocalStore(instruction, out var local))
                return local;

            throw new InvalidOperationException(
                $"Matched instruction at offset {offset} is not a local-store instruction.");
        }

        /// <summary>
        ///     Reads a typed operand from the matched instruction at <paramref name="offset" />.
        ///     从匹配区间中 <paramref name="offset" /> 处的指令读取指定类型 operand。
        /// </summary>
        public T GetOperand<T>(IReadOnlyList<CodeInstruction> code, int offset)
        {
            var instruction = InstructionAt(code, offset);
            if (HarmonyIl.TryGetOperand<T>(instruction, out var operand))
                return operand;

            throw new InvalidOperationException(
                $"Matched instruction at offset {offset} does not have a {typeof(T).Name} operand.");
        }

        /// <summary>
        ///     Reads a method operand from the matched instruction at <paramref name="offset" />.
        ///     从匹配区间中 <paramref name="offset" /> 处的指令读取方法 operand。
        /// </summary>
        public MethodInfo GetMethodOperand(IReadOnlyList<CodeInstruction> code, int offset)
        {
            return GetOperand<MethodInfo>(code, offset);
        }

        /// <summary>
        ///     Reads a field operand from the matched instruction at <paramref name="offset" />.
        ///     从匹配区间中 <paramref name="offset" /> 处的指令读取字段 operand。
        /// </summary>
        public FieldInfo GetFieldOperand(IReadOnlyList<CodeInstruction> code, int offset)
        {
            return GetOperand<FieldInfo>(code, offset);
        }

        /// <summary>
        ///     Reads a string operand from the matched instruction at <paramref name="offset" />.
        ///     从匹配区间中 <paramref name="offset" /> 处的指令读取字符串 operand。
        /// </summary>
        public string GetStringOperand(IReadOnlyList<CodeInstruction> code, int offset)
        {
            return GetOperand<string>(code, offset);
        }
    }
}
