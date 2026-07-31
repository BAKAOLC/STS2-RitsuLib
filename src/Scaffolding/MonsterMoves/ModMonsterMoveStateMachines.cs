using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace STS2RitsuLib.Scaffolding.MonsterMoves
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides common <see cref="MonsterMoveStateMachine" /> construction patterns for mod monsters, keeping
    ///         <see cref="MegaCrit.Sts2.Core.Models.MonsterModel.GenerateMoveStateMachine" /> implementations concise.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供模组怪物常用的 <see cref="MonsterMoveStateMachine" /> 构造模式，使
    ///         <see cref="MegaCrit.Sts2.Core.Models.MonsterModel.GenerateMoveStateMachine" /> 的实现保持简洁。
    ///     </para>
    /// </summary>
    public static class ModMonsterMoveStateMachines
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a state machine containing one move that repeats every turn.</para>
        ///     <para xml:lang="zh-CN">创建只包含一个行动、且每回合重复该行动的状态机。</para>
        /// </summary>
        public static MonsterMoveStateMachine SingleMoveLoop(MoveState move)
        {
            ArgumentNullException.ThrowIfNull(move);
            move.FollowUpState = move;
            return new([move], move);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a rotating cycle in which each move leads to the next and the last leads back to the first.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建轮转循环：每个行动衔接下一个行动，最后一个行动衔接回第一个。</para>
        /// </summary>
        public static MonsterMoveStateMachine Cycle(params MoveState[] moves)
        {
            return Cycle((IReadOnlyList<MoveState>)moves);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a rotating cycle in which each move leads to the next and the last leads back to the first.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建轮转循环：每个行动衔接下一个行动，最后一个行动衔接回第一个。</para>
        /// </summary>
        public static MonsterMoveStateMachine Cycle(IReadOnlyList<MoveState> moves)
        {
            ArgumentNullException.ThrowIfNull(moves);
            var n = moves.Count;
            if (n == 0)
                throw new ArgumentException("At least one move is required.", nameof(moves));
            if (moves.Any(static move => move == null))
                throw new ArgumentException("Move lists cannot contain null entries.", nameof(moves));

            for (var i = 0; i < n; i++)
                moves[i].FollowUpState = moves[(i + 1) % n];

            return new([.. moves], moves[0]);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a state machine that performs <paramref name="head" /> once, then repeats
        ///         <paramref name="tail" /> every subsequent turn, matching patterns such as Track → Hounds → Hounds.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建先执行一次 <paramref name="head" />、再于之后每回合重复 <paramref name="tail" /> 的状态机，
        ///         对应 Track → Hounds → Hounds 等模式。
        ///     </para>
        /// </summary>
        public static MonsterMoveStateMachine HeadThenRepeatTail(MoveState head, MoveState tail)
        {
            ArgumentNullException.ThrowIfNull(head);
            ArgumentNullException.ThrowIfNull(tail);
            head.FollowUpState = tail;
            tail.FollowUpState = tail;
            return new([head, tail], head);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a state machine with <see cref="RandomBranchState" /> as its entry state. Use
        ///         <paramref name="configureBranches" /> to call <c>AddBranch</c>, and include every
        ///         <see cref="MoveState" /> and other required <see cref="MonsterState" /> in
        ///         <paramref name="allStatesIncludingMoves" />, following the base game's registration rules.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建以 <see cref="RandomBranchState" /> 为入口状态的状态机。使用
        ///         <paramref name="configureBranches" /> 调用 <c>AddBranch</c>，并按照游戏的注册规则，将所有
        ///         <see cref="MoveState" /> 及其他必需的 <see cref="MonsterState" /> 包含在
        ///         <paramref name="allStatesIncludingMoves" /> 中。
        ///     </para>
        /// </summary>
        public static MonsterMoveStateMachine RandomEntry(
            string branchId,
            Action<RandomBranchState> configureBranches,
            IReadOnlyList<MonsterState> allStatesIncludingMoves)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(branchId);
            ArgumentNullException.ThrowIfNull(configureBranches);
            ArgumentNullException.ThrowIfNull(allStatesIncludingMoves);
            ValidateStates(allStatesIncludingMoves, nameof(allStatesIncludingMoves));
            var branch = new RandomBranchState(branchId);
            configureBranches(branch);
            var list = new List<MonsterState>(1 + allStatesIncludingMoves.Count) { branch };
            list.AddRange(allStatesIncludingMoves);
            return new(list, branch);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a state machine with <see cref="ConditionalBranchState" /> as its entry state, for patterns such as
        ///         Toadpole's initial branch.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建以 <see cref="ConditionalBranchState" /> 为入口状态的状态机，适用于 Toadpole 首次分支等模式。
        ///     </para>
        /// </summary>
        public static MonsterMoveStateMachine ConditionalEntry(
            string branchId,
            Action<ConditionalBranchState> configureBranches,
            IReadOnlyList<MonsterState> allStatesIncludingMoves)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(branchId);
            ArgumentNullException.ThrowIfNull(configureBranches);
            ArgumentNullException.ThrowIfNull(allStatesIncludingMoves);
            ValidateStates(allStatesIncludingMoves, nameof(allStatesIncludingMoves));
            var branch = new ConditionalBranchState(branchId);
            configureBranches(branch);
            var list = new List<MonsterState>(1 + allStatesIncludingMoves.Count) { branch };
            list.AddRange(allStatesIncludingMoves);
            return new(list, branch);
        }

        private static void ValidateStates(IReadOnlyList<MonsterState> states, string paramName)
        {
            if (states.Any(static state => state == null))
                throw new ArgumentException("Monster-state lists cannot contain null entries.", paramName);
        }
    }
}
