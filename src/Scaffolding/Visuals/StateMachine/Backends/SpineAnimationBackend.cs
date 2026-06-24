using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Random;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends
{
    /// <summary>
    ///     <see cref="IAnimationBackend" /> driver for Spine via <see cref="MegaSprite" />.
    ///     通过 <see cref="MegaSprite" /> 驱动 Spine 的 <see cref="IAnimationBackend" />。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Connects to <c>animation_started</c>, <c>animation_completed</c>, and <c>animation_interrupted</c>
    ///         signals; behaviour mirrors <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator" /> (including
    ///         looping-state random time-scale and start offset for natural idle variation).
    ///     </para>
    ///     <para>
    ///         连接到 <c>animation_started</c>、<c>animation_completed</c> 和 <c>animation_interrupted</c>
    ///         信号；行为对应 <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator" />（包括
    ///         循环状态的随机时间缩放和起始偏移，用于自然的 idle 变化）。
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
        ///     包装给定的 <paramref name="controller" /> 并挂接其生命周期信号。
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
        ///     Spine 没有通过 MegaSpine 绑定暴露干净的“停止轨道”API；此后端通过将动画状态时间缩放设为
        ///     <c>0</c> 来暂停播放。角色会冻结在当前姿势，
        ///     直到再次调用 <see cref="Play" />（这会恢复时间缩放）。这样会按要求保持
        ///     <see cref="Interrupted" /> / <see cref="Completed" /> 静默，即
        ///     <see cref="IAnimationBackend.Stop" /> 的要求。
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
        ///     断开信号连接。可安全多次调用。
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
