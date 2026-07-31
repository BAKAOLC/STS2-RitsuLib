using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">A typed handle for long-lived music playback with transactional replacement support.</para>
    ///     <para xml:lang="zh-CN">用于长期音乐播放并支持事务式替换的类型化句柄。</para>
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
    public sealed class AudioMusicHandle(AudioSource source, AudioLifecycleScope scope, GodotObject? rawInstance)
        : AudioHandleBase(source, scope, rawInstance)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Starts replacement music and disposes this handle, discarding the returned replacement-handle
        ///         reference.
        ///     </para>
        ///     <para xml:lang="zh-CN">启动替代音乐并释放此句柄，同时丢弃返回的替代句柄引用。</para>
        /// </summary>
        /// <param name="nextSource">
        ///     <para xml:lang="en">The music source to start as the replacement.</para>
        ///     <para xml:lang="zh-CN">要作为替代项启动的音乐源。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">The replacement playback options, or null to use defaults with this handle's lifecycle scope.</para>
        ///     <para xml:lang="zh-CN">替代播放选项；为 <see langword="null" /> 时使用带当前句柄生命周期作用域的默认值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when replacement starts and this handle releases successfully;
        ///         otherwise <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">替代音乐成功启动且此句柄成功释放时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public bool TrySwitchTo(AudioSource nextSource, AudioPlaybackOptions? options = null)
        {
            return TrySwitchTo(nextSource, out _, options);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Starts replacement music and returns its handle only after this handle releases successfully. If release
        ///         fails, the replacement is disposed and the switch reports failure.
        ///     </para>
        ///     <para xml:lang="zh-CN">启动替代音乐；仅当此句柄成功释放后才返回替代句柄。释放失败时会释放替代项并报告切换失败。</para>
        /// </summary>
        /// <param name="nextSource">
        ///     <para xml:lang="en">The music source to start as the replacement.</para>
        ///     <para xml:lang="zh-CN">要作为替代项启动的音乐源。</para>
        /// </param>
        /// <param name="replacement">
        ///     <para xml:lang="en">Receives the replacement handle on success; otherwise null.</para>
        ///     <para xml:lang="zh-CN">成功时接收替代句柄；否则为 <see langword="null" />。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">The replacement playback options, or null to use defaults with this handle's lifecycle scope.</para>
        ///     <para xml:lang="zh-CN">替代播放选项；为 <see langword="null" /> 时使用带当前句柄生命周期作用域的默认值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when replacement starts and this handle releases successfully;
        ///         otherwise <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">替代音乐成功启动且此句柄成功释放时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public bool TrySwitchTo(AudioSource nextSource, out AudioMusicHandle? replacement,
            AudioPlaybackOptions? options = null)
        {
            var next = GameFmod.Playback.PlayMusic(
                nextSource,
                options ?? new AudioPlaybackOptions { Scope = Scope });
            if (next is null)
            {
                replacement = null;
                return false;
            }

            Dispose();
            if (IsReleased)
            {
                replacement = next;
                return true;
            }

            next.Dispose();
            replacement = null;
            return false;
        }
    }
}
