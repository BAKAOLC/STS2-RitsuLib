using Godot;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends
{
    /// <summary>
    ///     <see cref="IAnimationBackend" /> dispatcher that selects the first child backend reporting
    ///     <see cref="IAnimationBackend.HasAnimation" /> for a given id and routes <see cref="Play" /> /
    ///     <see cref="Queue" /> to it.
    ///     <see cref="IAnimationBackend" /> 调度器：选择第一个报告指定 id 存在
    ///     <see cref="IAnimationBackend.HasAnimation" /> 的子后端，并将 <see cref="Play" /> /
    ///     <see cref="Queue" /> 路由给它。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Priority follows insertion order. Typical wiring (cue frame sequences and static textures first,
    ///         then Spine, then Godot animation player, then animated sprite) is produced by
    ///         <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachineBuilder" />.
    ///     </para>
    ///     <para>
    ///         Only one child is <c>active</c> at a time. Switching to a different backend during
    ///         <see cref="Play" /> raises <see cref="Interrupted" /> for the previously active id and
    ///         <see cref="IAnimationBackend.Stop" />s the outgoing backend so it does not continue playing
    ///         alongside the newly activated one. <see cref="Queue" /> across backends is deferred until the
    ///         current backend reports <see cref="Completed" />, at which point the stashed
    ///         <c>(backend, id, loop)</c> triple is activated.
    ///     </para>
    ///     <para>
    ///         优先级遵循插入顺序。典型接线（先是 cue 帧序列和静态纹理，
    ///         然后是 Spine、Godot animation player、animated sprite）由
    ///         <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachineBuilder" /> 生成。
    ///     </para>
    ///     <para>
    ///         同一时间只有一个子项为 <c>active</c>。在 <see cref="Play" /> 期间切换到不同后端时，
    ///         会为先前活动的 id 触发 <see cref="Interrupted" />，并对即将离开的后端调用
    ///         <see cref="IAnimationBackend.Stop" />，使其不会继续与新激活的后端
    ///         同时播放。跨后端的 <see cref="Queue" /> 会延迟到当前后端报告
    ///         <see cref="Completed" /> 后再执行，此时会激活暂存的
    ///         <c>(backend, id, loop)</c> 三元组。
    ///     </para>
    /// </remarks>
    public sealed class CompositeAnimationBackend : IAnimationBackend
    {
        private readonly IReadOnlyList<IAnimationBackend> _backends;
        private IAnimationBackend? _active;
        private string? _currentId;
        private IAnimationBackend? _queuedBackend;
        private string? _queuedId;
        private bool _queuedLoop;

        /// <summary>
        ///     Creates a composite from <paramref name="backends" /> (priority order).
        ///     从 <paramref name="backends" /> 创建组合后端（按优先级顺序）。
        /// </summary>
        public CompositeAnimationBackend(IReadOnlyList<IAnimationBackend> backends, Node? ownerNode = null)
        {
            ArgumentNullException.ThrowIfNull(backends);
            if (backends.Count == 0)
                throw new ArgumentException("At least one backend is required.", nameof(backends));

            _backends = backends;
            OwnerNode = ownerNode ?? backends[0].OwnerNode;

            foreach (var backend in _backends)
            {
                backend.Started += id => OnChildStarted(backend, id);
                backend.Completed += id => OnChildCompleted(backend, id);
                backend.Interrupted += id => OnChildInterrupted(backend, id);
            }
        }

        /// <inheritdoc />
        public Node? OwnerNode { get; }

        /// <inheritdoc />
        public event Action<string>? Started;

        /// <inheritdoc />
        public event Action<string>? Completed;

        /// <inheritdoc />
        public event Action<string>? Interrupted;

        /// <inheritdoc />
        public bool HasAnimation(string id)
        {
            return _backends.Any(backend => backend.HasAnimation(id));
        }

        /// <inheritdoc />
        public void Play(string id, bool loop)
        {
            var chosen = Resolve(id);
            if (chosen == null)
                return;

            var switching = _active != null && !ReferenceEquals(_active, chosen);
            if (switching && _currentId != null)
                Interrupted?.Invoke(_currentId);

            if (switching)
                _active!.Stop();

            ClearQueued();

            _active = chosen;
            _currentId = id;
            chosen.Play(id, loop);
        }

        /// <inheritdoc />
        public void Queue(string id, bool loop)
        {
            var chosen = Resolve(id);
            if (chosen == null)
                return;

            if (_active == null)
            {
                Play(id, loop);
                return;
            }

            if (ReferenceEquals(_active, chosen))
            {
                ClearQueued();
                chosen.Queue(id, loop);
                return;
            }

            _queuedBackend = chosen;
            _queuedId = id;
            _queuedLoop = loop;
        }

        /// <inheritdoc />
        public void Stop()
        {
            ClearQueued();
            var previous = _active;
            _active = null;
            _currentId = null;
            previous?.Stop();
        }

        private IAnimationBackend? Resolve(string id)
        {
            return _backends.FirstOrDefault(backend => backend.HasAnimation(id));
        }

        private void ClearQueued()
        {
            _queuedBackend = null;
            _queuedId = null;
            _queuedLoop = false;
        }

        private void OnChildStarted(IAnimationBackend backend, string id)
        {
            if (!ReferenceEquals(backend, _active))
                return;

            Started?.Invoke(id);
        }

        private void OnChildCompleted(IAnimationBackend backend, string id)
        {
            if (!ReferenceEquals(backend, _active))
                return;

            Completed?.Invoke(id);

            if (!ReferenceEquals(backend, _active) || _queuedBackend is not { } nextBackend ||
                _queuedId is not { } nextId)
                return;

            var nextLoop = _queuedLoop;
            ClearQueued();

            backend.Stop();
            _active = nextBackend;
            _currentId = nextId;
            nextBackend.Play(nextId, nextLoop);
        }

        private void OnChildInterrupted(IAnimationBackend backend, string id)
        {
            if (!ReferenceEquals(backend, _active))
                return;

            Interrupted?.Invoke(id);
        }
    }
}
