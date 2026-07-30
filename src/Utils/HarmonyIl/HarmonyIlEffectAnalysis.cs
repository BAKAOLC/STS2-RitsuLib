using System.Reflection;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     <para xml:lang="en">Named terminal method predicate used by <see cref="HarmonyIlEffectAnalyzer" />.</para>
    ///     <para xml:lang="zh-CN"><see cref="HarmonyIlEffectAnalyzer" /> 使用的具名终点方法谓词。</para>
    /// </summary>
    public sealed record HarmonyIlEffectSink(
        string Id,
        Func<MethodInfo, bool> IsMatch)
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a sink that matches one exact method.</para>
        ///     <para xml:lang="zh-CN">创建匹配一个确切方法的效果终点。</para>
        /// </summary>
        public static HarmonyIlEffectSink ForMethod(string id, MethodInfo method)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(method);
            return new(id, candidate => candidate == method);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Limits and traversal policy for an IL effect analysis.</para>
    ///     <para xml:lang="zh-CN">IL 效果分析的限制与遍历策略。</para>
    /// </summary>
    public sealed record HarmonyIlEffectAnalysisOptions
    {
        /// <summary>
        ///     <para xml:lang="en">Maximum number of traversed calls from a root.</para>
        ///     <para xml:lang="zh-CN">从根方法开始允许下钻的最大调用层数。</para>
        /// </summary>
        public int MaxDepth { get; init; } = 16;

        /// <summary>
        ///     <para xml:lang="en">Maximum number of distinct logical methods inspected.</para>
        ///     <para xml:lang="zh-CN">允许检查的最大不同逻辑方法数量。</para>
        /// </summary>
        public int MaxMethods { get; init; } = 4096;

        /// <summary>
        ///     <para xml:lang="en">Resolve async logical methods to compiler-generated <c>MoveNext</c> bodies.</para>
        ///     <para xml:lang="zh-CN">将异步逻辑方法解析到编译器生成的 <c>MoveNext</c> 方法体。</para>
        /// </summary>
        public bool ResolveAsync { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Selects non-sink calls whose bodies may be inspected. Null keeps analysis direct-call-only.</para>
        ///     <para xml:lang="zh-CN">选择允许继续检查方法体的非终点调用；为 null 时仅分析直接调用。</para>
        /// </summary>
        public Func<MethodInfo, bool>? ShouldTraverse { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Severity of an IL effect-analysis diagnostic.</para>
    ///     <para xml:lang="zh-CN">IL 效果分析诊断的严重程度。</para>
    /// </summary>
    public enum HarmonyIlEffectDiagnosticSeverity
    {
        /// <summary>
        ///     <para xml:lang="en">Informational analysis detail.</para>
        ///     <para xml:lang="zh-CN">信息性分析详情。</para>
        /// </summary>
        Information,

        /// <summary>
        ///     <para xml:lang="en">Recoverable condition that may reduce completeness.</para>
        ///     <para xml:lang="zh-CN">可能降低完整性的可恢复状况。</para>
        /// </summary>
        Warning,

        /// <summary>
        ///     <para xml:lang="en">Condition that prevents complete analysis.</para>
        ///     <para xml:lang="zh-CN">阻止完整分析的错误状况。</para>
        /// </summary>
        Error,
    }

    /// <summary>
    ///     <para xml:lang="en">One diagnostic emitted by <see cref="HarmonyIlEffectAnalyzer" />.</para>
    ///     <para xml:lang="zh-CN"><see cref="HarmonyIlEffectAnalyzer" /> 产生的一条诊断。</para>
    /// </summary>
    public sealed record HarmonyIlEffectDiagnostic(
        HarmonyIlEffectDiagnosticSeverity Severity,
        MethodBase? Method,
        string Message);

    /// <summary>
    ///     <para xml:lang="en">A call site that directly reaches a sink or another effect-relevant method.</para>
    ///     <para xml:lang="zh-CN">直接到达效果终点或另一个效果相关方法的调用点。</para>
    /// </summary>
    public sealed record HarmonyIlEffectCallSite(
        int InstructionIndex,
        MethodInfo CalledMethod,
        string? SinkId,
        bool ReachesRelevantMethod)
    {
        /// <summary>
        ///     <para xml:lang="en">True when this call directly matches a configured sink.</para>
        ///     <para xml:lang="zh-CN">此调用直接匹配已配置效果终点时为 true。</para>
        /// </summary>
        public bool IsDirectSink => SinkId != null;
    }

    /// <summary>
    ///     <para xml:lang="en">Conservative control-flow slice for one effect-relevant logical method.</para>
    ///     <para xml:lang="zh-CN">一个效果相关逻辑方法的保守控制流切片。</para>
    /// </summary>
    public sealed class HarmonyIlEffectMethodSlice
    {
        internal HarmonyIlEffectMethodSlice(
            MethodBase method,
            HarmonyIlMethodBody body,
            HarmonyIlControlFlowGraph controlFlow,
            IReadOnlyList<HarmonyIlEffectCallSite> effectCallSites,
            IReadOnlySet<int> retainedBlockIndexes)
        {
            Method = method;
            Body = body;
            ControlFlow = controlFlow;
            EffectCallSites = effectCallSites;
            RetainedBlockIndexes = retainedBlockIndexes;
        }

        /// <summary>
        ///     <para xml:lang="en">Logical method represented by this slice.</para>
        ///     <para xml:lang="zh-CN">此切片所表示的逻辑方法。</para>
        /// </summary>
        public MethodBase Method { get; }

        /// <summary>
        ///     <para xml:lang="en">Resolved original IL body.</para>
        ///     <para xml:lang="zh-CN">解析后的原始 IL 方法体。</para>
        /// </summary>
        public HarmonyIlMethodBody Body { get; }

        /// <summary>
        ///     <para xml:lang="en">Full basic-block graph of <see cref="Body" />.</para>
        ///     <para xml:lang="zh-CN"><see cref="Body" /> 的完整基本块图。</para>
        /// </summary>
        public HarmonyIlControlFlowGraph ControlFlow { get; }

        /// <summary>
        ///     <para xml:lang="en">Effect-relevant calls in instruction order.</para>
        ///     <para xml:lang="zh-CN">按指令顺序排列的效果相关调用。</para>
        /// </summary>
        public IReadOnlyList<HarmonyIlEffectCallSite> EffectCallSites { get; }

        /// <summary>
        ///     <para xml:lang="en">Blocks that can reach an effect call through normal control flow.</para>
        ///     <para xml:lang="zh-CN">可通过普通控制流到达效果调用的基本块。</para>
        /// </summary>
        public IReadOnlySet<int> RetainedBlockIndexes { get; }

        /// <summary>
        ///     <para xml:lang="en">Returns true when an instruction belongs to a retained block.</para>
        ///     <para xml:lang="zh-CN">指令属于保留基本块时返回 true。</para>
        /// </summary>
        public bool RetainsInstruction(int instructionIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(instructionIndex);
            // Preserve the established exception construction and message.
#pragma warning disable CA1512
            if (instructionIndex >= ControlFlow.InstructionBlockIndexes.Count)
                throw new ArgumentOutOfRangeException(nameof(instructionIndex));
#pragma warning restore CA1512

            return RetainedBlockIndexes.Contains(
                ControlFlow.InstructionBlockIndexes[instructionIndex]);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Result of an interprocedural effect reachability and control-flow slicing pass.</para>
    ///     <para xml:lang="zh-CN">跨方法效果可达性与控制流切片分析结果。</para>
    /// </summary>
    public sealed class HarmonyIlEffectAnalysisResult
    {
        internal HarmonyIlEffectAnalysisResult(
            IReadOnlyList<MethodBase> roots,
            IReadOnlyDictionary<MethodBase, HarmonyIlEffectMethodSlice> slices,
            IReadOnlyList<HarmonyIlEffectDiagnostic> diagnostics)
        {
            Roots = roots;
            Slices = slices;
            Diagnostics = diagnostics;
        }

        /// <summary>
        ///     <para xml:lang="en">Logical root methods supplied to the analysis.</para>
        ///     <para xml:lang="zh-CN">提供给分析器的逻辑根方法。</para>
        /// </summary>
        public IReadOnlyList<MethodBase> Roots { get; }

        /// <summary>
        ///     <para xml:lang="en">Effect-relevant method slices keyed by logical method.</para>
        ///     <para xml:lang="zh-CN">以逻辑方法为键的效果相关方法切片。</para>
        /// </summary>
        public IReadOnlyDictionary<MethodBase, HarmonyIlEffectMethodSlice> Slices { get; }

        /// <summary>
        ///     <para xml:lang="en">Analysis diagnostics.</para>
        ///     <para xml:lang="zh-CN">分析诊断。</para>
        /// </summary>
        public IReadOnlyList<HarmonyIlEffectDiagnostic> Diagnostics { get; }

        /// <summary>
        ///     <para xml:lang="en">True when analysis completed without error diagnostics or incomplete control-flow graphs.</para>
        ///     <para xml:lang="zh-CN">分析未产生错误诊断且所有控制流图完整时为 true。</para>
        /// </summary>
        public bool IsComplete =>
            Diagnostics.All(static diagnostic =>
                diagnostic.Severity != HarmonyIlEffectDiagnosticSeverity.Error) &&
            Slices.Values.All(static slice => slice.ControlFlow.IsComplete);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Discovers calls that can reach configured effect sinks and creates conservative per-method
    ///         control slices.
    ///     </para>
    ///     <para xml:lang="zh-CN">发现可到达已配置效果终点的调用，并为每个相关方法创建保守控制切片。</para>
    /// </summary>
    public static class HarmonyIlEffectAnalyzer
    {
        /// <summary>
        ///     <para xml:lang="en">Analyzes logical root methods against named effect sinks.</para>
        ///     <para xml:lang="zh-CN">按具名效果终点分析逻辑根方法。</para>
        /// </summary>
        public static HarmonyIlEffectAnalysisResult Analyze(
            IEnumerable<MethodBase> roots,
            IEnumerable<HarmonyIlEffectSink> sinks,
            HarmonyIlEffectAnalysisOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(roots);
            ArgumentNullException.ThrowIfNull(sinks);

            var rootArray = roots.Distinct().ToArray();
            var sinkArray = sinks.ToArray();
            if (sinkArray.Any(static sink =>
                    string.IsNullOrWhiteSpace(sink.Id) || sink.IsMatch == null))
                throw new ArgumentException("Every effect sink must have an id and a predicate.", nameof(sinks));
            if (sinkArray.Select(static sink => sink.Id).Distinct(StringComparer.Ordinal).Count() !=
                sinkArray.Length)
                throw new ArgumentException("Effect sink ids must be unique.", nameof(sinks));

            options ??= new();
            ArgumentOutOfRangeException.ThrowIfNegative(options.MaxDepth);
            ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxMethods, 1);

            var diagnostics = new List<HarmonyIlEffectDiagnostic>();
            var nodes = DiscoverMethods(rootArray, sinkArray, options, diagnostics);
            var relevantMethods = FindRelevantMethods(nodes);
            var slices = BuildSlices(nodes, relevantMethods, diagnostics);
            return new(rootArray, slices, diagnostics);
        }

        private static Dictionary<MethodBase, MethodNode> DiscoverMethods(
            IReadOnlyList<MethodBase> roots,
            IReadOnlyList<HarmonyIlEffectSink> sinks,
            HarmonyIlEffectAnalysisOptions options,
            ICollection<HarmonyIlEffectDiagnostic> diagnostics)
        {
            var nodes = new Dictionary<MethodBase, MethodNode>();
            var pending = new Queue<(MethodBase Method, int Depth)>(
                roots.Select(static method => (method, 0)));
            var scheduled = roots.ToHashSet();

            while (pending.TryDequeue(out var item))
            {
                if (nodes.Count >= options.MaxMethods)
                {
                    diagnostics.Add(new(
                        HarmonyIlEffectDiagnosticSeverity.Error,
                        item.Method,
                        $"Method budget {options.MaxMethods} was exceeded."));
                    break;
                }

                HarmonyIlMethodBody body;
                try
                {
                    body = item.Method.GetOriginalIl(options.ResolveAsync);
                }
                catch (Exception exception)
                {
                    diagnostics.Add(new(
                        HarmonyIlEffectDiagnosticSeverity.Error,
                        item.Method,
                        $"Could not read original IL: {exception.Message}"));
                    continue;
                }

                var calls = ReadCalls(body, sinks);
                nodes[item.Method] = new(item.Method, body, calls);
                if (item.Depth >= options.MaxDepth || options.ShouldTraverse == null)
                    continue;

                foreach (var call in calls)
                {
                    if (call.SinkId != null ||
                        !options.ShouldTraverse(call.CalledMethod) ||
                        !scheduled.Add(call.CalledMethod))
                        continue;

                    pending.Enqueue((call.CalledMethod, item.Depth + 1));
                }
            }

            return nodes;
        }

        private static IReadOnlyList<DiscoveredCall> ReadCalls(
            HarmonyIlMethodBody body,
            IReadOnlyList<HarmonyIlEffectSink> sinks)
        {
            var result = new List<DiscoveredCall>();
            for (var instructionIndex = 0;
                 instructionIndex < body.Instructions.Count;
                 instructionIndex++)
            {
                if (!HarmonyIl.TryGetCalledMethod(
                        body.Instructions[instructionIndex],
                        out var calledMethod))
                    continue;

                var sinkId = sinks.FirstOrDefault(sink => sink.IsMatch(calledMethod))?.Id;
                result.Add(new(instructionIndex, calledMethod, sinkId));
            }

            return result;
        }

        private static HashSet<MethodBase> FindRelevantMethods(
            IReadOnlyDictionary<MethodBase, MethodNode> nodes)
        {
            var relevant = nodes.Values
                .Where(static node => node.Calls.Any(static call => call.SinkId != null))
                .Select(static node => node.Method)
                .ToHashSet();

            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var node in nodes.Values)
                {
                    if (relevant.Contains(node.Method) ||
                        !node.Calls.Any(call => relevant.Contains(call.CalledMethod)))
                        continue;

                    relevant.Add(node.Method);
                    changed = true;
                }
            }

            return relevant;
        }

        private static IReadOnlyDictionary<MethodBase, HarmonyIlEffectMethodSlice> BuildSlices(
            IReadOnlyDictionary<MethodBase, MethodNode> nodes,
            IReadOnlySet<MethodBase> relevantMethods,
            ICollection<HarmonyIlEffectDiagnostic> diagnostics)
        {
            var slices = new Dictionary<MethodBase, HarmonyIlEffectMethodSlice>();
            foreach (var method in relevantMethods)
            {
                var node = nodes[method];
                var graph = HarmonyIlControlFlowGraph.Build(node.Body);
                foreach (var diagnostic in graph.Diagnostics)
                    diagnostics.Add(new(
                        HarmonyIlEffectDiagnosticSeverity.Warning,
                        method,
                        $"IL {diagnostic.InstructionIndex}: {diagnostic.Message}"));

                var effectCalls = node.Calls
                    .Where(call =>
                        call.SinkId != null ||
                        relevantMethods.Contains(call.CalledMethod))
                    .Select(call => new HarmonyIlEffectCallSite(
                        call.InstructionIndex,
                        call.CalledMethod,
                        call.SinkId,
                        relevantMethods.Contains(call.CalledMethod)))
                    .ToArray();
                var retainedBlocks = FindPredecessorClosure(graph, effectCalls);
                slices[method] = new(
                    method,
                    node.Body,
                    graph,
                    effectCalls,
                    retainedBlocks);
            }

            return slices;
        }

        private static IReadOnlySet<int> FindPredecessorClosure(
            HarmonyIlControlFlowGraph graph,
            IReadOnlyList<HarmonyIlEffectCallSite> effectCalls)
        {
            var retained = new HashSet<int>();
            var pending = new Queue<int>();
            foreach (var call in effectCalls)
            {
                var blockIndex = graph.InstructionBlockIndexes[call.InstructionIndex];
                if (retained.Add(blockIndex))
                    pending.Enqueue(blockIndex);
            }

            while (pending.TryDequeue(out var blockIndex))
                foreach (var edge in graph.Blocks[blockIndex].IncomingEdges)
                    if (retained.Add(edge.SourceBlockIndex))
                        pending.Enqueue(edge.SourceBlockIndex);

            return retained;
        }

        private sealed record MethodNode(
            MethodBase Method,
            HarmonyIlMethodBody Body,
            IReadOnlyList<DiscoveredCall> Calls);

        private sealed record DiscoveredCall(
            int InstructionIndex,
            MethodInfo CalledMethod,
            string? SinkId);
    }
}
