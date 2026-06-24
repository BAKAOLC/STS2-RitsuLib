namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Looping events keyed by path (vanilla loop dictionary semantics).
    ///     按路径作为 key 的循环事件（原版 loop 字典语义）。
    /// </summary>
    public interface IFmodLoopPlayback
    {
        /// <summary>
        ///     Starts or continues a loop; <paramref name="usesLoopParam" /> matches vanilla loop-parameter convention.
        ///     启动或继续循环；<paramref name="usesLoopParam" /> 匹配原版 loop-parameter 约定。
        /// </summary>
        void PlayLoop(string eventPath, bool usesLoopParam = true);

        /// <summary>
        ///     Stops a previously started loop for <paramref name="eventPath" />.
        ///     停止先前为 <paramref name="eventPath" /> 启动的循环。
        /// </summary>
        void StopLoop(string eventPath);

        /// <summary>
        ///     Sets a parameter on the active loop instance for <paramref name="eventPath" />.
        ///     为 <paramref name="eventPath" /> 的活动 loop 实例设置参数。
        /// </summary>
        void SetLoopParameter(string eventPath, string parameterName, float value);

        /// <summary>
        ///     Stops every managed loop.
        ///     停止每个受管理的 loop。
        /// </summary>
        void StopAllLoops();
    }
}
