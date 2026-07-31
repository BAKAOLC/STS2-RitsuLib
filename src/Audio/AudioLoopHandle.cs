using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">A typed audio handle for loop-oriented playback with restart support.</para>
    ///     <para xml:lang="zh-CN">用于循环型播放并支持重新启动的类型化音频句柄。</para>
    /// </summary>
    /// <param name="source">
    ///     <para xml:lang="en">The logical audio source represented by the instance.</para>
    ///     <para xml:lang="zh-CN">该实例所代表的逻辑音频源。</para>
    /// </param>
    /// <param name="scope">
    ///     <para xml:lang="en">The lifecycle scope associated with the handle.</para>
    ///     <para xml:lang="zh-CN">与句柄关联的生命周期作用域。</para>
    /// </param>
    /// <param name="rawInstance">
    ///     <para xml:lang="en">The underlying FMOD Godot object, or null to create an invalid handle.</para>
    ///     <para xml:lang="zh-CN">底层 FMOD Godot 对象；为 <see langword="null" /> 时创建无效句柄。</para>
    /// </param>
    public sealed class AudioLoopHandle(AudioSource source, AudioLifecycleScope scope, GodotObject? rawInstance)
        : AudioHandleBase(source, scope, rawInstance)
    {
        /// <summary>
        ///     <para xml:lang="en">Attempts to restart playback by stopping the current instance and then starting it again.</para>
        ///     <para xml:lang="zh-CN">尝试先停止当前实例，再重新启动播放。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when both stop and start complete; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">停止和启动均执行完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public bool TryRestart()
        {
            return TryStop() && TryPlay();
        }
    }
}
