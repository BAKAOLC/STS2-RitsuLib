using System.Reflection;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     Named terminal method predicate used by <see cref="HarmonyIlEffectAnalyzer" />.
    ///     <see cref="HarmonyIlEffectAnalyzer" /> 使用的具名终点方法谓词。
    /// </summary>
    public sealed record HarmonyIlEffectSink(
        string Id,
        Func<MethodInfo, bool> IsMatch)
    {
        /// <summary>
        ///     Creates a sink that matches one exact method.
        ///     创建匹配一个确切方法的效果终点。
        /// </summary>
        public static HarmonyIlEffectSink ForMethod(string id, MethodInfo method)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(method);
            return new(id, candidate => candidate == method);
        }
    }

    /// <summary>
    ///     Limits and traversal policy for an IL effect analysis.
    ///     IL 效果分析的限制与遍历策略。
    /// </summary>
    public sealed record HarmonyIlEffectAnalysisOptions
    {
        /// <summary>
        ///     Maximum number of traversed calls from a root.
        ///     从根方法开始允许下钻的最大调用层数。
        /// </summary>
        public int MaxDepth { get; init; } = 16;

        /// <summary>
        ///     Maximum number of distinct logical methods inspected.
        ///     允许检查的最大不同逻辑方法数量。
        /// </summary>
        public int MaxMethods { get; init; } = 4096;

        /// <summary>
        ///     Resolve async logical methods to compiler-generated <c>MoveNext</c> bodies.
        ///     将 async 逻辑方法解析到编译器生成的 <c>MoveNext</c> 方法体。
        /// </summary>
        public bool ResolveAsync { get; init; } = true;

        /// <summary>
        ///     Selects non-sink calls whose bodies may be inspected. Null keeps analysis direct-call-only.
        ///     选择允许继续检查方法体的非终点调用；为 null 时仅分析直接调用。
        /// </summary>
        public Func<MethodInfo, bool>? ShouldTraverse { get; init; }
    }

    /// <summary>
    ///     Severity of an IL effect-analysis diagnostic.
    ///     IL 效果分析诊断的严重程度。
    /// </summary>
    public enum HarmonyIlEffectDiagnosticSeverity
    {
        /// <summary>
        ///     Informational analysis detail.
        ///     信息性分析详情。
        /// </summary>
        Information,

        /// <summary>
        ///     Recoverable condition that may reduce completeness.
        ///     可能降低完整性的可恢复状况。
        /// </summary>
        Warning,

        /// <summary>
        ///     Condition that prevents complete analysis.
        ///     阻止完整分析的错误状况。
        /// </summary>
        Error,
    }

    /// <summary>
    ///     One diagnostic emitted by <see cref="HarmonyIlEffectAnalyzer" />.
    ///     <see cref="HarmonyIlEffectAnalyzer" /> 产生的一条诊断。
    /// </summary>
    public sealed record HarmonyIlEffectDiagnostic(
        HarmonyIlEffectDiagnosticSeverity Severity,
        MethodBase? Method,
        string Message);

    /// <summary>
    ///     A call site that directly reaches a sink or another effect-relevant method.
    ///     直接到达效果终点或另一个效果相关方法的调用点。
    /// </summary>
    public sealed record HarmonyIlEffectCallSite(
        int InstructionIndex,
        MethodInfo CalledMethod,
        string? SinkId,
        bool ReachesRelevantMethod)
    {
        /// <summary>
        ///     True when this call directly matches a configured sink.
        ///     此调用直接匹配已配置效果终点时为 true。
        /// </summary>
        public bool IsDirectSink => SinkId != null;
    }

    /// <summary>
    ///     Conservative control-flow slice for one effect-relevant logical method.
    ///     一个效果相关逻辑方法的保守控制流切片。
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
        ///     Logical method represented by this slice.
        ///     此切片所表示的逻辑方法。
        /// </summary>
        public MethodBase Method { get; }

        /// <summary>
        ///     Resolved original IL body.
        ///     解析后的原始 IL 方法体。
        /// </summary>
        public HarmonyIlMethodBody Body { get; }

        /// <summary>
        ///     Full basic-block graph of <see cref="Body" />.
        ///     <see cref="Body" /> 的完整基本块图。
        /// </summary>
        public HarmonyIlControlFlowGraph ControlFlow { get; }

        /// <summary>
        ///     Effect-relevant calls in instruction order.
        ///     按指令顺序排列的效果相关调用。
        /// </summary>
        public IReadOnlyList<HarmonyIlEffectCallSite> EffectCallSites { get; }

        /// <summary>
        ///     Blocks that can reach an effect call through normal control flow.
        ///     可通过普通控制流到达效果调用的基本块。
        /// </summary>
        public IReadOnlySet<int> RetainedBlockIndexes { get; }

        /// <summary>
        ///     Returns true when an instruction belongs to a retained block.
        ///     指令属于保留基本块时返回 true。
        /// </summary>
        public bool RetainsInstruction(int instructionIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(instructionIndex);
            if (instructionIndex >= ControlFlow.InstructionBlockIndexes.Count)
                throw new ArgumentOutOfRangeException(nameof(instructionIndex));

            return RetainedBlockIndexes.Contains(
                ControlFlow.InstructionBlockIndexes[instructionIndex]);
        }
    }

    /// <summary>
    ///     Result of an interprocedural effect reachability and control-flow slicing pass.
    ///     跨方法效果可达性与控制流切片分析结果。
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
        ///     Logical root methods supplied to the analysis.
        ///     提供给分析器的逻辑根方法。
        /// </summary>
        public IReadOnlyList<MethodBase> Roots { get; }

        /// <summary>
        ///     Effect-relevant method slices keyed by logical method.
        ///     以逻辑方法为键的效果相关方法切片。
        /// </summary>
        public IReadOnlyDictionary<MethodBase, HarmonyIlEffectMethodSlice> Slices { get; }

        /// <summary>
        ///     Analysis diagnostics.
        ///     分析诊断。
        /// </summary>
        public IReadOnlyList<HarmonyIlEffectDiagnostic> Diagnostics { get; }

        /// <summary>
        ///     True when analysis completed without error diagnostics or incomplete control-flow graphs.
        ///     分析未产生错误诊断且所有控制流图完整时为 true。
        /// </summary>
        public bool IsComplete =>
            Diagnostics.All(static diagnostic =>
                diagnostic.Severity != HarmonyIlEffectDiagnosticSeverity.Error) &&
            Slices.Values.All(static slice => slice.ControlFlow.IsComplete);
    }

    /// <summary>
    ///     Discovers calls that can reach configured effect sinks and creates conservative per-method control slices.
    ///     发现可到达已配置效果终点的调用，并为每个相关方法创建保守控制切片。
    /// </summary>
    public static class HarmonyIlEffectAnalyzer
    {
        /// <summary>
        ///     Analyzes logical root methods against named effect sinks.
        ///     按具名效果终点分析逻辑根方法。
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
