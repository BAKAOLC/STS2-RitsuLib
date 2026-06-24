using Godot;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     Uniform driver surface required by <see cref="ModAnimStateMachine" /> so the same state graph can run on
    ///     Spine (<c>MegaSprite</c>), Godot <c>AnimationPlayer</c>, <c>AnimatedSprite2D</c>, or cue-frame-sequence
    ///     playback (see <see cref="STS2RitsuLib.Scaffolding.Visuals.Definition.VisualCueSet" />).
    ///     <see cref="ModAnimStateMachine" /> 所需的统一驱动器表面，使同一状态图可以运行在
    ///     Spine（<c>MegaSprite</c>）、Godot <c>AnimationPlayer</c>、<c>AnimatedSprite2D</c> 或 cue 帧序列
    ///     播放上（见 <see cref="STS2RitsuLib.Scaffolding.Visuals.Definition.VisualCueSet" />）。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Implementations raise <see cref="Started" />, <see cref="Completed" />, and <see cref="Interrupted" />
    ///         whenever the underlying system reports the corresponding events so the state machine can advance
    ///         <see cref="ModAnimState.NextState" />.
    ///     </para>
    ///     <para>
    ///         <see cref="Queue" /> is only meaningful for backends with true queue semantics (Spine); other backends
    ///         may forward it to <see cref="Play" /> or defer until <see cref="Completed" /> fires.
    ///     </para>
    ///     <para>
    ///         当底层系统报告对应事件时，实现会触发 <see cref="Started" />、<see cref="Completed" /> 和 <see cref="Interrupted" />，
    ///         使状态机可以推进
    ///         <see cref="ModAnimState.NextState" />。
    ///     </para>
    ///     <para>
    ///         <see cref="Queue" /> 仅对具备真实队列语义的后端（Spine）有意义；其他后端
    ///         可以将其转发到 <see cref="Play" />，或延迟到 <see cref="Completed" /> 触发后处理。
    ///     </para>
    /// </remarks>
    public interface IAnimationBackend
    {
        /// <summary>
        ///     Backend owner node (visuals root, merchant root, etc.); <see langword="null" /> when not applicable.
        ///     后端拥有者节点（视觉根、商人根节点等）；不适用时为 <see langword="null" />。
        /// </summary>
        Node? OwnerNode { get; }

        /// <summary>
        ///     Fired when the backend reports playback start for animation id <c>arg</c>.
        ///     后端报告动画 id <c>arg</c> 开始播放时触发。
        /// </summary>
        event Action<string>? Started;

        /// <summary>
        ///     Fired when the backend reports playback completion (loop cycle end or one-shot end) for id <c>arg</c>.
        ///     后端报告 id <c>arg</c> 播放完成（循环周期结束或一次性播放结束）时触发。
        /// </summary>
        event Action<string>? Completed;

        /// <summary>
        ///     Fired when the backend reports playback interruption for id <c>arg</c>.
        ///     后端报告 id <c>arg</c> 播放被中断时触发。
        /// </summary>
        event Action<string>? Interrupted;

        /// <summary>
        ///     Returns <see langword="true" /> when the backend can play <paramref name="id" />.
        ///     当后端可以播放 <c>id</c> 时返回 <see langword="true" />。
        /// </summary>
        bool HasAnimation(string id);

        /// <summary>
        ///     Plays <paramref name="id" /> immediately (replaces any active animation).
        ///     立即播放 <c>id</c>（替换任何当前动画）。
        /// </summary>
        /// <param name="id">
        ///     Animation id; must satisfy <see cref="HasAnimation" />.
        ///     动画 id；必须满足 <see cref="HasAnimation" />。
        /// </param>
        /// <param name="loop">
        ///     Loop hint; backends without looping support should treat this as a best-effort flag.
        ///     循环提示；不支持循环的后端应将其视为尽力而为的标志。
        /// </param>
        void Play(string id, bool loop);

        /// <summary>
        ///     Queues <paramref name="id" /> after the currently active animation. Non-queue backends may treat this
        ///     as a deferred <see cref="Play" /> triggered on the next <see cref="Completed" />.
        ///     在当前活动动画之后排队 <paramref name="id" />。非队列后端可以把它视为在下一次
        ///     <see cref="Completed" /> 触发时执行的延迟 <see cref="Play" />。
        /// </summary>
        void Queue(string id, bool loop);

        /// <summary>
        ///     Stops any active playback silently (does not raise <see cref="Interrupted" /> / <see cref="Completed" />)
        ///     and clears any pending queued animation. Intended for callers that need to relinquish the backend —
        ///     typically <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends.CompositeAnimationBackend" />
        ///     during cross-backend transitions, so the previously active backend does not continue visibly playing
        ///     alongside the newly activated one.
        ///     静默停止任何活动播放（不触发 <see cref="Interrupted" /> / <see cref="Completed" />），
        ///     并清除任何待处理的排队动画。用于需要释放后端的调用方，
        ///     通常是 <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends.CompositeAnimationBackend" />
        ///     在跨后端转换期间使用，避免先前活动的后端继续与新激活的后端
        ///     同时可见地播放。
        /// </summary>
        /// <remarks>
        ///     Default implementation is a no-op; backends that drive a visible node should override to halt
        ///     playback and suppress any lifecycle events that the underlying engine may fire as a consequence
        ///     of the stop.
        ///     默认实现为空操作；驱动可见节点的后端应重写它以停止
        ///     播放，并抑制底层引擎可能因停止而触发的任何生命周期事件，
        ///     作为停止操作的后果。
        /// </remarks>
        void Stop()
        {
        }
    }
}
