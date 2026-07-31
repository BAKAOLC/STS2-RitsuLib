namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Controls path-keyed looping events through the game's native loop queue.</para>
    ///     <para xml:lang="zh-CN">通过游戏的原生循环队列控制按路径分组的循环事件。</para>
    /// </summary>
    public interface IFmodLoopPlayback
    {
        /// <summary>
        ///     <para xml:lang="en">Creates and starts another loop instance under an event path.</para>
        ///     <para xml:lang="zh-CN">在事件路径下创建并启动另一个循环实例。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The FMOD Studio event path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 事件路径。</para>
        /// </param>
        /// <param name="usesLoopParam">
        ///     <para xml:lang="en">Whether stopping uses the game's <c>loop = 1</c> convention instead of an immediate event stop.</para>
        ///     <para xml:lang="zh-CN">停止时是否使用游戏的 <c>loop = 1</c> 约定，而不是立即停止事件。</para>
        /// </param>
        void PlayLoop(string eventPath, bool usesLoopParam = true);

        /// <summary>
        ///     <para xml:lang="en">Stops the oldest queued loop instance under an event path.</para>
        ///     <para xml:lang="zh-CN">停止事件路径下队列中最早的循环实例。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The FMOD Studio event path used to group the loop.</para>
        ///     <para xml:lang="zh-CN">用于对循环分组的 FMOD Studio 事件路径。</para>
        /// </param>
        void StopLoop(string eventPath);

        /// <summary>
        ///     <para xml:lang="en">Sets a numeric parameter on the oldest queued loop instance under an event path.</para>
        ///     <para xml:lang="zh-CN">为事件路径下队列中最早的循环实例设置数值参数。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The FMOD Studio event path used to find the loop.</para>
        ///     <para xml:lang="zh-CN">用于查找循环的 FMOD Studio 事件路径。</para>
        /// </param>
        /// <param name="parameterName">
        ///     <para xml:lang="en">The FMOD parameter name.</para>
        ///     <para xml:lang="zh-CN">FMOD 参数名称。</para>
        /// </param>
        /// <param name="value">
        ///     <para xml:lang="en">The numeric parameter value.</para>
        ///     <para xml:lang="zh-CN">数值参数值。</para>
        /// </param>
        void SetLoopParameter(string eventPath, string parameterName, float value);

        /// <summary>
        ///     <para xml:lang="en">Stops every loop instance currently held by the game's native loop queue.</para>
        ///     <para xml:lang="zh-CN">停止游戏原生循环队列当前持有的所有循环实例。</para>
        /// </summary>
        void StopAllLoops();
    }
}
