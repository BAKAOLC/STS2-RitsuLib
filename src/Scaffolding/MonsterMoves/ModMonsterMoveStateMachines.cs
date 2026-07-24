using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace STS2RitsuLib.Scaffolding.MonsterMoves
{
    /// <summary>
    ///     Common <see cref="MonsterMoveStateMachine" /> wiring patterns for mod monsters, so
    ///     <see cref="MegaCrit.Sts2.Core.Models.MonsterModel.GenerateMoveStateMachine" /> stays short.
    ///     mod 怪物常用的 <see cref="MonsterMoveStateMachine" /> 接线模式，使
    ///     <see cref="MegaCrit.Sts2.Core.Models.MonsterModel.GenerateMoveStateMachine" /> 保持简短。
    /// </summary>
    public static class ModMonsterMoveStateMachines
    {
        /// <summary>
        ///     One move that repeats every turn (<c>FollowUpState = self</c>).
        /// </summary>
        public static MonsterMoveStateMachine SingleMoveLoop(MoveState move)
        {
            ArgumentNullException.ThrowIfNull(move);
            move.FollowUpState = move;
            return new(new List<MonsterState> { move }, move);
        }

        /// <summary>
        ///     Rotating cycle: each move leads to the next, last leads back to the first.
        ///     轮转循环：每个行动指向下一个，最后一个指回第一个。
        /// </summary>
        public static MonsterMoveStateMachine Cycle(params MoveState[] moves)
        {
            return Cycle((IReadOnlyList<MoveState>)moves);
        }

        /// <summary>
        ///     Rotating cycle: each move leads to the next, last leads back to the first.
        ///     轮转循环：每个行动指向下一个，最后一个指回第一个。
        /// </summary>
        public static MonsterMoveStateMachine Cycle(IReadOnlyList<MoveState> moves)
        {
            ArgumentNullException.ThrowIfNull(moves);
            var n = moves.Count;
            if (n == 0) throw new ArgumentException("At least one move is required.", nameof(moves));

            for (var i = 0; i < n; i++) moves[i].FollowUpState = moves[(i + 1) % n];

            return new([.. moves], moves[0]);
        }

        /// <summary>
        ///     <paramref name="head" /> once, then <paramref name="tail" /> every subsequent turn
        ///     (matches patterns like Track → Hounds, Hounds → Hounds).
        ///     <paramref name="head" /> 执行一次，之后每回合执行 <paramref name="tail" />
        ///     （匹配 Track → Hounds、Hounds → Hounds 等模式）。
        /// </summary>
        public static MonsterMoveStateMachine HeadThenRepeatTail(MoveState head, MoveState tail)
        {
            ArgumentNullException.ThrowIfNull(head);
            ArgumentNullException.ThrowIfNull(tail);
            head.FollowUpState = tail;
            tail.FollowUpState = tail;
            return new(new List<MonsterState> { head, tail }, head);
        }

        /// <summary>
        ///     <see cref="RandomBranchState" /> as entry: call <paramref name="configureBranches" /> to
        ///     <c>AddBranch</c> moves, then pass every <see cref="MoveState" /> and other
        ///     <see cref="MonsterState" /> nodes that must register (same rules as vanilla).
        ///     将 <see cref="RandomBranchState" /> 作为条目：调用 <paramref name="configureBranches" /> 来
        ///     <c>AddBranch</c> 各个招式，然后传入所有必须注册的 <see cref="MoveState" /> 以及其他
        ///     <see cref="MonsterState" /> 节点（规则与原版相同）。
        /// </summary>
        public static MonsterMoveStateMachine RandomEntry(
            string branchId,
            Action<RandomBranchState> configureBranches,
            IReadOnlyList<MonsterState> allStatesIncludingMoves)
        {
            ArgumentNullException.ThrowIfNull(configureBranches);
            ArgumentNullException.ThrowIfNull(allStatesIncludingMoves);
            var branch = new RandomBranchState(branchId);
            configureBranches(branch);
            var list = new List<MonsterState>(1 + allStatesIncludingMoves.Count) { branch };
            list.AddRange(allStatesIncludingMoves);
            return new(list, branch);
        }

        /// <summary>
        ///     <see cref="ConditionalBranchState" /> as entry (e.g. Toadpole-style first branch).
        ///     <see cref="ConditionalBranchState" /> as 条目 (e.g. Toadpole-style first 分支)。
        /// </summary>
        public static MonsterMoveStateMachine ConditionalEntry(
            string branchId,
            Action<ConditionalBranchState> configureBranches,
            IReadOnlyList<MonsterState> allStatesIncludingMoves)
        {
            ArgumentNullException.ThrowIfNull(configureBranches);
            ArgumentNullException.ThrowIfNull(allStatesIncludingMoves);
            var branch = new ConditionalBranchState(branchId);
            configureBranches(branch);
            var list = new List<MonsterState>(1 + allStatesIncludingMoves.Count) { branch };
            list.AddRange(allStatesIncludingMoves);
            return new(list, branch);
        }
    }
}
