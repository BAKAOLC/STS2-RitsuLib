using Godot;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends
{
    /// <summary>
    ///     <para xml:lang="en">Implements <see cref="IAnimationBackend" /> for a Godot <see cref="AnimationPlayer" />.</para>
    ///     <para xml:lang="zh-CN">为 Godot <see cref="AnimationPlayer" /> 实现 <see cref="IAnimationBackend" />。</para>
    /// </summary>
    public sealed class GodotAnimationPlayerBackend : IAnimationBackend, IAnimationTimingProvider
    {
        private readonly Callable _finishedCallable;
        private readonly AnimationPlayer _player;
        private readonly Callable _startedCallable;
        private string? _currentId;
        private bool _suppressEvents;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Wraps <paramref name="player" /> and forwards its started and finished signals, including starts caused by
        ///         the player's native queue.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         包装 <paramref name="player" /> 并转发其开始和结束信号，包括播放器原生队列推进所引起的开始事件。
        ///     </para>
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
        public Node OwnerNode => _player;

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

            _player.ClearQueue();
            if (_player.CurrentAnimation == id)
                _player.Stop();

            _player.Play(id);
        }

        /// <inheritdoc />
        public void Queue(string id, bool loop)
        {
            if (!HasAnimation(id))
                return;

            if (!_player.IsPlaying())
            {
                Play(id, loop);
                return;
            }

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

        /// <inheritdoc />
        public bool TryGetAnimationDuration(string id, out float seconds)
        {
            seconds = 0f;
            if (!HasAnimation(id))
                return false;

            var animation = _player.GetAnimation(id);
            if (animation == null)
                return false;

            seconds = ScaleDuration(animation.Length);
            return seconds > 0f;
        }

        /// <inheritdoc />
        public bool TryGetCurrentAnimationRemaining(out float seconds)
        {
            seconds = 0f;
            var id = _currentId;
            if (string.IsNullOrWhiteSpace(id))
                id = _player.CurrentAnimation;

            if (string.IsNullOrWhiteSpace(id) || !TryGetAnimationDuration(id, out var duration))
                return false;

            seconds = Math.Max(0f, duration - ScaleDuration((float)_player.CurrentAnimationPosition));
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Detaches the signal connections. Repeated calls are safe.</para>
        ///     <para xml:lang="zh-CN">断开信号连接；可安全地重复调用。</para>
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

        private float ScaleDuration(float seconds)
        {
            if (!float.IsFinite(seconds) || seconds <= 0f)
                return 0f;

            var speed = Math.Abs(_player.SpeedScale);
            return speed <= 0f ? seconds : seconds / speed;
        }
    }
}
