using Godot;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends
{
    /// <summary>
    ///     <see cref="IAnimationBackend" /> driver for Godot <see cref="AnimationTree" /> with an
    ///     <see cref="AnimationNodeStateMachine" /> root.
    /// </summary>
    /// <remarks>
    ///     State ids map to state-machine node names, and <see cref="Play" /> dispatches through
    ///     <see cref="AnimationNodeStateMachinePlayback.Travel" />.
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
        ///     Wraps <paramref name="tree" /> and binds to its state-machine playback.
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     Thrown when <paramref name="tree" /> is not configured as a state-machine tree.
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
        ///     Detaches the optional player signal connections. Safe to call more than once.
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
