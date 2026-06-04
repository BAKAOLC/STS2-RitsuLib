using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     Method context used for IL queries that need local variable metadata.
    ///     需要本地变量元数据的 IL 查询使用的方法上下文。
    /// </summary>
    public sealed class HarmonyIlContext
    {
        private HarmonyIlContext(MethodBase? originalMethod, IReadOnlyList<LocalVariableInfo> locals)
        {
            OriginalMethod = originalMethod;
            Locals = locals;
        }

        /// <summary>
        ///     Original method supplied by Harmony, when available.
        ///     Harmony 提供的原始方法（如果可用）。
        /// </summary>
        public MethodBase? OriginalMethod { get; }

        /// <summary>
        ///     Local variables declared by the original method body.
        ///     原始方法体声明的本地变量。
        /// </summary>
        public IReadOnlyList<LocalVariableInfo> Locals { get; }

        /// <summary>
        ///     Creates context from an original method.
        ///     根据原始方法创建上下文。
        /// </summary>
        public static HarmonyIlContext From(MethodBase? originalMethod)
        {
            var locals = originalMethod?.GetMethodBody()?.LocalVariables.ToList() ?? [];
            return new(originalMethod, locals);
        }

        /// <summary>
        ///     Tries to resolve a local variable type by index.
        ///     尝试按索引解析本地变量类型。
        /// </summary>
        public bool TryGetLocalType(int index, out Type localType)
        {
            var local = Locals.FirstOrDefault(variable => variable.LocalIndex == index);
            if (local != null)
            {
                localType = local.LocalType;
                return true;
            }

            localType = null!;
            return false;
        }
    }

    /// <summary>
    ///     Mutable instruction-list wrapper for small, auditable Harmony transpiler rewrites.
    ///     用于小型、可审计 Harmony transpiler 改写的可变指令列表包装器。
    /// </summary>
    public sealed class HarmonyIlRewriter
    {
        private readonly List<CodeInstruction> _code;

        private HarmonyIlRewriter(IEnumerable<CodeInstruction> instructions, HarmonyIlContext? context = null)
        {
            ArgumentNullException.ThrowIfNull(instructions);
            _code = instructions.ToList();
            Context = context;
        }

        /// <summary>
        ///     Current mutable instruction list.
        ///     当前可变指令列表。
        /// </summary>
        public IReadOnlyList<CodeInstruction> Code => _code;

        /// <summary>
        ///     Optional method context used by type-aware local queries.
        ///     类型感知本地变量查询使用的可选方法上下文。
        /// </summary>
        public HarmonyIlContext? Context { get; }

        /// <summary>
        ///     Creates a rewriter over <paramref name="instructions" />.
        ///     基于 <paramref name="instructions" /> 创建 rewriter。
        /// </summary>
        public static HarmonyIlRewriter From(IEnumerable<CodeInstruction> instructions)
        {
            return new(instructions);
        }

        /// <summary>
        ///     Creates a rewriter with method context from Harmony's original method parameter.
        ///     使用 Harmony 原始方法参数提供的方法上下文创建 rewriter。
        /// </summary>
        public static HarmonyIlRewriter From(IEnumerable<CodeInstruction> instructions, MethodBase? originalMethod)
        {
            return new(instructions, HarmonyIlContext.From(originalMethod));
        }

        /// <summary>
        ///     Creates a rewriter with explicit method context.
        ///     使用显式方法上下文创建 rewriter。
        /// </summary>
        public static HarmonyIlRewriter From(IEnumerable<CodeInstruction> instructions, HarmonyIlContext? context)
        {
            return new(instructions, context);
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
        ///     Finds the nearest instruction before an anchor that satisfies <paramref name="predicate" />.
        ///     查找锚点前最近一条满足 <paramref name="predicate" /> 的指令。
        /// </summary>
        public bool TryFindBefore(
            HarmonyIlMatch anchor,
            Func<CodeInstruction, bool> predicate,
            out HarmonyIlMatch match,
            int maxDistance = int.MaxValue)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            ValidateMatch(anchor);

            var start = maxDistance == int.MaxValue
                ? 0
                : Math.Max(0, anchor.Index - Math.Max(0, maxDistance));
            for (var i = anchor.Index - 1; i >= start; i--)
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
        ///     Finds all instructions that satisfy <paramref name="predicate" />.
        ///     查找所有满足 <paramref name="predicate" /> 的指令。
        /// </summary>
        public HarmonyIlMatches FindAll(Func<CodeInstruction, bool> predicate, string description = "IL instruction")
        {
            ArgumentNullException.ThrowIfNull(predicate);

            var matches = new List<HarmonyIlMatch>();
            for (var i = 0; i < _code.Count; i++)
                if (predicate(_code[i]))
                    matches.Add(new(i, 1));

            return new(description, matches);
        }

        /// <summary>
        ///     Finds all occurrences of a sequential pattern.
        ///     查找顺序模式的所有匹配。
        /// </summary>
        public HarmonyIlMatches FindMatches(HarmonyIlPattern pattern, string description = "IL pattern")
        {
            ArgumentNullException.ThrowIfNull(pattern);
            return pattern.FindMatches(_code, description);
        }

        /// <summary>
        ///     Finds the first local-load instruction for a local index.
        ///     查找指定本地变量索引的第一条读取指令。
        /// </summary>
        public bool TryFindLdloc(int index, out HarmonyIlMatch match)
        {
            return TryFindFirst(HarmonyIl.IsLdloc(index), out match);
        }

        /// <summary>
        ///     Finds the first local-store instruction for a local index.
        ///     查找指定本地变量索引的第一条存储指令。
        /// </summary>
        public bool TryFindStloc(int index, out HarmonyIlMatch match)
        {
            return TryFindFirst(HarmonyIl.IsStloc(index), out match);
        }

        /// <summary>
        ///     Finds the first local-load instruction for a local type.
        ///     查找指定本地变量类型的第一条读取指令。
        /// </summary>
        public bool TryFindLdlocOfType(Type localType, out HarmonyIlMatch match)
        {
            ArgumentNullException.ThrowIfNull(localType);
            return TryFindFirst(
                instruction => TryGetLocalLoad(instruction, out var local) && local.LocalType == localType,
                out match);
        }

        /// <summary>
        ///     Finds the first local-load instruction for a local type.
        ///     查找指定本地变量类型的第一条读取指令。
        /// </summary>
        public bool TryFindLdlocOfType<T>(out HarmonyIlMatch match)
        {
            return TryFindLdlocOfType(typeof(T), out match);
        }

        /// <summary>
        ///     Finds the first local-store instruction for a local type.
        ///     查找指定本地变量类型的第一条存储指令。
        /// </summary>
        public bool TryFindStlocOfType(Type localType, out HarmonyIlMatch match)
        {
            ArgumentNullException.ThrowIfNull(localType);
            return TryFindFirst(
                instruction => TryGetLocalStore(instruction, out var local) && local.LocalType == localType,
                out match);
        }

        /// <summary>
        ///     Finds the first local-store instruction for a local type.
        ///     查找指定本地变量类型的第一条存储指令。
        /// </summary>
        public bool TryFindStlocOfType<T>(out HarmonyIlMatch match)
        {
            return TryFindStlocOfType(typeof(T), out match);
        }

        /// <summary>
        ///     Finds the first call/callvirt instruction for a method.
        ///     查找指定方法的第一条 call/callvirt 指令。
        /// </summary>
        public bool TryFindCall(MethodInfo? method, out HarmonyIlMatch match)
        {
            return TryFindFirst(HarmonyIl.IsCall(method), out match);
        }

        /// <summary>
        ///     Finds the first call/callvirt instruction using a method predicate.
        ///     使用方法谓词查找第一条 call/callvirt 指令。
        /// </summary>
        public bool TryFindCall(Func<MethodInfo, bool> predicate, out HarmonyIlMatch match)
        {
            return TryFindFirst(HarmonyIl.IsCall(predicate), out match);
        }

        /// <summary>
        ///     Finds all call/callvirt instructions using a method predicate.
        ///     使用方法谓词查找所有 call/callvirt 指令。
        /// </summary>
        public HarmonyIlMatches FindCalls(Func<MethodInfo, bool> predicate, string description = "IL call")
        {
            return FindAll(HarmonyIl.IsCall(predicate), description);
        }

        /// <summary>
        ///     Finds the first field access instruction for a field.
        ///     查找指定字段的第一条字段访问指令。
        /// </summary>
        public bool TryFindFieldAccess(FieldInfo? field, out HarmonyIlMatch match)
        {
            return TryFindFirst(HarmonyIl.IsFieldAccess(field), out match);
        }

        private bool TryGetLocalLoad(CodeInstruction instruction, out HarmonyIlLocalRef local)
        {
            return HarmonyIl.TryGetLocalLoad(instruction, out local) && TryEnrichLocal(ref local);
        }

        private bool TryGetLocalStore(CodeInstruction instruction, out HarmonyIlLocalRef local)
        {
            return HarmonyIl.TryGetLocalStore(instruction, out local) && TryEnrichLocal(ref local);
        }

        private bool TryEnrichLocal(ref HarmonyIlLocalRef local)
        {
            if (local.LocalType != null || Context?.TryGetLocalType(local.Index, out var localType) != true)
                return true;

            local = local with { LocalType = localType };
            return true;
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

            var found = pattern.FindAll(_code);
            if (found.Count == 0)
                return NewReport(operation, [], [], before, alreadySatisfied);

            var match = found[0];
            Replace(match, replacement);
            return new(operation, found.Count, 1, before, _code.Count, MatchedIndexes: MatchIndexes(found),
                AppliedIndexes: [match.Index]);
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
            var matchedIndexes = new List<int>();
            var appliedIndexes = new List<int>();
            for (var i = 0; i < _code.Count; i++)
            {
                if (!isMatch(_code, i))
                    continue;

                matchedIndexes.Add(i);
                var replacement = buildReplacement(_code, i).ToList();
                if (replacement.Count == 0)
                    continue;

                appliedIndexes.Add(i);
                HarmonyIl.MoveMetadataToFirst(_code[i], replacement);
                _code.RemoveAt(i);
                _code.InsertRange(i, replacement);
                i += replacement.Count - 1;
            }

            return NewReport(operation, matchedIndexes, appliedIndexes, before, alreadySatisfied);
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
            var matchedIndexes = new List<int>();
            var appliedIndexes = new List<int>();
            for (var i = 0; i < _code.Count; i++)
            {
                var instruction = _code[i];
                if (instruction.opcode != OpCodes.Call && instruction.opcode != OpCodes.Callvirt)
                    continue;
                if (instruction.operand is not MethodInfo called)
                    continue;

                var replacement = resolveReplacement(called);
                if (replacement == null)
                    continue;

                matchedIndexes.Add(i);
                appliedIndexes.Add(i);
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
            }

            return NewReport(operation, matchedIndexes, appliedIndexes, before, alreadySatisfied);
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
                return NewReport(operation, [], [], before, alreadySatisfied);

            var matchedIndexes = new List<int>();
            for (var i = 0; i < _code.Count; i++)
            {
                if (_code[i].opcode != OpCodes.Ret)
                    continue;

                matchedIndexes.Add(i);
                InsertAt(i, prefix, moveLabelsAndBlocksToInserted ? _code[i] : null);
                return new(operation, matchedIndexes.Count, 1, before, _code.Count,
                    MatchedIndexes: matchedIndexes, AppliedIndexes: [i]);
            }

            return NewReport(operation, matchedIndexes, [], before, alreadySatisfied);
        }

        /// <summary>
        ///     Inserts instructions before every ret opcode and returns a rewrite report.
        ///     在每条 ret 指令前插入指令并返回改写报告。
        /// </summary>
        public HarmonyIlRewriteReport InsertBeforeEachRet(
            string operation,
            IReadOnlyList<CodeInstruction> prefix,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null,
            bool moveLabelsAndBlocksToInserted = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);
            ArgumentNullException.ThrowIfNull(prefix);

            var before = _code.Count;
            if (prefix.Count == 0)
                return NewReport(operation, [], [], before, alreadySatisfied);

            var matchedIndexes = new List<int>();
            var appliedIndexes = new List<int>();
            for (var i = 0; i < _code.Count; i++)
            {
                if (_code[i].opcode != OpCodes.Ret)
                    continue;

                matchedIndexes.Add(i);
                appliedIndexes.Add(i);
                InsertAt(i, HarmonyIl.CloneAll(prefix), moveLabelsAndBlocksToInserted ? _code[i] : null);
                i += prefix.Count;
            }

            return NewReport(operation, matchedIndexes, appliedIndexes, before, alreadySatisfied);
        }

        /// <summary>
        ///     Inserts instructions before the last ret opcode and returns a rewrite report.
        ///     在最后一条 ret 指令前插入指令并返回改写报告。
        /// </summary>
        public HarmonyIlRewriteReport InsertBeforeLastRet(
            string operation,
            IReadOnlyList<CodeInstruction> prefix,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null,
            bool moveLabelsAndBlocksToInserted = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);
            ArgumentNullException.ThrowIfNull(prefix);

            var before = _code.Count;
            if (prefix.Count == 0)
                return NewReport(operation, [], [], before, alreadySatisfied);

            var matchedIndexes = FindAll(HarmonyIl.IsRet(), operation).Items.Select(static match => match.Index)
                .ToList();
            if (matchedIndexes.Count == 0)
                return NewReport(operation, matchedIndexes, [], before, alreadySatisfied);

            var index = matchedIndexes[^1];
            InsertAt(index, prefix, moveLabelsAndBlocksToInserted ? _code[index] : null);
            return new(operation, matchedIndexes.Count, 1, before, _code.Count,
                MatchedIndexes: matchedIndexes, AppliedIndexes: [index]);
        }

        /// <summary>
        ///     Inserts instructions before the only ret opcode and returns a rewrite report.
        ///     在唯一一条 ret 指令前插入指令并返回改写报告。
        /// </summary>
        public HarmonyIlRewriteReport InsertBeforeSingleRet(
            string operation,
            IReadOnlyList<CodeInstruction> prefix,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null,
            bool moveLabelsAndBlocksToInserted = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);
            ArgumentNullException.ThrowIfNull(prefix);

            var before = _code.Count;
            if (prefix.Count == 0)
                return NewReport(operation, [], [], before, alreadySatisfied);

            var retMatches = FindAll(HarmonyIl.IsRet(), operation);
            var ret = retMatches.RequireSingle();
            InsertAt(ret.Index, prefix, moveLabelsAndBlocksToInserted ? _code[ret.Index] : null);
            return new(operation, retMatches.Count, 1, before, _code.Count,
                MatchedIndexes: retMatches.Items.Select(static match => match.Index).ToArray(),
                AppliedIndexes: [ret.Index]);
        }

        /// <summary>
        ///     Inserts instructions before the first call/callvirt to a method.
        ///     在第一次调用指定方法前插入指令。
        /// </summary>
        public HarmonyIlRewriteReport InsertBeforeCall(
            string operation,
            MethodInfo method,
            IEnumerable<CodeInstruction> instructions,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null,
            bool moveLabelsAndBlocksToInserted = false)
        {
            ArgumentNullException.ThrowIfNull(method);
            return TryInsertBeforeFirst(operation, HarmonyIlPattern.Sequence(HarmonyIl.IsCall(method)), instructions,
                alreadySatisfied, moveLabelsAndBlocksToInserted);
        }

        /// <summary>
        ///     Inserts instructions after the first call/callvirt to a method.
        ///     在第一次调用指定方法后插入指令。
        /// </summary>
        public HarmonyIlRewriteReport InsertAfterCall(
            string operation,
            MethodInfo method,
            IEnumerable<CodeInstruction> instructions,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null)
        {
            ArgumentNullException.ThrowIfNull(method);
            return TryInsertAfterFirst(operation, HarmonyIlPattern.Sequence(HarmonyIl.IsCall(method)), instructions,
                alreadySatisfied);
        }

        /// <summary>
        ///     Replaces calls to one method with calls to another method.
        ///     将一个方法调用替换为另一个方法调用。
        /// </summary>
        public HarmonyIlRewriteReport ReplaceCall(
            string operation,
            MethodInfo fromMethod,
            MethodInfo toMethod,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null)
        {
            ArgumentNullException.ThrowIfNull(fromMethod);
            ArgumentNullException.ThrowIfNull(toMethod);
            return RedirectCalls(operation, called => called == fromMethod ? toMethod : null, alreadySatisfied);
        }

        /// <summary>
        ///     Replaces every instruction that satisfies <paramref name="isMatch" /> with the supplied replacement instructions.
        ///     将每条满足 <paramref name="isMatch" /> 的指令替换为指定指令。
        /// </summary>
        public HarmonyIlRewriteReport ReplaceInstructions(
            string operation,
            Func<CodeInstruction, bool> isMatch,
            IEnumerable<CodeInstruction> replacement,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);
            ArgumentNullException.ThrowIfNull(isMatch);
            ArgumentNullException.ThrowIfNull(replacement);

            var template = replacement.ToList();
            return ReplaceEach(operation,
                (code, index) => isMatch(code[index]),
                (_, _) => HarmonyIl.CloneAll(template),
                alreadySatisfied);
        }

        /// <summary>
        ///     Replaces every instruction that satisfies <paramref name="isMatch" /> using a per-instruction replacement builder.
        ///     使用逐指令替换构造器替换每条满足 <paramref name="isMatch" /> 的指令。
        /// </summary>
        public HarmonyIlRewriteReport ReplaceInstructions(
            string operation,
            Func<CodeInstruction, bool> isMatch,
            Func<CodeInstruction, IReadOnlyList<CodeInstruction>> buildReplacement,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);
            ArgumentNullException.ThrowIfNull(isMatch);
            ArgumentNullException.ThrowIfNull(buildReplacement);

            return ReplaceEach(operation,
                (code, index) => isMatch(code[index]),
                (code, index) => buildReplacement(code[index]),
                alreadySatisfied);
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

            var found = pattern.FindAll(_code);
            if (found.Count == 0)
                return NewReport(operation, [], [], before, alreadySatisfied);

            var match = found[0];
            if (insertAfter)
                InsertAfter(match, instructions);
            else
                InsertBefore(match, instructions, moveLabelsAndBlocksToInserted);

            return new(operation, found.Count, 1, before, _code.Count, MatchedIndexes: MatchIndexes(found),
                AppliedIndexes: [match.Index]);
        }

        private HarmonyIlRewriteReport NewReport(
            string operation,
            IReadOnlyList<int> matchedIndexes,
            IReadOnlyList<int> appliedIndexes,
            int before,
            Func<IReadOnlyList<CodeInstruction>, bool>? alreadySatisfied)
        {
            return new(
                operation,
                matchedIndexes.Count,
                appliedIndexes.Count,
                before,
                _code.Count,
                appliedIndexes.Count == 0 && alreadySatisfied?.Invoke(_code) == true,
                matchedIndexes,
                appliedIndexes);
        }

        private static int[] MatchIndexes(IReadOnlyList<HarmonyIlMatch> matches)
        {
            return matches.Select(static match => match.Index).ToArray();
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
