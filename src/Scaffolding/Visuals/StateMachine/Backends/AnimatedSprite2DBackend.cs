using Godot;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends
{
    /// <summary>
    ///     <see cref="IAnimationBackend" /> driver for Godot <see cref="AnimatedSprite2D" />.
    ///     用于 Godot <see cref="AnimatedSprite2D" /> 的 <see cref="IAnimationBackend" /> 驱动器。
    /// </summary>
    /// <remarks>
    ///     Loop flag is written back to <see cref="SpriteFrames" /> when it differs from the stored value so the
    ///     state machine's intent wins; completion is reported through <see cref="AnimatedSprite2D.AnimationFinished" />.
    ///     当循环标志与 <see cref="SpriteFrames" /> 中存储的值不同时，会将其写回，确保
    ///     状态机的意图优先；完成事件通过 <see cref="AnimatedSprite2D.AnimationFinished" /> 报告。
    /// </remarks>
    public sealed class AnimatedSprite2DBackend : IAnimationBackend, IAnimationTimingProvider
    {
        private readonly Callable _finishedCallable;
        private readonly AnimatedSprite2D _sprite;
        private string? _currentId;
        private string? _queuedId;
        private bool _queuedLoop;

        /// <summary>
        ///     Wraps <paramref name="sprite" /> and hooks <see cref="AnimatedSprite2D.AnimationFinished" />.
        ///     包装 <paramref name="sprite" /> 并挂接 <see cref="AnimatedSprite2D.AnimationFinished" />。
        /// </summary>
        public AnimatedSprite2DBackend(AnimatedSprite2D sprite)
        {
            ArgumentNullException.ThrowIfNull(sprite);
            _sprite = sprite;
            _finishedCallable = Callable.From(OnAnimationFinished);
            _sprite.Connect(AnimatedSprite2D.SignalName.AnimationFinished, _finishedCallable);
        }

        /// <inheritdoc />
        public Node OwnerNode => _sprite;

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

        /// <inheritdoc />
        public bool TryGetAnimationDuration(string id, out float seconds)
        {
            seconds = 0f;
            var frames = _sprite.SpriteFrames;
            if (string.IsNullOrWhiteSpace(id) || frames == null || !frames.HasAnimation(id))
                return false;

            var speed = GetAnimationPlaybackSpeed(frames, id, false);
            if (speed <= 0f)
                return false;

            var frameCount = frames.GetFrameCount(id);
            for (var i = 0; i < frameCount; i++)
                seconds += frames.GetFrameDuration(id, i);

            seconds /= speed;
            return seconds > 0f && float.IsFinite(seconds);
        }

        /// <inheritdoc />
        public bool TryGetCurrentAnimationRemaining(out float seconds)
        {
            seconds = 0f;
            var frames = _sprite.SpriteFrames;
            var id = _currentId ?? _sprite.Animation.ToString();
            if (string.IsNullOrWhiteSpace(id) || frames == null || !frames.HasAnimation(id))
                return false;

            var speed = GetAnimationPlaybackSpeed(frames, id, true);
            if (speed <= 0f)
                return false;

            var frameCount = frames.GetFrameCount(id);
            var currentFrame = Math.Clamp(_sprite.Frame, 0, Math.Max(0, frameCount - 1));
            var progress = Math.Clamp(_sprite.FrameProgress, 0f, 1f);
            for (var i = currentFrame; i < frameCount; i++)
            {
                var duration = frames.GetFrameDuration(id, i);
                seconds += i == currentFrame ? duration * (1f - progress) : duration;
            }

            seconds /= speed;
            return seconds >= 0f && float.IsFinite(seconds);
        }

        /// <summary>
        ///     Detaches the signal connection. Safe to call more than once.
        ///     断开信号连接。可安全多次调用。
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

        private float GetAnimationPlaybackSpeed(SpriteFrames frames, string id, bool useLiveSpeed)
        {
            if (useLiveSpeed && _sprite.IsPlaying())
            {
                var liveSpeed = Math.Abs(_sprite.GetPlayingSpeed());
                if (liveSpeed > 0f)
                    return liveSpeed;
            }

            var speed = (float)Math.Abs(frames.GetAnimationSpeed(id) * _sprite.SpeedScale);
            return speed > 0f ? speed : (float)Math.Abs(frames.GetAnimationSpeed(id));
        }
    }
}
