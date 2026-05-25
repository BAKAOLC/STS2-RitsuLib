using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     Mutable instruction-list wrapper for small, auditable Harmony transpiler rewrites.
    ///     用于小型、可审计 Harmony transpiler 改写的可变指令列表包装器。
    /// </summary>
    public sealed class HarmonyIlRewriter
    {
        private readonly List<CodeInstruction> _code;

        private HarmonyIlRewriter(IEnumerable<CodeInstruction> instructions)
        {
            ArgumentNullException.ThrowIfNull(instructions);
            _code = instructions.ToList();
        }

        /// <summary>
        ///     Current mutable instruction list.
        ///     当前可变指令列表。
        /// </summary>
        public IReadOnlyList<CodeInstruction> Code => _code;

        /// <summary>
        ///     Creates a rewriter over <paramref name="instructions" />.
        ///     基于 <paramref name="instructions" /> 创建 rewriter。
        /// </summary>
        public static HarmonyIlRewriter From(IEnumerable<CodeInstruction> instructions)
        {
            return new(instructions);
        }

        /// <summary>
        ///     Returns the rewritten instruction list.
        ///     返回改写后的指令列表。
        /// </summary>
        public List<CodeInstruction> Instructions()
        {
            return _code;
        }

        /// <summary>
        ///     Validates and returns the rewritten instruction list.
        ///     验证并返回改写后的指令列表。
        /// </summary>
        public List<CodeInstruction> InstructionsChecked(string operation = "Harmony IL rewrite")
        {
            Validate(operation).ThrowIfInvalid();
            return _code;
        }

        /// <summary>
        ///     Validates the current instruction list.
        ///     验证当前指令列表。
        /// </summary>
        public HarmonyIlValidationReport Validate(string operation = "Harmony IL rewrite")
        {
            return HarmonyIlValidation.Validate(_code, operation);
        }

        /// <summary>
        ///     Returns true when any instruction satisfies <paramref name="predicate" />.
        ///     任一指令满足 <paramref name="predicate" /> 时返回 true。
        /// </summary>
        public bool Contains(Func<CodeInstruction, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return _code.Any(predicate);
        }

        /// <summary>
        ///     Counts instructions that satisfy <paramref name="predicate" />.
        ///     统计满足 <paramref name="predicate" /> 的指令数量。
        /// </summary>
        public int Count(Func<CodeInstruction, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return _code.Count(predicate);
        }

        /// <summary>
        ///     Finds a sequential pattern.
        ///     查找顺序模式。
        /// </summary>
        public bool TryFind(HarmonyIlPattern pattern, out HarmonyIlMatch match)
        {
            ArgumentNullException.ThrowIfNull(pattern);
            return pattern.TryFind(_code, out match);
        }

        /// <summary>
        ///     Finds a sequential pattern at or after <paramref name="startIndex" />.
        ///     从 <paramref name="startIndex" /> 起查找顺序模式。
        /// </summary>
        public bool TryFind(HarmonyIlPattern pattern, int startIndex, out HarmonyIlMatch match)
        {
            ArgumentNullException.ThrowIfNull(pattern);
            return pattern.TryFind(_code, startIndex, out match);
        }

        /// <summary>
        ///     Finds the last occurrence of a sequential pattern.
        ///     查找顺序模式最后一次出现的位置。
        /// </summary>
        public bool TryFindLast(HarmonyIlPattern pattern, out HarmonyIlMatch match)
        {
            ArgumentNullException.ThrowIfNull(pattern);
            return pattern.TryFindLast(_code, out match);
        }

        /// <summary>
        ///     Finds a pattern after an anchor match, optionally within a limited instruction distance.
        ///     在锚点匹配之后查找模式，可限制最大指令距离。
        /// </summary>
        public bool TryFindAfter(
            HarmonyIlMatch anchor,
            HarmonyIlPattern pattern,
            out HarmonyIlMatch match,
            int maxDistance = int.MaxValue)
        {
            ArgumentNullException.ThrowIfNull(pattern);
            ValidateMatch(anchor);

            var start = anchor.EndExclusive;
            var end = maxDistance == int.MaxValue
                ? _code.Count
                : Math.Min(_code.Count, start + Math.Max(0, maxDistance));
            return pattern.TryFind(_code, start, end, out match);
        }

        /// <summary>
        ///     Finds a pattern before an anchor match, optionally within a limited instruction distance.
        ///     在锚点匹配之前查找模式，可限制最大指令距离。
        /// </summary>
        public bool TryFindBefore(
            HarmonyIlMatch anchor,
            HarmonyIlPattern pattern,
            out HarmonyIlMatch match,
            int maxDistance = int.MaxValue)
        {
            ArgumentNullException.ThrowIfNull(pattern);
            ValidateMatch(anchor);

            var start = maxDistance == int.MaxValue
                ? 0
                : Math.Max(0, anchor.Index - Math.Max(0, maxDistance));
            return pattern.TryFind(_code, start, anchor.Index, out match);
        }

        /// <summary>
        ///     Finds the first instruction that satisfies <paramref name="predicate" />.
        ///     查找第一条满足 <paramref name="predicate" /> 的指令。
        /// </summary>
        public bool TryFindFirst(Func<CodeInstruction, bool> predicate, out HarmonyIlMatch match)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            for (var i = 0; i < _code.Count; i++)
            {
                if (!predicate(_code[i]))
                    continue;

                match = new(i, 1);
                return true;
            }

            match = default;
            return false;
        }

        /// <summary>
        ///     Finds the last instruction that satisfies <paramref name="predicate" />.
        ///     查找最后一条满足 <paramref name="predicate" /> 的指令。
        /// </summary>
        public bool TryFindLast(Func<CodeInstruction, bool> predicate, out HarmonyIlMatch match)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            for (var i = _code.Count - 1; i >= 0; i--)
            {
                if (!predicate(_code[i]))
                    continue;

                match = new(i, 1);
                return true;
            }

            match = default;
            return false;
        }

        /// <summary>
        ///     Inserts instructions immediately before a matched span.
        ///     在已匹配区间前插入指令。
        /// </summary>
        public void InsertBefore(
            HarmonyIlMatch match,
            IEnumerable<CodeInstruction> instructions,
            bool moveLabelsAndBlocksToInserted = false)
        {
            ValidateMatch(match);
            InsertAt(match.Index, instructions, moveLabelsAndBlocksToInserted ? _code[match.Index] : null);
        }

        /// <summary>
        ///     Inserts instructions immediately after a matched span.
        ///     在已匹配区间后插入指令。
        /// </summary>
        public void InsertAfter(HarmonyIlMatch match, IEnumerable<CodeInstruction> instructions)
        {
            ValidateMatch(match);
            InsertAt(match.EndExclusive, instructions, null);
        }

        /// <summary>
        ///     Inserts before the first matched pattern and returns a rewrite report.
        ///     在第一个匹配模式前插入并返回改写报告。
        /// </summary>
        public HarmonyIlRewriteReport TryInsertBeforeFirst(
            string operation,
            HarmonyIlPattern pattern,
            IEnumerable<CodeInstruction> instructions,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null,
            bool moveLabelsAndBlocksToInserted = false)
        {
            return TryInsertAroundFirst(
                operation,
                pattern,
                instructions,
                false,
                alreadySatisfied,
                moveLabelsAndBlocksToInserted);
        }

        /// <summary>
        ///     Inserts after the first matched pattern and returns a rewrite report.
        ///     在第一个匹配模式后插入并返回改写报告。
        /// </summary>
        public HarmonyIlRewriteReport TryInsertAfterFirst(
            string operation,
            HarmonyIlPattern pattern,
            IEnumerable<CodeInstruction> instructions,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null)
        {
            return TryInsertAroundFirst(operation, pattern, instructions, true, alreadySatisfied);
        }

        /// <summary>
        ///     Replaces a matched span with new instructions, moving labels and exception blocks from the first replaced
        ///     instruction to the first inserted instruction.
        ///     用新指令替换已匹配区间，并把第一条被替换指令的 labels 和 exception blocks
        ///     转移到第一条插入指令。
        /// </summary>
        public void Replace(HarmonyIlMatch match, IEnumerable<CodeInstruction> replacement)
        {
            ValidateMatch(match);
            EnsureSafeToReplace(match);

            var replacements = replacement.ToList();
            if (replacements.Count > 0)
                HarmonyIl.MoveMetadataToFirst(_code[match.Index], replacements);

            _code.RemoveRange(match.Index, match.Length);
            _code.InsertRange(match.Index, replacements);
        }

        /// <summary>
        ///     Replaces the first matched pattern and returns a rewrite report.
        ///     替换第一个匹配模式并返回改写报告。
        /// </summary>
        public HarmonyIlRewriteReport TryReplaceFirst(
            string operation,
            HarmonyIlPattern pattern,
            IEnumerable<CodeInstruction> replacement,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);
            ArgumentNullException.ThrowIfNull(pattern);

            var before = _code.Count;
            if (alreadySatisfied?.Invoke(_code) == true)
                return new(operation, 0, 0, before, before, true);

            var matches = pattern.FindAll(_code).Count;
            if (matches == 0)
                return NewReport(operation, 0, 0, before, alreadySatisfied);

            pattern.TryFind(_code, out var match);
            Replace(match, replacement);
            return new(operation, matches, 1, before, _code.Count);
        }

        /// <summary>
        ///     Replaces every instruction whose site predicate matches.
        ///     替换每个满足站点谓词的指令。
        /// </summary>
        public int ReplaceEach(
            Func<IReadOnlyList<CodeInstruction>, int, bool> isMatch,
            Func<IReadOnlyList<CodeInstruction>, int, IReadOnlyList<CodeInstruction>> buildReplacement)
        {
            return ReplaceEach("Harmony IL replace-each", isMatch, buildReplacement).Applied;
        }

        /// <summary>
        ///     Replaces every instruction whose site predicate matches and returns a rewrite report.
        ///     替换每个满足站点谓词的指令并返回改写报告。
        /// </summary>
        public HarmonyIlRewriteReport ReplaceEach(
            string operation,
            Func<IReadOnlyList<CodeInstruction>, int, bool> isMatch,
            Func<IReadOnlyList<CodeInstruction>, int, IReadOnlyList<CodeInstruction>> buildReplacement,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);
            ArgumentNullException.ThrowIfNull(isMatch);
            ArgumentNullException.ThrowIfNull(buildReplacement);

            var before = _code.Count;
            var matches = 0;
            var applied = 0;
            for (var i = 0; i < _code.Count; i++)
            {
                if (!isMatch(_code, i))
                    continue;

                matches++;
                var replacement = buildReplacement(_code, i).ToList();
                if (replacement.Count == 0)
                    continue;

                HarmonyIl.MoveMetadataToFirst(_code[i], replacement);
                _code.RemoveAt(i);
                _code.InsertRange(i, replacement);
                i += replacement.Count - 1;
                applied++;
            }

            return NewReport(operation, matches, applied, before, alreadySatisfied);
        }

        /// <summary>
        ///     Redirects call/callvirt instructions using <paramref name="resolveReplacement" />.
        ///     使用 <paramref name="resolveReplacement" /> 重定向 call/callvirt 指令。
        /// </summary>
        public int RedirectCalls(Func<MethodInfo, MethodInfo?> resolveReplacement)
        {
            return RedirectCalls("Harmony IL redirect-calls", resolveReplacement).Applied;
        }

        /// <summary>
        ///     Redirects call/callvirt instructions and returns a rewrite report.
        ///     重定向 call/callvirt 指令并返回改写报告。
        /// </summary>
        public HarmonyIlRewriteReport RedirectCalls(
            string operation,
            Func<MethodInfo, MethodInfo?> resolveReplacement,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);
            ArgumentNullException.ThrowIfNull(resolveReplacement);

            var before = _code.Count;
            var matches = 0;
            var applied = 0;
            foreach (var instruction in _code)
            {
                if (instruction.opcode != OpCodes.Call && instruction.opcode != OpCodes.Callvirt)
                    continue;
                if (instruction.operand is not MethodInfo called)
                    continue;

                var replacement = resolveReplacement(called);
                if (replacement == null)
                    continue;

                matches++;
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                applied++;
            }

            return NewReport(operation, matches, applied, before, alreadySatisfied);
        }

        /// <summary>
        ///     Inserts instructions before the first ret opcode.
        ///     在第一条 ret 指令前插入指令。
        /// </summary>
        public bool TryInsertBeforeFirstRet(
            IReadOnlyList<CodeInstruction> prefix,
            bool moveLabelsAndBlocksToInserted = false)
        {
            return InsertBeforeFirstRet(
                "Harmony IL insert-before-ret",
                prefix,
                moveLabelsAndBlocksToInserted: moveLabelsAndBlocksToInserted).Applied > 0;
        }

        /// <summary>
        ///     Inserts instructions before the first ret opcode and returns a rewrite report.
        ///     在第一条 ret 指令前插入指令并返回改写报告。
        /// </summary>
        public HarmonyIlRewriteReport InsertBeforeFirstRet(
            string operation,
            IReadOnlyList<CodeInstruction> prefix,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null,
            bool moveLabelsAndBlocksToInserted = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);
            ArgumentNullException.ThrowIfNull(prefix);

            var before = _code.Count;
            if (prefix.Count == 0)
                return NewReport(operation, 0, 0, before, alreadySatisfied);

            var matches = 0;
            for (var i = 0; i < _code.Count; i++)
            {
                if (_code[i].opcode != OpCodes.Ret)
                    continue;

                matches++;
                InsertAt(i, prefix, moveLabelsAndBlocksToInserted ? _code[i] : null);
                return new(operation, matches, 1, before, _code.Count);
            }

            return NewReport(operation, matches, 0, before, alreadySatisfied);
        }

        private HarmonyIlRewriteReport TryInsertAroundFirst(
            string operation,
            HarmonyIlPattern pattern,
            IEnumerable<CodeInstruction> instructions,
            bool insertAfter,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null,
            bool moveLabelsAndBlocksToInserted = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);
            ArgumentNullException.ThrowIfNull(pattern);

            var before = _code.Count;
            if (alreadySatisfied?.Invoke(_code) == true)
                return new(operation, 0, 0, before, before, true);

            var matches = pattern.FindAll(_code).Count;
            if (matches == 0)
                return NewReport(operation, 0, 0, before, alreadySatisfied);

            pattern.TryFind(_code, out var match);
            if (insertAfter)
                InsertAfter(match, instructions);
            else
                InsertBefore(match, instructions, moveLabelsAndBlocksToInserted);

            return new(operation, matches, 1, before, _code.Count);
        }

        private HarmonyIlRewriteReport NewReport(
            string operation,
            int matches,
            int applied,
            int before,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied)
        {
            return new(
                operation,
                matches,
                applied,
                before,
                _code.Count,
                applied == 0 && alreadySatisfied?.Invoke(_code) == true);
        }

        private void InsertAt(
            int index,
            IEnumerable<CodeInstruction> instructions,
            CodeInstruction? metadataSource)
        {
            ArgumentNullException.ThrowIfNull(instructions);
            if (index < 0 || index > _code.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var insertion = instructions.ToList();
            if (insertion.Count == 0)
                return;

            if (metadataSource != null)
                HarmonyIl.MoveMetadataToFirst(metadataSource, insertion);

            _code.InsertRange(index, insertion);
        }

        private void ValidateMatch(HarmonyIlMatch match)
        {
            if (match.Index < 0 || match.Length <= 0 || match.EndExclusive > _code.Count)
                throw new ArgumentOutOfRangeException(nameof(match),
                    $"Invalid IL match span index={match.Index}, length={match.Length}, codeCount={_code.Count}.");
        }

        private void EnsureSafeToReplace(HarmonyIlMatch match)
        {
            if (HarmonyIlValidation.CanReplaceSpan(_code, match, out var reason))
                return;

            throw new InvalidOperationException(reason);
        }
    }
}
