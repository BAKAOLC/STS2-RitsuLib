using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Random;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends
{
    /// <summary>
    ///     <para xml:lang="en">Implements <see cref="IAnimationBackend" /> for Spine through <see cref="MegaSprite" />.</para>
    ///     <para xml:lang="zh-CN">通过 <see cref="MegaSprite" /> 为 Spine 实现 <see cref="IAnimationBackend" />。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Connects to <c>animation_started</c>, <c>animation_completed</c>, and <c>animation_interrupted</c>
    ///         signals; behaviour mirrors <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator" /> (including
    ///         looping-state random time-scale and start offset for natural idle variation).
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         连接到 <c>animation_started</c>、<c>animation_completed</c> 和 <c>animation_interrupted</c>
    ///         信号；行为对应 <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator" />，包括循环状态的随机
    ///         时间缩放和起始偏移，以使待机动画更自然。
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
        ///     <para xml:lang="en">Wraps <paramref name="controller" /> and subscribes to its lifecycle signals.</para>
        ///     <para xml:lang="zh-CN">包装 <paramref name="controller" /> 并订阅其生命周期信号。</para>
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

#if STS2_AT_LEAST_0_108_0
            animationState.SetAnimation(id, loop);
            using var track = animationState.GetCurrent(0);
#else
            var track = animationState.SetAnimation(id, loop);
#endif
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
#if STS2_AT_LEAST_0_108_0
            using var track = animationState.AddAnimationTracked(id, 0f, loop);
#else
            var track = animationState.AddAnimation(id, 0f, loop);
#endif
            if (loop)
                OffsetLoopingAnimation(track);
        }

        /// <inheritdoc />
        /// <remarks>
        ///     <para xml:lang="en">
        ///         MegaSpine provides no direct stop-track operation, so this backend pauses the animation state with a
        ///         time scale of <c>0</c>. The pose remains frozen until <see cref="Play" /> restores the scale, without
        ///         raising <see cref="Interrupted" /> or <see cref="Completed" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         MegaSpine 未提供直接停止轨道的操作，因此此后端将动画状态的时间缩放设为 <c>0</c> 来暂停播放。
        ///         姿势会冻结到 <see cref="Play" /> 恢复缩放为止，且不会触发 <see cref="Interrupted" /> 或
        ///         <see cref="Completed" />。
        ///     </para>
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
        ///     <para xml:lang="en">Detaches signal connections. Repeated calls are safe.</para>
        ///     <para xml:lang="zh-CN">断开信号连接；可安全地重复调用。</para>
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
