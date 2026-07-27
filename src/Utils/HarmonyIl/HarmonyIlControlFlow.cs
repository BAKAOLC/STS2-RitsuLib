using System.Reflection.Emit;
using HarmonyLib;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     Kind of a directed edge in a Harmony IL control-flow graph.
    ///     Harmony IL 控制流图中的有向边类型。
    /// </summary>
    public enum HarmonyIlFlowEdgeKind
    {
        /// <summary>
        ///     Sequential flow into the next basic block.
        ///     顺序进入下一个基本块。
        /// </summary>
        FallThrough,

        /// <summary>
        ///     Conditional or unconditional branch flow.
        ///     条件或无条件分支流。
        /// </summary>
        Branch,

        /// <summary>
        ///     One target of a switch instruction.
        ///     switch 指令的一个目标分支。
        /// </summary>
        Switch,

        /// <summary>
        ///     Flow leaving a protected exception region.
        ///     离开受保护异常区域的控制流。
        /// </summary>
        Leave,
    }

    /// <summary>
    ///     A directed edge between two basic blocks.
    ///     两个基本块之间的有向边。
    /// </summary>
    public sealed record HarmonyIlFlowEdge(
        int SourceBlockIndex,
        int TargetBlockIndex,
        HarmonyIlFlowEdgeKind Kind);

    /// <summary>
    ///     One diagnostic produced while constructing a control-flow graph.
    ///     构建控制流图时产生的一条诊断。
    /// </summary>
    public sealed record HarmonyIlControlFlowDiagnostic(
        int InstructionIndex,
        string Message);

    /// <summary>
    ///     A contiguous basic block in a <see cref="HarmonyIlControlFlowGraph" />.
    ///     <see cref="HarmonyIlControlFlowGraph" /> 中连续的基本块。
    /// </summary>
    public sealed class HarmonyIlBasicBlock
    {
        private readonly List<HarmonyIlFlowEdge> _incomingEdges = [];
        private readonly List<HarmonyIlFlowEdge> _outgoingEdges = [];

        internal HarmonyIlBasicBlock(
            int index,
            int startInstructionIndex,
            int endInstructionIndexExclusive)
        {
            Index = index;
            StartInstructionIndex = startInstructionIndex;
            EndInstructionIndexExclusive = endInstructionIndexExclusive;
        }

        /// <summary>
        ///     Stable zero-based index of this block in its graph.
        ///     此基本块在所属图中的稳定零基索引。
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     Index of the first instruction in this block.
        ///     此基本块第一条指令的索引。
        /// </summary>
        public int StartInstructionIndex { get; }

        /// <summary>
        ///     Exclusive index immediately after the last instruction in this block.
        ///     此基本块最后一条指令之后的排他索引。
        /// </summary>
        public int EndInstructionIndexExclusive { get; }

        /// <summary>
        ///     Number of instructions in this block.
        ///     此基本块中的指令数量。
        /// </summary>
        public int InstructionCount => EndInstructionIndexExclusive - StartInstructionIndex;

        /// <summary>
        ///     Incoming control-flow edges.
        ///     传入此基本块的控制流边。
        /// </summary>
        public IReadOnlyList<HarmonyIlFlowEdge> IncomingEdges => _incomingEdges;

        /// <summary>
        ///     Outgoing control-flow edges.
        ///     从此基本块传出的控制流边。
        /// </summary>
        public IReadOnlyList<HarmonyIlFlowEdge> OutgoingEdges => _outgoingEdges;

        internal void AddIncoming(HarmonyIlFlowEdge edge)
        {
            _incomingEdges.Add(edge);
        }

        internal void AddOutgoing(HarmonyIlFlowEdge edge)
        {
            _outgoingEdges.Add(edge);
        }
    }

    /// <summary>
    ///     Basic-block control-flow graph built from a <see cref="HarmonyIlMethodBody" />.
    ///     从 <see cref="HarmonyIlMethodBody" /> 构建的基本块控制流图。
    /// </summary>
    public sealed class HarmonyIlControlFlowGraph
    {
        private HarmonyIlControlFlowGraph(
            HarmonyIlMethodBody methodBody,
            IReadOnlyList<HarmonyIlBasicBlock> blocks,
            IReadOnlyList<HarmonyIlFlowEdge> edges,
            IReadOnlyList<HarmonyIlControlFlowDiagnostic> diagnostics,
            IReadOnlyList<int> instructionBlockIndexes)
        {
            MethodBody = methodBody;
            Blocks = blocks;
            Edges = edges;
            Diagnostics = diagnostics;
            InstructionBlockIndexes = instructionBlockIndexes;
        }

        /// <summary>
        ///     IL body represented by this graph.
        ///     此图所表示的 IL 方法体。
        /// </summary>
        public HarmonyIlMethodBody MethodBody { get; }

        /// <summary>
        ///     Basic blocks in instruction order.
        ///     按指令顺序排列的基本块。
        /// </summary>
        public IReadOnlyList<HarmonyIlBasicBlock> Blocks { get; }

        /// <summary>
        ///     Directed control-flow edges.
        ///     有向控制流边。
        /// </summary>
        public IReadOnlyList<HarmonyIlFlowEdge> Edges { get; }

        /// <summary>
        ///     Non-fatal construction diagnostics.
        ///     构建过程中产生的非致命诊断。
        /// </summary>
        public IReadOnlyList<HarmonyIlControlFlowDiagnostic> Diagnostics { get; }

        /// <summary>
        ///     Basic-block index for every instruction index.
        ///     每个指令索引对应的基本块索引。
        /// </summary>
        public IReadOnlyList<int> InstructionBlockIndexes { get; }

        /// <summary>
        ///     True when all branch targets were resolved.
        ///     所有分支目标均已解析时为 true。
        /// </summary>
        public bool IsComplete => Diagnostics.Count == 0;

        /// <summary>
        ///     Builds a basic-block control-flow graph from original Harmony instructions.
        ///     从原始 Harmony 指令构建基本块控制流图。
        /// </summary>
        public static HarmonyIlControlFlowGraph Build(HarmonyIlMethodBody methodBody)
        {
            ArgumentNullException.ThrowIfNull(methodBody);

            var code = methodBody.Instructions;
            if (code.Count == 0)
                return new(methodBody, [], [], [], []);

            var diagnostics = new List<HarmonyIlControlFlowDiagnostic>();
            var labelTargets = BuildLabelTargets(code);
            var leaders = new SortedSet<int> { 0 };

            for (var i = 0; i < code.Count; i++)
            {
                AddExceptionRegionLeaders(code, i, leaders);
                AddBranchTargetLeaders(code[i], i, labelTargets, leaders, diagnostics);

                if (EndsBasicBlock(code[i]) && i + 1 < code.Count)
                    leaders.Add(i + 1);
            }

            var leaderArray = leaders.Where(index => index >= 0 && index < code.Count).ToArray();
            var blocks = new List<HarmonyIlBasicBlock>(leaderArray.Length);
            var instructionBlockIndexes = new int[code.Count];
            for (var blockIndex = 0; blockIndex < leaderArray.Length; blockIndex++)
            {
                var start = leaderArray[blockIndex];
                var end = blockIndex + 1 < leaderArray.Length ? leaderArray[blockIndex + 1] : code.Count;
                var block = new HarmonyIlBasicBlock(blockIndex, start, end);
                blocks.Add(block);
                for (var instructionIndex = start; instructionIndex < end; instructionIndex++)
                    instructionBlockIndexes[instructionIndex] = blockIndex;
            }

            var edges = new List<HarmonyIlFlowEdge>();
            var seenEdges = new HashSet<HarmonyIlFlowEdge>();
            foreach (var block in blocks)
            {
                var terminatorIndex = block.EndInstructionIndexExclusive - 1;
                var terminator = code[terminatorIndex];
                AddTerminatorEdges(
                    terminator,
                    terminatorIndex,
                    block,
                    blocks,
                    labelTargets,
                    instructionBlockIndexes,
                    edges,
                    seenEdges,
                    diagnostics);
            }

            return new(methodBody, blocks, edges, diagnostics, instructionBlockIndexes);
        }

        private static Dictionary<Label, int> BuildLabelTargets(IReadOnlyList<CodeInstruction> code)
        {
            var targets = new Dictionary<Label, int>();
            for (var i = 0; i < code.Count; i++)
                foreach (var label in code[i].labels)
                    targets[label] = i;
            return targets;
        }

        private static void AddExceptionRegionLeaders(
            IReadOnlyList<CodeInstruction> code,
            int instructionIndex,
            ISet<int> leaders)
        {
            if (code[instructionIndex].blocks.Count == 0)
                return;

            leaders.Add(instructionIndex);
            if (instructionIndex + 1 < code.Count &&
                code[instructionIndex].blocks.Any(static block =>
                    block.blockType == ExceptionBlockType.EndExceptionBlock))
                leaders.Add(instructionIndex + 1);
        }

        private static void AddBranchTargetLeaders(
            CodeInstruction instruction,
            int instructionIndex,
            IReadOnlyDictionary<Label, int> labelTargets,
            ISet<int> leaders,
            ICollection<HarmonyIlControlFlowDiagnostic> diagnostics)
        {
            if (instruction.opcode.OperandType == OperandType.InlineSwitch)
            {
                if (instruction.operand is not Label[] switchTargets)
                {
                    diagnostics.Add(new(
                        instructionIndex,
                        $"Switch operand has unsupported type {instruction.operand?.GetType().FullName ?? "<null>"}."));
                    return;
                }

                foreach (var target in switchTargets)
                    AddLabelTarget(target, instructionIndex, labelTargets, leaders, diagnostics);
                return;
            }

            if (instruction.opcode.OperandType is not (
                OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget))
                return;

            if (instruction.operand is Label targetLabel)
            {
                AddLabelTarget(targetLabel, instructionIndex, labelTargets, leaders, diagnostics);
                return;
            }

            diagnostics.Add(new(
                instructionIndex,
                $"Branch operand has unsupported type {instruction.operand?.GetType().FullName ?? "<null>"}."));
        }

        private static void AddLabelTarget(
            Label label,
            int instructionIndex,
            IReadOnlyDictionary<Label, int> labelTargets,
            ISet<int> leaders,
            ICollection<HarmonyIlControlFlowDiagnostic> diagnostics)
        {
            if (labelTargets.TryGetValue(label, out var targetIndex))
            {
                leaders.Add(targetIndex);
                return;
            }

            diagnostics.Add(new(instructionIndex, "Branch target label was not found in the instruction list."));
        }

        private static bool EndsBasicBlock(CodeInstruction instruction)
        {
            return instruction.opcode.FlowControl is
                FlowControl.Branch or
                FlowControl.Cond_Branch or
                FlowControl.Return or
                FlowControl.Throw;
        }

        private static void AddTerminatorEdges(
            CodeInstruction terminator,
            int terminatorIndex,
            HarmonyIlBasicBlock source,
            IReadOnlyList<HarmonyIlBasicBlock> blocks,
            IReadOnlyDictionary<Label, int> labelTargets,
            IReadOnlyList<int> instructionBlockIndexes,
            ICollection<HarmonyIlFlowEdge> edges,
            ISet<HarmonyIlFlowEdge> seenEdges,
            ICollection<HarmonyIlControlFlowDiagnostic> diagnostics)
        {
            var flowControl = terminator.opcode.FlowControl;
            switch (flowControl)
            {
                case FlowControl.Cond_Branch:
                {
                    var kind = terminator.opcode == OpCodes.Switch
                        ? HarmonyIlFlowEdgeKind.Switch
                        : HarmonyIlFlowEdgeKind.Branch;
                    AddBranchEdges(
                        terminator,
                        terminatorIndex,
                        source,
                        kind,
                        blocks,
                        labelTargets,
                        instructionBlockIndexes,
                        edges,
                        seenEdges,
                        diagnostics);
                    AddFallThroughEdge(source, blocks, edges, seenEdges);
                    return;
                }
                case FlowControl.Branch:
                {
                    var kind = terminator.opcode == OpCodes.Leave ||
                               terminator.opcode == OpCodes.Leave_S
                        ? HarmonyIlFlowEdgeKind.Leave
                        : HarmonyIlFlowEdgeKind.Branch;
                    AddBranchEdges(
                        terminator,
                        terminatorIndex,
                        source,
                        kind,
                        blocks,
                        labelTargets,
                        instructionBlockIndexes,
                        edges,
                        seenEdges,
                        diagnostics);
                    return;
                }
                case FlowControl.Return or FlowControl.Throw:
                    return;
                default:
                    AddFallThroughEdge(source, blocks, edges, seenEdges);
                    break;
            }
        }

        private static void AddBranchEdges(
            CodeInstruction instruction,
            int instructionIndex,
            HarmonyIlBasicBlock source,
            HarmonyIlFlowEdgeKind kind,
            IReadOnlyList<HarmonyIlBasicBlock> blocks,
            IReadOnlyDictionary<Label, int> labelTargets,
            IReadOnlyList<int> instructionBlockIndexes,
            ICollection<HarmonyIlFlowEdge> edges,
            ISet<HarmonyIlFlowEdge> seenEdges,
            ICollection<HarmonyIlControlFlowDiagnostic> diagnostics)
        {
            IEnumerable<Label> labels = instruction.operand switch
            {
                Label label => [label],
                Label[] switchLabels => switchLabels,
                _ => [],
            };

            var found = false;
            foreach (var label in labels)
            {
                found = true;
                if (!labelTargets.TryGetValue(label, out var targetInstructionIndex))
                {
                    diagnostics.Add(new(
                        instructionIndex,
                        "Branch target label was not found in the instruction list."));
                    continue;
                }

                var targetBlockIndex = instructionBlockIndexes[targetInstructionIndex];
                AddEdge(source, blocks[targetBlockIndex], kind, edges, seenEdges);
            }

            if (!found && instruction.opcode != OpCodes.Jmp)
                diagnostics.Add(new(
                    instructionIndex,
                    $"No supported target was found for branch opcode {instruction.opcode}."));
        }

        private static void AddFallThroughEdge(
            HarmonyIlBasicBlock source,
            IReadOnlyList<HarmonyIlBasicBlock> blocks,
            ICollection<HarmonyIlFlowEdge> edges,
            ISet<HarmonyIlFlowEdge> seenEdges)
        {
            if (source.Index + 1 >= blocks.Count)
                return;

            AddEdge(
                source,
                blocks[source.Index + 1],
                HarmonyIlFlowEdgeKind.FallThrough,
                edges,
                seenEdges);
        }

        private static void AddEdge(
            HarmonyIlBasicBlock source,
            HarmonyIlBasicBlock target,
            HarmonyIlFlowEdgeKind kind,
            ICollection<HarmonyIlFlowEdge> edges,
            ISet<HarmonyIlFlowEdge> seenEdges)
        {
            var edge = new HarmonyIlFlowEdge(source.Index, target.Index, kind);
            if (!seenEdges.Add(edge))
                return;

            edges.Add(edge);
            source.AddOutgoing(edge);
            target.AddIncoming(edge);
        }
    }
}
