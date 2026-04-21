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
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The builder does not validate ids against backend animation availability: if a state id is unresolvable
    ///         by the chosen backend, <see cref="ModAnimStateMachine" /> will skip playback for that state and log a
    ///         warning on entry.
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
        /// </summary>
        public ModAnimStateMachineBuilder AddAnyState(string trigger, string toId, Func<bool>? condition = null)
        {
            _anyBranches.Add(new(trigger, toId, condition));
            return this;
        }

        /// <summary>
        ///     Materialises the graph against <paramref name="backend" /> and returns a started
        ///     <see cref="ModAnimStateMachine" />.
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
        /// </summary>
        public ModAnimStateMachine BuildSpine(MegaSprite controller)
        {
            return Build(new SpineAnimationBackend(controller));
        }

        /// <summary>
        ///     Convenience overload: composes cue / Spine / Godot animation player / animated-sprite backends
        ///     rooted at <paramref name="visualsRoot" /> and builds the machine.
        /// </summary>
        /// <param name="visualsRoot">Visuals root (typically an <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCreatureVisuals" />).</param>
        /// <param name="character">
        ///     Optional character model; used when <paramref name="cueSet" /> is <see langword="null" />
        ///     to pull cue data from <c>IModCharacterAssetOverrides</c>.
        /// </param>
        /// <param name="cueSet">Optional explicit cue set; takes priority over the character-derived one.</param>
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
            /// </summary>
            public StateScope WithNext(string nextStateId)
            {
                _draft.NextStateId = nextStateId;
                return this;
            }

            /// <summary>
            ///     Sets <see cref="ModAnimState.BoundsContainer" /> tag emitted via
            ///     <see cref="ModAnimStateMachine.BoundsUpdated" /> on enter.
            /// </summary>
            public StateScope WithBounds(string boundsContainer)
            {
                _draft.BoundsContainer = boundsContainer;
                return this;
            }

            /// <summary>
            ///     Marks the current state as the initial state (overrides the auto-first-state behaviour).
            /// </summary>
            public StateScope AsInitial()
            {
                _owner._initialStateId = _draft.Id;
                return this;
            }

            /// <summary>
            ///     Returns the owning builder so chaining can continue.
            /// </summary>
            public ModAnimStateMachineBuilder Done()
            {
                return _owner;
            }
        }
    }
}
