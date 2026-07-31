namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines optional timing information for animation backends that can report clip duration and remaining playback
    ///         time. Existing <see cref="IAnimationBackend" /> implementations do not need to implement this interface.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义可选的动画计时信息，供能够报告片段总时长和剩余播放时间的后端实现。
    ///         现有的 <see cref="IAnimationBackend" /> 实现无需实现此接口。
    ///     </para>
    /// </summary>
    public interface IAnimationTimingProvider
    {
        /// <summary>
        ///     <para xml:lang="en">Tries to get the total playback duration of <paramref name="id" /> in real seconds.</para>
        ///     <para xml:lang="zh-CN">尝试获取 <paramref name="id" /> 以实际秒数表示的总播放时长。</para>
        /// </summary>
        bool TryGetAnimationDuration(string id, out float seconds);

        /// <summary>
        ///     <para xml:lang="en">Tries to get the remaining real seconds of the active animation.</para>
        ///     <para xml:lang="zh-CN">尝试获取当前活动动画剩余的实际秒数。</para>
        /// </summary>
        bool TryGetCurrentAnimationRemaining(out float seconds);
    }
}
