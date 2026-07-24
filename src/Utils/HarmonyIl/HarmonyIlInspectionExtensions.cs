using System.Reflection;
using HarmonyLib;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     A snapshot of the original IL body selected for a logical method.
    ///     为逻辑方法选定的原始 IL 方法体快照。
    /// </summary>
    public sealed class HarmonyIlMethodBody
    {
        internal HarmonyIlMethodBody(
            MethodBase sourceMethod,
            MethodBase bodyMethod,
            IReadOnlyList<CodeInstruction> instructions)
        {
            SourceMethod = sourceMethod;
            BodyMethod = bodyMethod;
            Instructions = instructions;
            CalledMethods =
            [
                .. instructions
                    .Select(static instruction =>
                        HarmonyIl.TryGetCalledMethod(instruction, out var called) ? called : null)
                    .Where(static method => method != null)
                    .Cast<MethodInfo>(),
            ];
        }

        /// <summary>
        ///     Logical method requested by the caller.
        ///     调用方请求检查的逻辑方法。
        /// </summary>
        public MethodBase SourceMethod { get; }

        /// <summary>
        ///     Method that owns <see cref="Instructions" />. For a resolved async method this is its generated
        ///     <c>MoveNext</c> method.
        ///     拥有 <see cref="Instructions" /> 的方法。解析 async 方法时，这是编译器生成的 <c>MoveNext</c> 方法。
        /// </summary>
        public MethodBase BodyMethod { get; }

        /// <summary>
        ///     True when the logical method was resolved to an async state-machine body.
        ///     逻辑方法已解析为 async 状态机方法体时为 true。
        /// </summary>
        public bool IsAsyncStateMachineBody => SourceMethod != BodyMethod;

        /// <summary>
        ///     Original Harmony instructions for <see cref="BodyMethod" />.
        ///     <see cref="BodyMethod" /> 的 Harmony 原始指令。
        /// </summary>
        public IReadOnlyList<CodeInstruction> Instructions { get; }

        /// <summary>
        ///     Call/callvirt targets in instruction order. Repeated call sites are preserved.
        ///     按指令顺序排列的 call/callvirt 目标；重复调用点会被保留。
        /// </summary>
        public IReadOnlyList<MethodInfo> CalledMethods { get; }

        /// <summary>
        ///     Creates an isolated mutable rewriter over cloned instructions.
        ///     基于克隆指令创建独立的可变 rewriter。
        /// </summary>
        public HarmonyIlRewriter CreateRewriter()
        {
            return HarmonyIlRewriter.From(HarmonyIl.CloneAll(Instructions), BodyMethod);
        }

        /// <summary>
        ///     Returns true when any call/callvirt target satisfies <paramref name="predicate" />.
        ///     任一 call/callvirt 目标满足 <paramref name="predicate" /> 时返回 true。
        /// </summary>
        public bool HasCall(Func<MethodInfo, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return CalledMethods.Any(predicate);
        }
    }

    /// <summary>
    ///     A shortest call path found by <see cref="HarmonyIlInspectionExtensions.FindOriginalIlCallPath" />.
    ///     <see cref="HarmonyIlInspectionExtensions.FindOriginalIlCallPath" /> 找到的最短调用路径。
    /// </summary>
    public sealed class HarmonyIlCallPath
    {
        internal HarmonyIlCallPath(IReadOnlyList<MethodBase> methods)
        {
            Methods = methods;
        }

        /// <summary>
        ///     Logical methods from the root through the matched call target.
        ///     从根方法到匹配调用目标的逻辑方法序列。
        /// </summary>
        public IReadOnlyList<MethodBase> Methods { get; }

        /// <summary>
        ///     Root logical method.
        ///     根逻辑方法。
        /// </summary>
        public MethodBase Root => Methods[0];

        /// <summary>
        ///     Matched call target.
        ///     匹配到的调用目标。
        /// </summary>
        public MethodInfo Target => (MethodInfo)Methods[^1];

        /// <summary>
        ///     Number of traversed intermediate methods. A direct call has depth zero.
        ///     已下钻的中间方法数量；直接调用的深度为零。
        /// </summary>
        public int TraversalDepth => Math.Max(0, Methods.Count - 2);

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Join(" -> ", Methods.Select(FormatMethod));
        }

        private static string FormatMethod(MethodBase method)
        {
            return $"{method.DeclaringType?.FullName ?? "<global>"}.{method.Name}";
        }
    }

    /// <summary>
    ///     Convenience extensions for inspecting original Harmony IL and following explicitly selected calls.
    ///     用于检查 Harmony 原始 IL 并沿显式选定调用下钻的快捷扩展。
    /// </summary>
    public static class HarmonyIlInspectionExtensions
    {
        /// <summary>
        ///     Reads the original IL for a logical method. Async methods resolve to their generated
        ///     <c>MoveNext</c> body by default.
        ///     读取逻辑方法的原始 IL。默认将 async 方法解析到编译器生成的 <c>MoveNext</c> 方法体。
        /// </summary>
        /// <param name="method">
        ///     Logical method to inspect.
        ///     要检查的逻辑方法。
        /// </param>
        /// <param name="resolveAsync">
        ///     Resolve an async method to its generated state-machine body.
        ///     将 async 方法解析到生成的状态机方法体。
        /// </param>
        public static HarmonyIlMethodBody GetOriginalIl(this MethodBase method, bool resolveAsync = true)
        {
            ArgumentNullException.ThrowIfNull(method);

            var bodyMethod = resolveAsync ? AccessTools.AsyncMoveNext(method) ?? method : method;
            var instructions = PatchProcessor.GetOriginalInstructions(bodyMethod, out _);
            return new(method, bodyMethod, instructions);
        }

        /// <summary>
        ///     Returns true when the selected original IL call graph contains a matching call target.
        ///     选定的原始 IL 调用图包含匹配调用目标时返回 true。
        /// </summary>
        /// <param name="method">
        ///     Root logical method.
        ///     根逻辑方法。
        /// </param>
        /// <param name="isTarget">
        ///     Identifies a target call. This predicate runs before traversal filtering.
        ///     识别目标调用；此谓词先于下钻过滤执行。
        /// </param>
        /// <param name="shouldTraverse">
        ///     Selects helper methods whose bodies may be inspected. Null performs a direct-call-only query.
        ///     选择允许继续检查方法体的辅助方法；为 null 时仅查询直接调用。
        /// </param>
        /// <param name="maxDepth">
        ///     Maximum number of intermediate helper methods. A direct call has depth zero.
        ///     最大中间辅助方法数量；直接调用的深度为零。
        /// </param>
        /// <param name="resolveAsync">
        ///     Resolve each inspected async method to its generated state-machine body.
        ///     将每个被检查的 async 方法解析到生成的状态机方法体。
        /// </param>
        public static bool HasOriginalIlCall(
            this MethodBase method,
            Func<MethodInfo, bool> isTarget,
            Func<MethodInfo, bool>? shouldTraverse = null,
            int maxDepth = 8,
            bool resolveAsync = true)
        {
            return method.FindOriginalIlCallPath(isTarget, shouldTraverse, maxDepth, resolveAsync) != null;
        }

        /// <summary>
        ///     Finds the shortest path to a matching call target in the selected original IL call graph.
        ///     在选定的原始 IL 调用图中查找到匹配调用目标的最短路径。
        /// </summary>
        /// <remarks>
        ///     Traversal is opt-in. The helper does not infer virtual dispatch, delegate targets, reflection calls,
        ///     or methods that are not accepted by <paramref name="shouldTraverse" />.
        ///     下钻是显式选择的。本工具不会推断虚调用分派、委托目标、反射调用，也不会检查未被
        ///     <paramref name="shouldTraverse" /> 接受的方法。
        /// </remarks>
        /// <param name="method">
        ///     Root logical method.
        ///     根逻辑方法。
        /// </param>
        /// <param name="isTarget">
        ///     Identifies a target call. This predicate runs before traversal filtering.
        ///     识别目标调用；此谓词先于下钻过滤执行。
        /// </param>
        /// <param name="shouldTraverse">
        ///     Selects helper methods whose bodies may be inspected. Null performs a direct-call-only query.
        ///     选择允许继续检查方法体的辅助方法；为 null 时仅查询直接调用。
        /// </param>
        /// <param name="maxDepth">
        ///     Maximum number of intermediate helper methods. A direct call has depth zero.
        ///     最大中间辅助方法数量；直接调用的深度为零。
        /// </param>
        /// <param name="resolveAsync">
        ///     Resolve each inspected async method to its generated state-machine body.
        ///     将每个被检查的 async 方法解析到生成的状态机方法体。
        /// </param>
        /// <returns>
        ///     The shortest matching path, or null when no selected path reaches a target.
        ///     最短匹配路径；没有选定路径到达目标时为 null。
        /// </returns>
        public static HarmonyIlCallPath? FindOriginalIlCallPath(
            this MethodBase method,
            Func<MethodInfo, bool> isTarget,
            Func<MethodInfo, bool>? shouldTraverse = null,
            int maxDepth = 8,
            bool resolveAsync = true)
        {
            ArgumentNullException.ThrowIfNull(method);
            ArgumentNullException.ThrowIfNull(isTarget);
            ArgumentOutOfRangeException.ThrowIfNegative(maxDepth);

            var pending = new Queue<CallGraphNode>();
            pending.Enqueue(new(method, [method], 0));
            var visited = new HashSet<MethodBase>();

            while (pending.TryDequeue(out var node))
            {
                if (!visited.Add(node.Method))
                    continue;

                var body = node.Method.GetOriginalIl(resolveAsync);
                foreach (var calledMethod in body.CalledMethods)
                {
                    var path = Append(node.Path, calledMethod);
                    if (isTarget(calledMethod))
                        return new(path);

                    if (shouldTraverse == null ||
                        node.TraversalDepth >= maxDepth ||
                        !shouldTraverse(calledMethod))
                        continue;

                    pending.Enqueue(new(calledMethod, path, node.TraversalDepth + 1));
                }
            }

            return null;
        }

        private static IReadOnlyList<MethodBase> Append(
            IReadOnlyList<MethodBase> path,
            MethodBase calledMethod)
        {
            var result = new MethodBase[path.Count + 1];
            for (var i = 0; i < path.Count; i++)
                result[i] = path[i];
            result[^1] = calledMethod;
            return result;
        }

        private sealed record CallGraphNode(
            MethodBase Method,
            IReadOnlyList<MethodBase> Path,
            int TraversalDepth);
    }
}
