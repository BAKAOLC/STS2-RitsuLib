using Godot;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends
{
    /// <summary>
    ///     <see cref="IAnimationBackend" /> driver for Godot <see cref="AnimationPlayer" />.
    /// </summary>
    public sealed class GodotAnimationPlayerBackend : IAnimationBackend
    {
        private readonly Callable _finishedCallable;
        private readonly AnimationPlayer _player;
        private readonly Callable _startedCallable;
        private string? _currentId;
        private bool _suppressEvents;

        /// <summary>
        ///     Wraps <paramref name="player" /> and hooks <c>AnimationPlayer.AnimationFinished</c> and
        ///     <c>AnimationPlayer.AnimationStarted</c> so queued auto-advances surface as <see cref="Started" />.
        /// </summary>
        public GodotAnimationPlayerBackend(AnimationPlayer player)
        {
            ArgumentNullException.ThrowIfNull(player);
            _player = player;
            _finishedCallable = Callable.From<StringName>(OnAnimationFinished);
            _startedCallable = Callable.From<StringName>(OnAnimationStarted);
            _player.Connect(AnimationMixer.SignalName.AnimationFinished, _finishedCallable);
            _player.Connect(AnimationMixer.SignalName.AnimationStarted, _startedCallable);
        }

        /// <inheritdoc />
        public Node? OwnerNode => _player;

        /// <inheritdoc />
        public event Action<string>? Started;

        /// <inheritdoc />
        public event Action<string>? Completed;

        /// <inheritdoc />
        public event Action<string>? Interrupted;

        /// <inheritdoc />
        public bool HasAnimation(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && _player.HasAnimation(id);
        }

        /// <inheritdoc />
        public void Play(string id, bool loop)
        {
            if (!HasAnimation(id))
                return;

            if (_currentId != null && _player.IsPlaying())
                Interrupted?.Invoke(_currentId);

            _currentId = id;
            var animation = _player.GetAnimation(id);
            if (animation != null)
                animation.LoopMode = loop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None;

            if (_player.CurrentAnimation == id)
                _player.Stop();

            _player.Play(id);
        }

        /// <inheritdoc />
        public void Queue(string id, bool loop)
        {
            if (!HasAnimation(id))
                return;

            var animation = _player.GetAnimation(id);
            if (animation != null)
                animation.LoopMode = loop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None;

            _player.Queue(id);
        }

        /// <inheritdoc />
        public void Stop()
        {
            _currentId = null;
            _suppressEvents = true;
            try
            {
                _player.ClearQueue();
                if (_player.IsPlaying())
                    _player.Stop();
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        /// <summary>
        ///     Detaches the signal connections. Safe to call more than once.
        /// </summary>
        public void Dispose()
        {
            if (_player.IsConnected(AnimationMixer.SignalName.AnimationFinished, _finishedCallable))
                _player.Disconnect(AnimationMixer.SignalName.AnimationFinished, _finishedCallable);
            if (_player.IsConnected(AnimationMixer.SignalName.AnimationStarted, _startedCallable))
                _player.Disconnect(AnimationMixer.SignalName.AnimationStarted, _startedCallable);
        }

        private void OnAnimationStarted(StringName animName)
        {
            if (_suppressEvents)
                return;
            var name = animName.ToString();
            _currentId = name;
            Started?.Invoke(name);
        }

        private void OnAnimationFinished(StringName animName)
        {
            if (_suppressEvents)
                return;
            var name = animName.ToString();
            Completed?.Invoke(name);
        }
    }
}
