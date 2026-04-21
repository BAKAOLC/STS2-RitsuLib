using Godot;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends
{
    /// <summary>
    ///     <see cref="IAnimationBackend" /> driver for Godot <see cref="AnimatedSprite2D" />.
    /// </summary>
    /// <remarks>
    ///     Loop flag is written back to <see cref="SpriteFrames" /> when it differs from the stored value so the
    ///     state machine's intent wins; completion is reported through <see cref="AnimatedSprite2D.AnimationFinished" />.
    /// </remarks>
    public sealed class AnimatedSprite2DBackend : IAnimationBackend
    {
        private readonly Callable _finishedCallable;
        private readonly AnimatedSprite2D _sprite;
        private string? _currentId;
        private string? _queuedId;
        private bool _queuedLoop;

        /// <summary>
        ///     Wraps <paramref name="sprite" /> and hooks <see cref="AnimatedSprite2D.AnimationFinished" />.
        /// </summary>
        public AnimatedSprite2DBackend(AnimatedSprite2D sprite)
        {
            ArgumentNullException.ThrowIfNull(sprite);
            _sprite = sprite;
            _finishedCallable = Callable.From(OnAnimationFinished);
            _sprite.Connect(AnimatedSprite2D.SignalName.AnimationFinished, _finishedCallable);
        }

        /// <inheritdoc />
        public Node? OwnerNode => _sprite;

        /// <inheritdoc />
        public event Action<string>? Started;

        /// <inheritdoc />
        public event Action<string>? Completed;

        /// <inheritdoc />
        public event Action<string>? Interrupted;

        /// <inheritdoc />
        public bool HasAnimation(string id)
        {
            return !string.IsNullOrWhiteSpace(id) &&
                   _sprite.SpriteFrames != null &&
                   _sprite.SpriteFrames.HasAnimation(id);
        }

        /// <inheritdoc />
        public void Play(string id, bool loop)
        {
            if (!HasAnimation(id))
                return;

            if (_currentId != null && _sprite.IsPlaying())
                Interrupted?.Invoke(_currentId);

            _queuedId = null;
            _currentId = id;
            var frames = _sprite.SpriteFrames;
            if (frames != null && frames.GetAnimationLoop(id) != loop)
                frames.SetAnimationLoop(id, loop);

            _sprite.Play(id);
            Started?.Invoke(id);
        }

        /// <inheritdoc />
        public void Queue(string id, bool loop)
        {
            if (!HasAnimation(id))
                return;

            if (_currentId == null || !_sprite.IsPlaying())
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
            _queuedId = null;
            _currentId = null;
            if (_sprite.IsPlaying())
                _sprite.Stop();
        }

        /// <summary>
        ///     Detaches the signal connection. Safe to call more than once.
        /// </summary>
        public void Dispose()
        {
            if (_sprite.IsConnected(AnimatedSprite2D.SignalName.AnimationFinished, _finishedCallable))
                _sprite.Disconnect(AnimatedSprite2D.SignalName.AnimationFinished, _finishedCallable);
        }

        private void OnAnimationFinished()
        {
            Completed?.Invoke(_currentId ?? _sprite.Animation.ToString());

            if (_queuedId is not { } next)
                return;

            var loop = _queuedLoop;
            _queuedId = null;
            Play(next, loop);
        }
    }
}
