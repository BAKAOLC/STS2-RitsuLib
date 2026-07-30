using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     <para xml:lang="en">Fluently declares states and transitions, then builds and starts a <see cref="ModAnimStateMachine" />.</para>
    ///     <para xml:lang="zh-CN">以流式方式声明状态和转换，然后构建并启动 <see cref="ModAnimStateMachine" />。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">The builder does not validate IDs against backend availability. Entering an unavailable state logs a warning and leaves the current state unchanged.</para>
    ///     <para xml:lang="zh-CN">构建器不会根据后端可用性验证 ID。进入不可用状态时会记录警告，并保持当前状态不变。</para>
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
        ///     <para xml:lang="en">Creates a new builder.</para>
        ///     <para xml:lang="zh-CN">创建新的构建器。</para>
        /// </summary>
        public static ModAnimStateMachineBuilder Create()
        {
            return new();
        }

        /// <summary>
        ///     <para xml:lang="en">Declares a state with backend animation <paramref name="id" /> and loop request <paramref name="loop" />.</para>
        ///     <para xml:lang="zh-CN">声明使用后端动画 <paramref name="id" /> 和循环请求 <paramref name="loop" /> 的状态。</para>
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
        ///     <para xml:lang="en">Adds a trigger branch from <paramref name="fromId" /> to <paramref name="toId" />, guarded by <paramref name="condition" /> when supplied.</para>
        ///     <para xml:lang="zh-CN">添加从 <paramref name="fromId" /> 到 <paramref name="toId" /> 的触发器分支；提供时由 <paramref name="condition" /> 守卫。</para>
        /// </summary>
        public ModAnimStateMachineBuilder AddBranch(string fromId, string trigger, string toId,
            Func<bool>? condition = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fromId);
            ArgumentException.ThrowIfNullOrWhiteSpace(trigger);
            ArgumentException.ThrowIfNullOrWhiteSpace(toId);
            if (!_states.TryGetValue(fromId, out var draft))
                throw new InvalidOperationException($"Source state '{fromId}' not declared.");

            draft.Branches.Add(new(trigger, toId, condition));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Adds an any-state branch to <paramref name="toId" />, optionally guarded by <paramref name="condition" />.</para>
        ///     <para xml:lang="zh-CN">添加指向 <paramref name="toId" /> 的任意状态分支，可选地由 <paramref name="condition" /> 守卫。</para>
        /// </summary>
        public ModAnimStateMachineBuilder AddAnyState(string trigger, string toId, Func<bool>? condition = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(trigger);
            ArgumentException.ThrowIfNullOrWhiteSpace(toId);
            _anyBranches.Add(new(trigger, toId, condition));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Materializes the graph against <paramref name="backend" /> and starts the resulting state machine.</para>
        ///     <para xml:lang="zh-CN">针对 <paramref name="backend" /> 实例化状态图，并启动所得状态机。</para>
        /// </summary>
        public ModAnimStateMachine Build(IAnimationBackend backend)
        {
            ArgumentNullException.ThrowIfNull(backend);
            var machine = BuildCore(backend, out var initial);
            machine.Start(initial);
            return machine;
        }

        /// <summary>
        ///     <para xml:lang="en">Wraps <paramref name="controller" /> in a <see cref="SpineAnimationBackend" /> and builds the state machine.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="controller" /> 包装为 <see cref="SpineAnimationBackend" /> 并构建状态机。</para>
        /// </summary>
        public ModAnimStateMachine BuildSpine(MegaSprite controller)
        {
            return Build(new SpineAnimationBackend(controller));
        }

        /// <summary>
        ///     <para xml:lang="en">Discovers cue, Spine, Godot AnimationPlayer, and AnimatedSprite2D backends under <paramref name="visualsRoot" />, then builds the state machine.</para>
        ///     <para xml:lang="zh-CN">在 <paramref name="visualsRoot" /> 下发现视觉提示、Spine、Godot AnimationPlayer 和 AnimatedSprite2D 后端，再构建状态机。</para>
        /// </summary>
        /// <param name="visualsRoot">
        ///     <para xml:lang="en">The visuals root, typically an <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCreatureVisuals" />.</para>
        ///     <para xml:lang="zh-CN">视觉根节点，通常为 <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCreatureVisuals" />。</para>
        /// </param>
        /// <param name="character">
        ///     <para xml:lang="en">An optional character used to obtain cues when <paramref name="cueSet" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN">可选角色；当 <paramref name="cueSet" /> 为 <see langword="null" /> 时用于获取视觉提示。</para>
        /// </param>
        /// <param name="cueSet">
        ///     <para xml:lang="en">An optional explicit cue set, which takes precedence over character-derived cues.</para>
        ///     <para xml:lang="zh-CN">可选的显式视觉提示集，优先于从角色获得的视觉提示。</para>
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
                if (draft.NextStateId != null)
                    state.NextState = materialised.TryGetValue(draft.NextStateId, out var next)
                        ? next
                        : throw new InvalidOperationException(
                            $"Next state '{draft.NextStateId}' referenced by '{id}' was not declared.");

                foreach (var branch in draft.Branches)
                {
                    if (!materialised.TryGetValue(branch.ToId, out var target))
                        throw new InvalidOperationException(
                            $"Branch target state '{branch.ToId}' referenced by '{id}' was not declared.");

                    state.AddBranch(branch.Trigger, target, branch.Condition);
                }
            }

            initial = materialised[_initialStateId];
            var machine = new ModAnimStateMachine(backend);
            foreach (var branch in _anyBranches)
            {
                if (!materialised.TryGetValue(branch.ToId, out var target))
                    throw new InvalidOperationException(
                        $"Any-state branch target '{branch.ToId}' was not declared.");

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
        ///     <para xml:lang="en">Fluently configures metadata for a state declared by <see cref="ModAnimStateMachineBuilder.AddState" />.</para>
        ///     <para xml:lang="zh-CN">以流式方式配置由 <see cref="ModAnimStateMachineBuilder.AddState" /> 声明的状态元数据。</para>
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
            ///     <para xml:lang="en">Sets the current state's next-state ID.</para>
            ///     <para xml:lang="zh-CN">设置当前状态的下一状态 ID。</para>
            /// </summary>
            public StateScope WithNext(string nextStateId)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(nextStateId);
                _draft.NextStateId = nextStateId;
                return this;
            }

            /// <summary>
            ///     <para xml:lang="en">Sets the bounds-container tag reported when this state is entered.</para>
            ///     <para xml:lang="zh-CN">设置进入此状态时报告的边界容器标签。</para>
            /// </summary>
            public StateScope WithBounds(string boundsContainer)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(boundsContainer);
                _draft.BoundsContainer = boundsContainer;
                return this;
            }

            /// <summary>
            ///     <para xml:lang="en">Marks the current state as initial, replacing the default first declared state.</para>
            ///     <para xml:lang="zh-CN">将当前状态标记为初始状态，替代默认的第一个已声明状态。</para>
            /// </summary>
            public StateScope AsInitial()
            {
                _owner._initialStateId = _draft.Id;
                return this;
            }

            /// <summary>
            ///     <para xml:lang="en">Returns the owning builder.</para>
            ///     <para xml:lang="zh-CN">返回所属构建器。</para>
            /// </summary>
            public ModAnimStateMachineBuilder Done()
            {
                return _owner;
            }
        }
    }
}
