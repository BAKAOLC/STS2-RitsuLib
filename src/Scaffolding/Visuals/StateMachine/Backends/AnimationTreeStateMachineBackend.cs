using Godot;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Drives a Godot <see cref="AnimationTree" /> whose root is an
    ///         <see cref="AnimationNodeStateMachine" /> through <see cref="IAnimationBackend" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过 <see cref="IAnimationBackend" /> 驱动以 <see cref="AnimationNodeStateMachine" /> 为根节点的
    ///         Godot <see cref="AnimationTree" />。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         State IDs map to state-machine node names, and <see cref="Play" /> transitions through
    ///         <see cref="AnimationNodeStateMachinePlayback.Travel" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         状态 ID 对应状态机节点名称，<see cref="Play" /> 通过
    ///         <see cref="AnimationNodeStateMachinePlayback.Travel" /> 切换状态。
    ///     </para>
    /// </remarks>
    public sealed class AnimationTreeStateMachineBackend : IAnimationBackend
    {
        private readonly Callable _finishedCallable;
        private readonly AnimationNodeStateMachinePlayback _playback;
        private readonly AnimationPlayer? _player;
        private readonly AnimationTree _tree;
        private readonly AnimationNodeStateMachine _treeRoot;
        private string? _currentId;
        private string? _queuedId;
        private bool _queuedLoop;
        private bool _suppressEvents;

        /// <summary>
        ///     <para xml:lang="en">Wraps <paramref name="tree" /> and binds to its state-machine playback object.</para>
        ///     <para xml:lang="zh-CN">包装 <paramref name="tree" />，并绑定其状态机播放对象。</para>
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">
        ///         Thrown when <paramref name="tree" /> is not configured with a valid state-machine root and playback object.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当 <paramref name="tree" /> 未配置有效的状态机根节点和播放对象时抛出。
        ///     </para>
        /// </exception>
        public AnimationTreeStateMachineBackend(AnimationTree tree)
        {
            ArgumentNullException.ThrowIfNull(tree);
            _tree = tree;
            _treeRoot = tree.TreeRoot as AnimationNodeStateMachine
                        ?? throw new ArgumentException(
                            "AnimationTree.TreeRoot must be AnimationNodeStateMachine.", nameof(tree));
            _playback = tree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>()
                        ?? throw new ArgumentException(
                            "AnimationTree is missing a valid parameters/playback object.", nameof(tree));
            _player = ResolveAnimationPlayer(tree);
            _finishedCallable = Callable.From<StringName>(OnAnimationFinished);
            _player?.Connect(AnimationMixer.SignalName.AnimationFinished, _finishedCallable);
        }

        /// <inheritdoc />
        public Node OwnerNode => _tree;

        /// <inheritdoc />
        public event Action<string>? Started;

        /// <inheritdoc />
        public event Action<string>? Completed;

        /// <inheritdoc />
        public event Action<string>? Interrupted;

        /// <inheritdoc />
        public bool HasAnimation(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && _treeRoot.HasNode(id);
        }

        /// <inheritdoc />
        public void Play(string id, bool loop)
        {
            if (!HasAnimation(id))
                return;

            EnsureTreeActive();

            if (_currentId != null)
                Interrupted?.Invoke(_currentId);

            _queuedId = null;
            _currentId = id;
            _playback.Travel(id);
            Started?.Invoke(id);
        }

        /// <inheritdoc />
        public void Queue(string id, bool loop)
        {
            if (!HasAnimation(id))
                return;

            if (_currentId == null)
            {
                Play(id, loop);
                return;
            }

            _queuedId = id;
            _queuedLoop = loop;
        }

        /// <inheritdoc />
        public void Stop()
        {
            _suppressEvents = true;
            try
            {
                _queuedId = null;
                _currentId = null;
                _tree.Active = false;
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Disconnects the optional player signal handler. Repeated calls are safe.</para>
        ///     <para xml:lang="zh-CN">断开可选的播放器信号处理程序；可安全地重复调用。</para>
        /// </summary>
        public void Dispose()
        {
            if (_player == null)
                return;

            if (_player.IsConnected(AnimationMixer.SignalName.AnimationFinished, _finishedCallable))
                _player.Disconnect(AnimationMixer.SignalName.AnimationFinished, _finishedCallable);
        }

        private void OnAnimationFinished(StringName animName)
        {
            if (_suppressEvents || string.IsNullOrEmpty(_currentId))
                return;

            var active = _currentId!;
            _currentId = null;
            Completed?.Invoke(active);

            if (_queuedId is not { } next)
                return;

            var loop = _queuedLoop;
            _queuedId = null;
            Play(next, loop);
        }

        private void EnsureTreeActive()
        {
            if (!_tree.Active)
                _tree.Active = true;
        }

        private static AnimationPlayer? ResolveAnimationPlayer(AnimationTree tree)
        {
            var animPlayerPath = tree.AnimPlayer;
            return animPlayerPath.IsEmpty ? null : tree.GetNodeOrNull<AnimationPlayer>(animPlayerPath);
        }
    }
}
