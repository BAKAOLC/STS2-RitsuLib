using Godot;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends
{
    /// <summary>
    ///     <see cref="IAnimationBackend" /> driver for cue-based visuals backed by
    ///     <see cref="VisualCueSet" /> (static textures and/or <see cref="VisualFrameSequence" />).
    ///     用于 cue 视觉的 <see cref="IAnimationBackend" /> 驱动器，底层由
    ///     <see cref="VisualCueSet" /> 支持（静态纹理和/或 <see cref="VisualFrameSequence" />）。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Animation ids map to cue keys in <see cref="VisualCueSet.FrameSequenceByCue" /> (preferred) or
    ///         <see cref="VisualCueSet.TexturePathByCue" /> (fallback static texture). Frame sequences are played
    ///         through <see cref="CueFrameSequencePlayer" />; its <c>Finished</c> signal is converted to
    ///         <see cref="Completed" />.
    ///     </para>
    ///     <para>
    ///         Non-looping static cues raise <see cref="Completed" /> on the next idle frame so the state machine
    ///         can advance without re-entering the caller synchronously.
    ///     </para>
    ///     <para>
    ///         动画 id 映射到 <see cref="VisualCueSet.FrameSequenceByCue" /> 中的 cue 键（优先）或
    ///         <see cref="VisualCueSet.TexturePathByCue" />（回退静态纹理）。帧序列通过
    ///         <see cref="CueFrameSequencePlayer" /> 播放；其 <c>Finished</c> 信号会转换为
    ///         <see cref="Completed" />。
    ///     </para>
    ///     <para>
    ///         非循环静态 cue 会在下一次 idle 帧触发 <see cref="Completed" />，使状态机
    ///         可以继续推进，而不会同步重入调用方。
    ///     </para>
    /// </remarks>
    public sealed class CueAnimationBackend : IAnimationBackend, IAnimationTimingProvider
    {
        private readonly VisualCueSet _cues;
        private readonly Callable _finishedCallable;
        private readonly Sprite2D _sprite;
        private string? _currentId;
        private string? _queuedId;
        private bool _queuedLoop;
        private CueFrameSequencePlayer? _subscribedPlayer;

        /// <summary>
        ///     Binds cues <paramref name="cues" /> to sprite <paramref name="sprite" /> rooted at <paramref name="root" />.
        ///     将 cue <paramref name="cues" /> 绑定到以 <paramref name="root" /> 为根的精灵 <paramref name="sprite" />。
        /// </summary>
        public CueAnimationBackend(Node root, Sprite2D sprite, VisualCueSet cues)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(sprite);
            ArgumentNullException.ThrowIfNull(cues);
            OwnerNode = root;
            _sprite = sprite;
            _cues = cues;
            _finishedCallable = Callable.From(OnSequenceFinished);
        }

        /// <inheritdoc />
        public Node OwnerNode { get; }

        /// <inheritdoc />
        public event Action<string>? Started;

        /// <inheritdoc />
        public event Action<string>? Completed;

        /// <inheritdoc />
        public event Action<string>? Interrupted;

        /// <inheritdoc />
        public bool HasAnimation(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            if (_cues.FrameSequenceByCue is { Count: > 0 } sequences &&
                TryGetOrdinalIgnoreCase(sequences, id, out var sequence) &&
                sequence is { Frames.Count: > 0 })
                return true;

            return _cues.TexturePathByCue is { Count: > 0 } textures &&
                   TryGetOrdinalIgnoreCase(textures, id, out var path) &&
                   !string.IsNullOrWhiteSpace(path);
        }

        /// <inheritdoc />
        public void Play(string id, bool loop)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            if (_currentId != null)
                Interrupted?.Invoke(_currentId);

            UnsubscribeActivePlayer();
            CueFrameSequencePlayer.StopUnder(OwnerNode);

            _queuedId = null;
            _currentId = id;

            if (_cues.FrameSequenceByCue is { Count: > 0 } sequences &&
                TryGetOrdinalIgnoreCase(sequences, id, out var sequence) &&
                sequence is { Frames.Count: > 0 })
            {
                var player = CueFrameSequencePlayer.EnsureUnder(OwnerNode);
                if (!player.TryStart(_sprite, sequence))
                    return;

                SubscribePlayer(player);
                Started?.Invoke(id);
                return;
            }

            if (_cues.TexturePathByCue is not { Count: > 0 } textures ||
                !TryGetOrdinalIgnoreCase(textures, id, out var path) ||
                string.IsNullOrWhiteSpace(path)) return;
            var tex = ResourceLoader.Load<Texture2D>(path);
            if (tex == null)
                return;

            _sprite.Texture = tex;
            if (_cues.TextureStyleByCue is { Count: > 0 } styles &&
                TryGetOrdinalIgnoreCase(styles, id, out var style))
                style.ApplyTo(_sprite);

            Started?.Invoke(id);

            if (!loop)
                DeferCompletion(id);
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
            _queuedId = null;
            _currentId = null;
            UnsubscribeActivePlayer();
            CueFrameSequencePlayer.StopUnder(OwnerNode);
        }

        /// <inheritdoc />
        public bool TryGetAnimationDuration(string id, out float seconds)
        {
            seconds = 0f;
            if (string.IsNullOrWhiteSpace(id))
                return false;

            if (_cues.FrameSequenceByCue is not { Count: > 0 } sequences ||
                !TryGetOrdinalIgnoreCase(sequences, id, out var sequence) ||
                sequence is not { Frames.Count: > 0 })
                return false;

            seconds = GetSequenceDuration(sequence);
            return seconds > 0f;
        }

        /// <inheritdoc />
        public bool TryGetCurrentAnimationRemaining(out float seconds)
        {
            seconds = 0f;
            return _currentId != null && TryGetAnimationDuration(_currentId, out seconds);
        }

        /// <summary>
        ///     Stops active playback and detaches the frame-sequence signal, if any.
        ///     停止当前播放，并在存在时断开帧序列信号。
        /// </summary>
        public void Dispose()
        {
            UnsubscribeActivePlayer();
            CueFrameSequencePlayer.StopUnder(OwnerNode);
        }

        private void SubscribePlayer(CueFrameSequencePlayer player)
        {
            _subscribedPlayer = player;
            player.Connect(CueFrameSequencePlayer.SignalName.Finished, _finishedCallable);
        }

        private void UnsubscribeActivePlayer()
        {
            if (_subscribedPlayer == null)
                return;

            if (GodotObject.IsInstanceValid(_subscribedPlayer) &&
                _subscribedPlayer.IsConnected(CueFrameSequencePlayer.SignalName.Finished, _finishedCallable))
                _subscribedPlayer.Disconnect(CueFrameSequencePlayer.SignalName.Finished, _finishedCallable);

            _subscribedPlayer = null;
        }

        private void OnSequenceFinished()
        {
            UnsubscribeActivePlayer();
            var id = _currentId ?? string.Empty;
            _currentId = null;
            Completed?.Invoke(id);
            ConsumeQueue();
        }

        private void DeferCompletion(string id)
        {
            if (!GodotObject.IsInstanceValid(OwnerNode))
                return;

            var tree = OwnerNode.GetTree();
            if (tree == null)
            {
                _currentId = null;
                Completed?.Invoke(id);
                ConsumeQueue();
                return;
            }

            var timer = tree.CreateTimer(0.0);
            timer.Timeout += () =>
            {
                if (!GodotObject.IsInstanceValid(OwnerNode) || !GodotObject.IsInstanceValid(_sprite))
                    return;

                if (_currentId != id)
                    return;

                _currentId = null;
                Completed?.Invoke(id);
                ConsumeQueue();
            };
        }

        private void ConsumeQueue()
        {
            if (_queuedId is not { } next)
                return;

            var loop = _queuedLoop;
            _queuedId = null;
            Play(next, loop);
        }

        private static bool TryGetOrdinalIgnoreCase<TValue>(IReadOnlyDictionary<string, TValue> map, string key,
            out TValue? value)
        {
            if (map.TryGetValue(key, out var direct))
            {
                value = direct;
                return true;
            }

            foreach (var kv in map)
            {
                if (!string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                value = kv.Value;
                return true;
            }

            value = default;
            return false;
        }

        private static float GetSequenceDuration(VisualFrameSequence sequence)
        {
            return sequence.Frames.Select(frame => frame.DurationSeconds)
                .Select(seconds => !float.IsFinite(seconds) || seconds <= 0f ? 1f / 60f : seconds).Sum();
        }
    }
}
