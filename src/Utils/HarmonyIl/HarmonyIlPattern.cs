using System.Reflection;
using HarmonyLib;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     <para xml:lang="en">A small sequential IL pattern used by RitsuLib transpiler wrappers.</para>
    ///     <para xml:lang="zh-CN">供 RitsuLib 转译器包装器使用的小型顺序 IL 模式。</para>
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
        ///     <para xml:lang="en">Number of instructions matched by this pattern.</para>
        ///     <para xml:lang="zh-CN">此模式匹配的指令数量。</para>
        /// </summary>
        public int Length => _parts.Length;

        /// <summary>
        ///     <para xml:lang="en">Creates a sequential pattern.</para>
        ///     <para xml:lang="zh-CN">创建顺序模式。</para>
        /// </summary>
        public static HarmonyIlPattern Sequence(params Func<CodeInstruction, bool>[] parts)
        {
            ArgumentNullException.ThrowIfNull(parts);
            return new(parts);
        }

        /// <summary>
        ///     <para xml:lang="en">Finds the first occurrence of this pattern.</para>
        ///     <para xml:lang="zh-CN">查找此模式第一次出现的位置。</para>
        /// </summary>
        public bool TryFind(IReadOnlyList<CodeInstruction> code, out HarmonyIlMatch match)
        {
            return TryFind(code, 0, out match);
        }

        /// <summary>
        ///     <para xml:lang="en">Finds the first occurrence of this pattern at or after <paramref name="startIndex" />.</para>
        ///     <para xml:lang="zh-CN">从 <paramref name="startIndex" /> 起查找此模式第一次出现的位置。</para>
        /// </summary>
        public bool TryFind(IReadOnlyList<CodeInstruction> code, int startIndex, out HarmonyIlMatch match)
        {
            return TryFind(code, startIndex, code?.Count ?? 0, out match);
        }

        /// <summary>
        ///     <para xml:lang="en">Finds the first occurrence of this pattern inside a bounded range.</para>
        ///     <para xml:lang="zh-CN">在给定边界内查找此模式第一次出现的位置。</para>
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
        ///     <para xml:lang="en">Finds the last occurrence of this pattern.</para>
        ///     <para xml:lang="zh-CN">查找此模式最后一次出现的位置。</para>
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
        ///     <para xml:lang="en">Finds all non-overlapping occurrences of this pattern.</para>
        ///     <para xml:lang="zh-CN">查找此模式的所有非重叠匹配。</para>
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
        ///     <para xml:lang="en">Finds all non-overlapping occurrences of this pattern and returns assertion helpers.</para>
        ///     <para xml:lang="zh-CN">查找此模式的所有非重叠匹配并返回断言辅助对象。</para>
        /// </summary>
        public HarmonyIlMatches FindMatches(IReadOnlyList<CodeInstruction> code, string description = "IL pattern")
        {
            return new(description, FindAll(code));
        }

        /// <summary>
        ///     <para xml:lang="en">Returns true when this pattern matches at <paramref name="index" />.</para>
        ///     <para xml:lang="zh-CN">当此模式在 <paramref name="index" /> 处匹配时返回 true。</para>
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
    ///     <para xml:lang="en">A collection of IL matches with assertion helpers.</para>
    ///     <para xml:lang="zh-CN">带断言辅助方法的 IL 匹配集合。</para>
    /// </summary>
    public sealed class HarmonyIlMatches
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a match collection.</para>
        ///     <para xml:lang="zh-CN">创建匹配集合。</para>
        /// </summary>
        public HarmonyIlMatches(string description, IEnumerable<HarmonyIlMatch> matches)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentNullException.ThrowIfNull(matches);
            Description = description;
            Items = [.. matches];
        }

        /// <summary>
        ///     <para xml:lang="en">Human-readable description used in assertion errors.</para>
        ///     <para xml:lang="zh-CN">断言错误中使用的可读描述。</para>
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///     <para xml:lang="en">Number of matches.</para>
        ///     <para xml:lang="zh-CN">匹配数量。</para>
        /// </summary>
        public int Count => Items.Count;

        /// <summary>
        ///     <para xml:lang="en">Matched spans.</para>
        ///     <para xml:lang="zh-CN">已匹配区间。</para>
        /// </summary>
        public IReadOnlyList<HarmonyIlMatch> Items { get; }

        /// <summary>
        ///     <para xml:lang="en">Returns true when at least one match exists.</para>
        ///     <para xml:lang="zh-CN">存在至少一个匹配时返回 true。</para>
        /// </summary>
        public bool Any => Items.Count > 0;

        /// <summary>
        ///     <para xml:lang="en">Returns the first match and throws when none exist.</para>
        ///     <para xml:lang="zh-CN">返回第一个匹配；不存在匹配时抛出异常。</para>
        /// </summary>
        public HarmonyIlMatch First()
        {
            return Items.Count > 0 ? Items[0] : throw NewCountException("at least 1");
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the last match and throws when none exist.</para>
        ///     <para xml:lang="zh-CN">返回最后一个匹配；不存在匹配时抛出异常。</para>
        /// </summary>
        public HarmonyIlMatch Last()
        {
            return Items.Count > 0 ? Items[^1] : throw NewCountException("at least 1");
        }

        /// <summary>
        ///     <para xml:lang="en">Requires exactly one match and returns it.</para>
        ///     <para xml:lang="zh-CN">要求恰好一个匹配并返回它。</para>
        /// </summary>
        public HarmonyIlMatch RequireSingle()
        {
            return Items.Count == 1 ? Items[0] : throw NewCountException("exactly 1");
        }

        /// <summary>
        ///     <para xml:lang="en">Requires an exact match count.</para>
        ///     <para xml:lang="zh-CN">要求精确匹配数量。</para>
        /// </summary>
        public HarmonyIlMatches RequireExactly(int count)
        {
            return Items.Count == count ? this : throw NewCountException($"exactly {count}");
        }

        /// <summary>
        ///     <para xml:lang="en">Requires at least <paramref name="count" /> matches.</para>
        ///     <para xml:lang="zh-CN">要求至少 <paramref name="count" /> 个匹配。</para>
        /// </summary>
        public HarmonyIlMatches RequireAtLeast(int count)
        {
            return Items.Count >= count ? this : throw NewCountException($"at least {count}");
        }

        /// <summary>
        ///     <para xml:lang="en">Requires no matches.</para>
        ///     <para xml:lang="zh-CN">要求没有匹配。</para>
        /// </summary>
        public HarmonyIlMatches RequireNone()
        {
            return RequireExactly(0);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a compact diagnostic string.</para>
        ///     <para xml:lang="zh-CN">返回紧凑诊断字符串。</para>
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
    ///     <para xml:lang="en">A matched IL pattern span.</para>
    ///     <para xml:lang="zh-CN">已匹配的 IL 模式区间。</para>
    /// </summary>
    public readonly record struct HarmonyIlMatch(int Index, int Length)
    {
        /// <summary>
        ///     <para xml:lang="en">First index after the match.</para>
        ///     <para xml:lang="zh-CN">匹配结束后的第一个索引。</para>
        /// </summary>
        public int EndExclusive => Index + Length;

        /// <summary>
        ///     <para xml:lang="en">Returns the matched instruction at <paramref name="offset" />.</para>
        ///     <para xml:lang="zh-CN">返回匹配区间中 <paramref name="offset" /> 处的指令。</para>
        /// </summary>
        public CodeInstruction InstructionAt(IReadOnlyList<CodeInstruction> code, int offset)
        {
            ArgumentNullException.ThrowIfNull(code);
            if (offset < 0 || offset >= Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return code[Index + offset];
        }

        /// <summary>
        ///     <para xml:lang="en">Reads a local-load reference from the matched instruction at <paramref name="offset" />.</para>
        ///     <para xml:lang="zh-CN">从匹配区间中 <paramref name="offset" /> 处的指令读取本地变量读取引用。</para>
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
        ///     <para xml:lang="en">Reads a local-store reference from the matched instruction at <paramref name="offset" />.</para>
        ///     <para xml:lang="zh-CN">从匹配区间中 <paramref name="offset" /> 处的指令读取本地变量存储引用。</para>
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
        ///     <para xml:lang="en">Reads a typed operand from the matched instruction at <paramref name="offset" />.</para>
        ///     <para xml:lang="zh-CN">从匹配区间中 <paramref name="offset" /> 处的指令读取指定类型的操作数。</para>
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
        ///     <para xml:lang="en">Reads a method operand from the matched instruction at <paramref name="offset" />.</para>
        ///     <para xml:lang="zh-CN">从匹配区间中 <paramref name="offset" /> 处的指令读取方法操作数。</para>
        /// </summary>
        public MethodInfo GetMethodOperand(IReadOnlyList<CodeInstruction> code, int offset)
        {
            return GetOperand<MethodInfo>(code, offset);
        }

        /// <summary>
        ///     <para xml:lang="en">Reads a field operand from the matched instruction at <paramref name="offset" />.</para>
        ///     <para xml:lang="zh-CN">从匹配区间中 <paramref name="offset" /> 处的指令读取字段操作数。</para>
        /// </summary>
        public FieldInfo GetFieldOperand(IReadOnlyList<CodeInstruction> code, int offset)
        {
            return GetOperand<FieldInfo>(code, offset);
        }

        /// <summary>
        ///     <para xml:lang="en">Reads a string operand from the matched instruction at <paramref name="offset" />.</para>
        ///     <para xml:lang="zh-CN">从匹配区间中 <paramref name="offset" /> 处的指令读取字符串操作数。</para>
        /// </summary>
        public string GetStringOperand(IReadOnlyList<CodeInstruction> code, int offset)
        {
            return GetOperand<string>(code, offset);
        }
    }
}
