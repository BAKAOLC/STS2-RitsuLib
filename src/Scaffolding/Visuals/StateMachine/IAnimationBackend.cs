using Godot;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines the uniform playback contract required by <see cref="ModAnimStateMachine" />, allowing the same state
    ///         graph to drive Spine (<c>MegaSprite</c>), Godot <c>AnimationPlayer</c>, <c>AnimatedSprite2D</c>, or cue-frame
    ///         playback through <see cref="STS2RitsuLib.Scaffolding.Visuals.Definition.VisualCueSet" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义 <see cref="ModAnimStateMachine" /> 所需的统一播放契约，使同一状态图可以驱动 Spine
    ///         （<c>MegaSprite</c>）、Godot <c>AnimationPlayer</c>、<c>AnimatedSprite2D</c>，或通过
    ///         <see cref="STS2RitsuLib.Scaffolding.Visuals.Definition.VisualCueSet" /> 定义的视觉提示帧播放。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Implementations raise <see cref="Started" />, <see cref="Completed" />, and <see cref="Interrupted" />
    ///         when the underlying system reports the corresponding events, allowing the state machine to advance to
    ///         <see cref="ModAnimState.NextState" />.
    ///     </para>
    ///     <para xml:lang="en">
    ///         <see cref="Queue" /> is most meaningful for backends with native queue semantics, such as Spine.
    ///         Other backends may forward it to <see cref="Play" /> or defer it until <see cref="Completed" /> fires.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         当底层系统报告对应事件时，实现会触发 <see cref="Started" />、<see cref="Completed" /> 和
    ///         <see cref="Interrupted" />，使状态机可以推进到 <see cref="ModAnimState.NextState" />。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="Queue" /> 对 Spine 等原生支持队列语义的后端最有意义；其他后端可以将其转发到
    ///         <see cref="Play" />，或延迟到 <see cref="Completed" /> 触发后处理。
    ///     </para>
    /// </remarks>
    public interface IAnimationBackend
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the backend's owner node, such as a visuals or merchant root; returns
        ///         <see langword="null" /> when not applicable.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取后端的所有者节点，例如视觉效果或商人的根节点；不适用时返回 <see langword="null" />。
        ///     </para>
        /// </summary>
        Node? OwnerNode { get; }

        /// <summary>
        ///     <para xml:lang="en">Occurs when the backend reports that an animation ID has started playing.</para>
        ///     <para xml:lang="zh-CN">当后端报告某个动画 ID 开始播放时发生。</para>
        /// </summary>
        event Action<string>? Started;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Occurs when the backend reports the end of a loop cycle or one-shot animation.
        ///     </para>
        ///     <para xml:lang="zh-CN">当后端报告一次循环周期或单次动画结束时发生。</para>
        /// </summary>
        event Action<string>? Completed;

        /// <summary>
        ///     <para xml:lang="en">Occurs when the backend reports that an animation was interrupted.</para>
        ///     <para xml:lang="zh-CN">当后端报告动画播放被中断时发生。</para>
        /// </summary>
        event Action<string>? Interrupted;

        /// <summary>
        ///     <para xml:lang="en">Returns whether the backend can play <paramref name="id" />.</para>
        ///     <para xml:lang="zh-CN">返回后端是否能够播放 <paramref name="id" />。</para>
        /// </summary>
        bool HasAnimation(string id);

        /// <summary>
        ///     <para xml:lang="en">Plays <paramref name="id" /> immediately, replacing any active animation.</para>
        ///     <para xml:lang="zh-CN">立即播放 <paramref name="id" />，并替换当前活动的动画。</para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">The animation ID, which must be accepted by <see cref="HasAnimation" />.</para>
        ///     <para xml:lang="zh-CN">动画 ID，必须能通过 <see cref="HasAnimation" /> 检查。</para>
        /// </param>
        /// <param name="loop">
        ///     <para xml:lang="en">
        ///         Whether looping is requested. Backends without loop control treat this as a best-effort hint.
        ///     </para>
        ///     <para xml:lang="zh-CN">是否请求循环；无法控制循环的后端会尽可能遵循此提示。</para>
        /// </param>
        void Play(string id, bool loop);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <paramref name="id" /> after the active animation. Backends without native queues may defer
        ///         <see cref="Play" /> until the next <see cref="Completed" /> event.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <paramref name="id" /> 排在当前动画之后。没有原生队列的后端可以将
        ///         <see cref="Play" /> 延迟到下一次 <see cref="Completed" /> 事件发生时执行。
        ///     </para>
        /// </summary>
        void Queue(string id, bool loop);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Silently stops active playback without raising <see cref="Interrupted" /> or <see cref="Completed" />,
        ///         and clears queued animation. This allows callers such as
        ///         <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends.CompositeAnimationBackend" /> to
        ///         relinquish one backend before activating another.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         静默停止当前播放，不触发 <see cref="Interrupted" /> 或 <see cref="Completed" />，并清除排队的动画。
        ///         这使 <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends.CompositeAnimationBackend" />
        ///         等调用方能够在激活另一个后端之前释放当前后端。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         The default implementation does nothing. Backends that drive visible nodes should override it to halt
        ///         playback and suppress lifecycle events caused by the stop.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         默认实现不执行任何操作。驱动可见节点的后端应重写此方法，以停止播放并抑制停止操作引发的生命周期事件。
        ///     </para>
        /// </remarks>
        void Stop()
        {
        }
    }
}
