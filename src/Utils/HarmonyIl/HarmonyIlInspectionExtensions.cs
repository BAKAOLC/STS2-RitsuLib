using System.Reflection;
using HarmonyLib;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     <para xml:lang="en">A snapshot of the original IL body selected for a logical method.</para>
    ///     <para xml:lang="zh-CN">为逻辑方法选定的原始 IL 方法体快照。</para>
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
        ///     <para xml:lang="en">Logical method requested by the caller.</para>
        ///     <para xml:lang="zh-CN">调用方请求检查的逻辑方法。</para>
        /// </summary>
        public MethodBase SourceMethod { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Method that owns <see cref="Instructions" />. For a resolved async method this is its generated
        ///         <c>MoveNext</c> method.
        ///     </para>
        ///     <para xml:lang="zh-CN">拥有 <see cref="Instructions" /> 的方法。解析异步方法时，这是编译器生成的 <c>MoveNext</c> 方法。</para>
        /// </summary>
        public MethodBase BodyMethod { get; }

        /// <summary>
        ///     <para xml:lang="en">True when the logical method was resolved to an async state-machine body.</para>
        ///     <para xml:lang="zh-CN">逻辑方法已解析为异步状态机方法体时为 true。</para>
        /// </summary>
        public bool IsAsyncStateMachineBody => SourceMethod != BodyMethod;

        /// <summary>
        ///     <para xml:lang="en">Original Harmony instructions for <see cref="BodyMethod" />.</para>
        ///     <para xml:lang="zh-CN"><see cref="BodyMethod" /> 的 Harmony 原始指令。</para>
        /// </summary>
        public IReadOnlyList<CodeInstruction> Instructions { get; }

        /// <summary>
        ///     <para xml:lang="en">Call/callvirt targets in instruction order. Repeated call sites are preserved.</para>
        ///     <para xml:lang="zh-CN">按指令顺序排列的 call/callvirt 目标；重复调用点会被保留。</para>
        /// </summary>
        public IReadOnlyList<MethodInfo> CalledMethods { get; }

        /// <summary>
        ///     <para xml:lang="en">Creates an isolated mutable rewriter over cloned instructions.</para>
        ///     <para xml:lang="zh-CN">基于克隆指令创建独立的可变改写器。</para>
        /// </summary>
        public HarmonyIlRewriter CreateRewriter()
        {
            return HarmonyIlRewriter.From(HarmonyIl.CloneAll(Instructions), BodyMethod);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns true when any call/callvirt target satisfies <paramref name="predicate" />.</para>
        ///     <para xml:lang="zh-CN">任一 call/callvirt 目标满足 <paramref name="predicate" /> 时返回 true。</para>
        /// </summary>
        public bool HasCall(Func<MethodInfo, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return CalledMethods.Any(predicate);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         A shortest call path found by
    ///         <see cref="HarmonyIlInspectionExtensions.FindOriginalIlCallPath" />.
    ///     </para>
    ///     <para xml:lang="zh-CN"><see cref="HarmonyIlInspectionExtensions.FindOriginalIlCallPath" /> 找到的最短调用路径。</para>
    /// </summary>
    public sealed class HarmonyIlCallPath
    {
        internal HarmonyIlCallPath(IReadOnlyList<MethodBase> methods)
        {
            Methods = methods;
        }

        /// <summary>
        ///     <para xml:lang="en">Logical methods from the root through the matched call target.</para>
        ///     <para xml:lang="zh-CN">从根方法到匹配调用目标的逻辑方法序列。</para>
        /// </summary>
        public IReadOnlyList<MethodBase> Methods { get; }

        /// <summary>
        ///     <para xml:lang="en">Root logical method.</para>
        ///     <para xml:lang="zh-CN">根逻辑方法。</para>
        /// </summary>
        public MethodBase Root => Methods[0];

        /// <summary>
        ///     <para xml:lang="en">Matched call target.</para>
        ///     <para xml:lang="zh-CN">匹配到的调用目标。</para>
        /// </summary>
        public MethodInfo Target => (MethodInfo)Methods[^1];

        /// <summary>
        ///     <para xml:lang="en">Number of traversed intermediate methods. A direct call has depth zero.</para>
        ///     <para xml:lang="zh-CN">已下钻的中间方法数量；直接调用的深度为零。</para>
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
    ///     <para xml:lang="en">
    ///         Convenience extensions for inspecting original Harmony IL and following explicitly selected
    ///         calls.
    ///     </para>
    ///     <para xml:lang="zh-CN">用于检查 Harmony 原始 IL 并沿显式选定调用下钻的快捷扩展。</para>
    /// </summary>
    public static class HarmonyIlInspectionExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Reads the original IL for a logical method. Async methods resolve to their generated
        ///         <c>MoveNext</c> body by default.
        ///     </para>
        ///     <para xml:lang="zh-CN">读取逻辑方法的原始 IL。默认将异步方法解析到编译器生成的 <c>MoveNext</c> 方法体。</para>
        /// </summary>
        /// <param name="method">
        ///     <para xml:lang="en">Logical method to inspect.</para>
        ///     <para xml:lang="zh-CN">要检查的逻辑方法。</para>
        /// </param>
        /// <param name="resolveAsync">
        ///     <para xml:lang="en">Resolve an async method to its generated state-machine body.</para>
        ///     <para xml:lang="zh-CN">将异步方法解析到生成的状态机方法体。</para>
        /// </param>
        public static HarmonyIlMethodBody GetOriginalIl(this MethodBase method, bool resolveAsync = true)
        {
            ArgumentNullException.ThrowIfNull(method);

            var bodyMethod = resolveAsync ? AccessTools.AsyncMoveNext(method) ?? method : method;
            IReadOnlyList<CodeInstruction> instructions;
            try
            {
                instructions = PatchProcessor.GetOriginalInstructions(bodyMethod, out _);
            }
            catch (NotSupportedException)
            {
                instructions = HarmonyIlMethodReader.Read(bodyMethod);
            }

            return new(method, bodyMethod, instructions);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns true when the selected original IL call graph contains a matching call target.</para>
        ///     <para xml:lang="zh-CN">选定的原始 IL 调用图包含匹配调用目标时返回 true。</para>
        /// </summary>
        /// <param name="method">
        ///     <para xml:lang="en">Root logical method.</para>
        ///     <para xml:lang="zh-CN">根逻辑方法。</para>
        /// </param>
        /// <param name="isTarget">
        ///     <para xml:lang="en">Identifies a target call. This predicate runs before traversal filtering.</para>
        ///     <para xml:lang="zh-CN">识别目标调用；此谓词先于下钻过滤执行。</para>
        /// </param>
        /// <param name="shouldTraverse">
        ///     <para xml:lang="en">Selects helper methods whose bodies may be inspected. Null performs a direct-call-only query.</para>
        ///     <para xml:lang="zh-CN">选择允许继续检查方法体的辅助方法；为 null 时仅查询直接调用。</para>
        /// </param>
        /// <param name="maxDepth">
        ///     <para xml:lang="en">Maximum number of intermediate helper methods. A direct call has depth zero.</para>
        ///     <para xml:lang="zh-CN">最大中间辅助方法数量；直接调用的深度为零。</para>
        /// </param>
        /// <param name="resolveAsync">
        ///     <para xml:lang="en">Resolves each inspected async method to its generated state-machine body.</para>
        ///     <para xml:lang="zh-CN">将每个被检查的异步方法解析到生成的状态机方法体。</para>
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
        ///     <para xml:lang="en">Finds the shortest path to a matching call target in the selected original IL call graph.</para>
        ///     <para xml:lang="zh-CN">在选定的原始 IL 调用图中查找到匹配调用目标的最短路径。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Traversal is opt-in. The helper does not infer virtual dispatch, delegate targets, reflection
        ///         calls, or methods not accepted by <paramref name="shouldTraverse" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">下钻需显式启用。本工具不会推断虚调用分派、委托目标或反射调用，也不会检查未被 <paramref name="shouldTraverse" /> 接受的方法。</para>
        /// </remarks>
        /// <param name="method">
        ///     <para xml:lang="en">Root logical method.</para>
        ///     <para xml:lang="zh-CN">根逻辑方法。</para>
        /// </param>
        /// <param name="isTarget">
        ///     <para xml:lang="en">Identifies a target call. This predicate runs before traversal filtering.</para>
        ///     <para xml:lang="zh-CN">识别目标调用；此谓词先于下钻过滤执行。</para>
        /// </param>
        /// <param name="shouldTraverse">
        ///     <para xml:lang="en">Selects helper methods whose bodies may be inspected. Null performs a direct-call-only query.</para>
        ///     <para xml:lang="zh-CN">选择允许继续检查方法体的辅助方法；为 null 时仅查询直接调用。</para>
        /// </param>
        /// <param name="maxDepth">
        ///     <para xml:lang="en">Maximum number of intermediate helper methods. A direct call has depth zero.</para>
        ///     <para xml:lang="zh-CN">最大中间辅助方法数量；直接调用的深度为零。</para>
        /// </param>
        /// <param name="resolveAsync">
        ///     <para xml:lang="en">Resolve each inspected async method to its generated state-machine body.</para>
        ///     <para xml:lang="zh-CN">将每个被检查的异步方法解析到生成的状态机方法体。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The shortest matching path, or null when no selected path reaches a target.</para>
        ///     <para xml:lang="zh-CN">最短匹配路径；没有选定路径到达目标时为 null。</para>
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
