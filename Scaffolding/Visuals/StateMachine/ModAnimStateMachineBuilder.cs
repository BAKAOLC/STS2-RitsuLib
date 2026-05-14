using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     Fluent builder for <see cref="ModAnimStateMachine" />. Declare named states, per-state loop / next-state
    ///     / bounds metadata, per-state branches, and any-state transitions; finalise by calling one of the
    ///     <c>Build</c> overloads with an <see cref="IAnimationBackend" /> or a visuals root.
    ///     <see cref="ModAnimStateMachine" /> 的流式构建器。声明命名状态、每状态循环 / 下一状态
    ///     / 边界元数据、每状态分支和任意状态转换；最后通过调用某个带
    ///     <c>Build</c> 重载并传入 <see cref="IAnimationBackend" /> 或视觉根节点来完成。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The builder does not validate ids against backend animation availability: if a state id is unresolvable
    ///         by the chosen backend, <see cref="ModAnimStateMachine" /> will skip playback for that state and log a
    ///         warning on entry.
    ///     </para>
    ///     <para>
    ///         构建器不会根据后端动画可用性验证 id：如果某个状态 id 无法由
    ///         所选后端解析，<see cref="ModAnimStateMachine" /> 会跳过该状态的播放，并在进入时记录
    ///         警告。
    ///     </para>
    /// </remarks>
    public sealed class ModAnimStateMachineBuilder
    {
        private readonly List<AnyBranchDraft> _anyBranches = [];
        private readonly Dictionary<string, StateDraft> _states = new(StringComparer.Ordinal);
        private string? _initialStateId;

        private ModAnimStateMachineBuilder()
        {
        }

        /// <summary>
        ///     Creates a fresh builder.
        ///     创建新的构建器。
        /// </summary>
        public static ModAnimStateMachineBuilder Create()
        {
            return new();
        }

        /// <summary>
        ///     Declares a state with backend animation id <paramref name="id" /> and loop hint
        ///     <paramref name="loop" />. Returns a scope object so the caller can chain
        ///     <see cref="StateScope.WithNext" />, <see cref="StateScope.WithBounds" />, and
        ///     <see cref="StateScope.AsInitial" />.
        ///     声明一个状态，其后端动画 id 为 <paramref name="id" />，循环提示为
        ///     <paramref name="loop" />。返回一个作用域对象，使调用方可以链式调用
        ///     <see cref="StateScope.WithNext" />、<see cref="StateScope.WithBounds" /> 和
        ///     <see cref="StateScope.AsInitial" />。
        /// </summary>
        public StateScope AddState(string id, bool loop = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            if (_states.ContainsKey(id))
                throw new InvalidOperationException($"State '{id}' already declared.");

            var draft = new StateDraft(id, loop);
            _states[id] = draft;
            _initialStateId ??= id;
            return new(this, draft);
        }

        /// <summary>
        ///     Adds a branch from state <paramref name="fromId" /> for trigger <paramref name="trigger" /> to state
        ///     <paramref name="toId" />. Optional <paramref name="condition" /> guards activation.
        ///     添加一个分支：从状态 <paramref name="fromId" /> 在触发器 <paramref name="trigger" /> 下转到状态
        ///     <paramref name="toId" />。可选 <paramref name="condition" /> 用于保护激活。
        /// </summary>
        public ModAnimStateMachineBuilder AddBranch(string fromId, string trigger, string toId,
            Func<bool>? condition = null)
        {
            if (!_states.TryGetValue(fromId, out var draft))
                throw new InvalidOperationException($"Source state '{fromId}' not declared.");

            draft.Branches.Add(new(trigger, toId, condition));
            return this;
        }

        /// <summary>
        ///     Adds an any-state branch for trigger <paramref name="trigger" /> to <paramref name="toId" />
        ///     (guarded by optional <paramref name="condition" />).
        ///     为触发器 <paramref name="trigger" /> 添加一个转到 <paramref name="toId" /> 的任意状态分支
        ///     （由可选 <paramref name="condition" /> 保护）。
        /// </summary>
        public ModAnimStateMachineBuilder AddAnyState(string trigger, string toId, Func<bool>? condition = null)
        {
            _anyBranches.Add(new(trigger, toId, condition));
            return this;
        }

        /// <summary>
        ///     Materialises the graph against <paramref name="backend" /> and returns a started
        ///     <see cref="ModAnimStateMachine" />.
        ///     用 <paramref name="backend" /> 实例化状态图，并返回已启动的
        ///     <see cref="ModAnimStateMachine" />。
        /// </summary>
        public ModAnimStateMachine Build(IAnimationBackend backend)
        {
            ArgumentNullException.ThrowIfNull(backend);
            var machine = BuildCore(backend, out var initial);
            machine.Start(initial);
            return machine;
        }

        /// <summary>
        ///     Convenience overload: wraps <paramref name="controller" /> in a <see cref="SpineAnimationBackend" />
        ///     and builds the machine.
        ///     便捷重载：将 <paramref name="controller" /> 包装进 <see cref="SpineAnimationBackend" />
        ///     并构建状态机。
        /// </summary>
        public ModAnimStateMachine BuildSpine(MegaSprite controller)
        {
            return Build(new SpineAnimationBackend(controller));
        }

        /// <summary>
        ///     Convenience overload: composes cue / Spine / Godot animation player / animated-sprite backends
        ///     rooted at <paramref name="visualsRoot" /> and builds the machine.
        ///     便捷重载：组合以 <paramref name="visualsRoot" /> 为根的 cue / Spine / Godot animation player / animated-sprite 后端，
        ///     并构建状态机。
        /// </summary>
        /// <param name="visualsRoot">
        ///     Visuals root (typically an <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCreatureVisuals" />).
        ///     视觉根节点（通常是 <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCreatureVisuals" />）。
        /// </param>
        /// <param name="character">
        ///     Optional character model; used when <paramref name="cueSet" /> is <see langword="null" />
        ///     to pull cue data from <c>IModCharacterAssetOverrides</c>.
        ///     可选角色模型；当 <paramref name="cueSet" /> 为 <see langword="null" /> 时，
        ///     用于从 <c>IModCharacterAssetOverrides</c> 拉取 cue 数据。
        /// </param>
        /// <param name="cueSet">
        ///     Optional explicit cue set; takes priority over the character-derived one.
        ///     可选显式 cue set；优先于从角色派生的 cue set。
        /// </param>
        public ModAnimStateMachine BuildForVisualsRoot(Node visualsRoot, CharacterModel? character = null,
            VisualCueSet? cueSet = null)
        {
            var backend = CompositeBackendFactory.Build(visualsRoot, character, cueSet);
            return Build(backend);
        }

        private ModAnimStateMachine BuildCore(IAnimationBackend backend, out ModAnimState initial)
        {
            if (_initialStateId == null)
                throw new InvalidOperationException("No states declared.");

            var materialised = new Dictionary<string, ModAnimState>(StringComparer.Ordinal);

            foreach (var (id, draft) in _states)
                materialised[id] = new(draft.Id, draft.Loop) { BoundsContainer = draft.BoundsContainer };

            foreach (var (id, draft) in _states)
            {
                var state = materialised[id];
                if (draft.NextStateId != null && materialised.TryGetValue(draft.NextStateId, out var next))
                    state.NextState = next;

                foreach (var branch in draft.Branches)
                {
                    if (!materialised.TryGetValue(branch.ToId, out var target))
                        continue;

                    state.AddBranch(branch.Trigger, target, branch.Condition);
                }
            }

            initial = materialised[_initialStateId];
            var machine = new ModAnimStateMachine(backend);
            foreach (var branch in _anyBranches)
            {
                if (!materialised.TryGetValue(branch.ToId, out var target))
                    continue;

                machine.AddAnyState(branch.Trigger, target, branch.Condition);
            }

            return machine;
        }

        internal sealed class StateDraft(string id, bool loop)
        {
            public string Id { get; } = id;
            public bool Loop { get; } = loop;
            public string? NextStateId { get; set; }
            public string? BoundsContainer { get; set; }
            public List<BranchDraft> Branches { get; } = [];
        }

        internal readonly record struct BranchDraft(string Trigger, string ToId, Func<bool>? Condition);

        private readonly record struct AnyBranchDraft(string Trigger, string ToId, Func<bool>? Condition);

        /// <summary>
        ///     Fluent scope returned by <see cref="ModAnimStateMachineBuilder.AddState" /> for per-state metadata.
        ///     由 <see cref="ModAnimStateMachineBuilder.AddState" /> 返回的流式作用域，用于逐状态元数据。
        /// </summary>
        public sealed class StateScope
        {
            private readonly StateDraft _draft;
            private readonly ModAnimStateMachineBuilder _owner;

            internal StateScope(ModAnimStateMachineBuilder owner, StateDraft draft)
            {
                _owner = owner;
                _draft = draft;
            }

            /// <summary>
            ///     Sets <see cref="ModAnimState.NextState" /> for the current state (by target id).
            ///     为当前状态设置 <see cref="ModAnimState.NextState" />（按目标 id）。
            /// </summary>
            public StateScope WithNext(string nextStateId)
            {
                _draft.NextStateId = nextStateId;
                return this;
            }

            /// <summary>
            ///     Sets <see cref="ModAnimState.BoundsContainer" /> tag emitted via
            ///     <see cref="ModAnimStateMachine.BoundsUpdated" /> on enter.
            ///     设置进入状态时通过 <see cref="ModAnimStateMachine.BoundsUpdated" /> 发出的
            ///     <see cref="ModAnimState.BoundsContainer" /> 标签。
            /// </summary>
            public StateScope WithBounds(string boundsContainer)
            {
                _draft.BoundsContainer = boundsContainer;
                return this;
            }

            /// <summary>
            ///     Marks the current state as the initial state (overrides the auto-first-state behaviour).
            ///     将当前状态标记为初始状态（覆盖自动使用第一个状态的行为）。
            /// </summary>
            public StateScope AsInitial()
            {
                _owner._initialStateId = _draft.Id;
                return this;
            }

            /// <summary>
            ///     Returns the owning builder so chaining can continue.
            ///     返回所属构建器，以便继续链式调用。
            /// </summary>
            public ModAnimStateMachineBuilder Done()
            {
                return _owner;
            }
        }
    }
}
