using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Random;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends
{
    /// <summary>
    ///     <see cref="IAnimationBackend" /> driver for Spine via <see cref="MegaSprite" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Connects to <c>animation_started</c>, <c>animation_completed</c>, and <c>animation_interrupted</c>
    ///         signals; behaviour mirrors <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator" /> (including
    ///         looping-state random time-scale and start offset for natural idle variation).
    ///     </para>
    /// </remarks>
    public sealed class SpineAnimationBackend : IAnimationBackend
    {
        private readonly Callable _completedCallable;
        private readonly MegaSprite _controller;
        private readonly Callable _interruptedCallable;
        private readonly Callable _startedCallable;
        private string? _currentId;
        private bool _paused;

        /// <summary>
        ///     Wraps the given <paramref name="controller" /> and hooks its lifecycle signals.
        /// </summary>
        public SpineAnimationBackend(MegaSprite controller)
        {
            ArgumentNullException.ThrowIfNull(controller);
            _controller = controller;
            OwnerNode = controller.BoundObject as Node;
            _startedCallable = Callable.From<GodotObject, GodotObject, GodotObject>(OnStarted);
            _completedCallable = Callable.From<GodotObject, GodotObject, GodotObject>(OnCompleted);
            _interruptedCallable = Callable.From<GodotObject, GodotObject, GodotObject>(OnInterrupted);
            _controller.ConnectAnimationStarted(_startedCallable);
            _controller.ConnectAnimationCompleted(_completedCallable);
            _controller.ConnectAnimationInterrupted(_interruptedCallable);
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
            return !string.IsNullOrWhiteSpace(id) && _controller.HasAnimation(id);
        }

        /// <inheritdoc />
        public void Play(string id, bool loop)
        {
            if (!HasAnimation(id))
                return;

            _currentId = id;
            var animationState = _controller.GetAnimationState();
            if (_paused)
            {
                animationState.SetTimeScale(1f);
                _paused = false;
            }

            var track = animationState.SetAnimation(id, loop);
            if (track == null)
                return;

            if (loop)
                OffsetLoopingAnimation(track);
        }

        /// <inheritdoc />
        public void Queue(string id, bool loop)
        {
            if (!HasAnimation(id))
                return;

            var animationState = _controller.GetAnimationState();
            var track = animationState.AddAnimation(id, 0f, loop);
            if (loop)
                OffsetLoopingAnimation(track);
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Spine exposes no clean "stop track" API through the MegaSpine bindings; this backend pauses playback
        ///     by setting the animation state time scale to <c>0</c>. The character will freeze on its current pose
        ///     until <see cref="Play" /> is called again (which restores the time scale). This keeps
        ///     <see cref="Interrupted" /> / <see cref="Completed" /> silent as required by
        ///     <see cref="IAnimationBackend.Stop" />.
        /// </remarks>
        public void Stop()
        {
            _currentId = null;
            var animationState = _controller.GetAnimationState();
            if (animationState == null)
                return;
            animationState.SetTimeScale(0f);
            _paused = true;
        }

        /// <summary>
        ///     Detaches signal connections. Safe to call more than once.
        /// </summary>
        public void Dispose()
        {
            _controller.DisconnectAnimationStarted(_startedCallable);
            _controller.DisconnectAnimationCompleted(_completedCallable);
            _controller.DisconnectAnimationInterrupted(_interruptedCallable);
        }

        private void OnStarted(GodotObject first, GodotObject second, GodotObject third)
        {
            Started?.Invoke(ResolveSignalAnimationId(first, second, third));
        }

        private void OnCompleted(GodotObject first, GodotObject second, GodotObject third)
        {
            Completed?.Invoke(ResolveSignalAnimationId(first, second, third));
        }

        private void OnInterrupted(GodotObject first, GodotObject second, GodotObject third)
        {
            Interrupted?.Invoke(ResolveSignalAnimationId(first, second, third));
        }

        private string ResolveSignalAnimationId(GodotObject first, GodotObject second, GodotObject third)
        {
            var animationId =
                TryGetAnimationId(first) ??
                TryGetAnimationId(second) ??
                TryGetAnimationId(third);

            if (string.IsNullOrEmpty(animationId)) return _currentId ?? string.Empty;
            _currentId = animationId;
            return animationId;
        }

        private static string? TryGetAnimationId(GodotObject value)
        {
            if (value.GetClass() != "SpineTrackEntry")
                return null;

            var animationObj = value.Call("get_animation").AsGodotObject();
            if (animationObj == null || !animationObj.HasMethod("get_name"))
                return null;

            var name = animationObj.Call("get_name");
            return name.VariantType == Variant.Type.String ? name.AsString() : null;
        }

        private static void OffsetLoopingAnimation(MegaTrackEntry track)
        {
            track.SetTimeScale(Rng.Chaotic.NextFloat(0.9f, 1.1f));
            var end = track.GetAnimationEnd();
            track.SetTrackTime((end + Rng.Chaotic.NextFloat(-0.1f, 0.1f)) % end);
        }
    }
}
