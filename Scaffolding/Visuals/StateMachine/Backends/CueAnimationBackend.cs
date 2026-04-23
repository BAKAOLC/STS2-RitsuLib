using Godot;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends
{
    /// <summary>
    ///     <see cref="IAnimationBackend" /> driver for cue-based visuals backed by
    ///     <see cref="VisualCueSet" /> (static textures and/or <see cref="VisualFrameSequence" />).
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
    /// </remarks>
    public sealed class CueAnimationBackend : IAnimationBackend
    {
        private readonly VisualCueSet _cues;
        private readonly Callable _finishedCallable;
        private readonly Node _root;
        private readonly Sprite2D _sprite;
        private string? _currentId;
        private string? _queuedId;
        private bool _queuedLoop;
        private CueFrameSequencePlayer? _subscribedPlayer;

        /// <summary>
        ///     Binds cues <paramref name="cues" /> to sprite <paramref name="sprite" /> rooted at <paramref name="root" />.
        /// </summary>
        public CueAnimationBackend(Node root, Sprite2D sprite, VisualCueSet cues)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(sprite);
            ArgumentNullException.ThrowIfNull(cues);
            _root = root;
            _sprite = sprite;
            _cues = cues;
            _finishedCallable = Callable.From(OnSequenceFinished);
        }

        /// <inheritdoc />
        public Node? OwnerNode => _root;

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
            CueFrameSequencePlayer.StopUnder(_root);

            _queuedId = null;
            _currentId = id;

            if (_cues.FrameSequenceByCue is { Count: > 0 } sequences &&
                TryGetOrdinalIgnoreCase(sequences, id, out var sequence) &&
                sequence is { Frames.Count: > 0 })
            {
                var player = CueFrameSequencePlayer.EnsureUnder(_root);
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
            CueFrameSequencePlayer.StopUnder(_root);
        }

        /// <summary>
        ///     Stops active playback and detaches the frame-sequence signal, if any.
        /// </summary>
        public void Dispose()
        {
            UnsubscribeActivePlayer();
            CueFrameSequencePlayer.StopUnder(_root);
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
            if (!GodotObject.IsInstanceValid(_root))
                return;

            var tree = _root.GetTree();
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
    }
}
