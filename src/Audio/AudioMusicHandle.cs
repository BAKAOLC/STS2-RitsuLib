using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Handle for long-lived music playback.
    ///     长期音乐播放的句柄。
    /// </summary>
    public sealed class AudioMusicHandle(AudioSource source, AudioLifecycleScope scope, GodotObject? rawInstance)
        : AudioHandleBase(source, scope, rawInstance)
    {
        /// <summary>
        ///     Replaces this handle's playback with a new source.
        ///     用新源替换此句柄的播放。
        /// </summary>
        public bool TrySwitchTo(AudioSource nextSource, AudioPlaybackOptions? options = null)
        {
            return TrySwitchTo(nextSource, out _, options);
        }

        /// <summary>
        ///     <para xml:lang="en">Starts a replacement music source and returns its handle. This handle is disposed only after the replacement starts successfully.</para>
        ///     <para xml:lang="zh-CN">启动替代音乐源并返回其句柄。只有替代音乐成功开始播放后，才会释放当前句柄。</para>
        /// </summary>
        public bool TrySwitchTo(AudioSource nextSource, out AudioMusicHandle? replacement,
            AudioPlaybackOptions? options = null)
        {
            replacement = GameFmod.Playback.PlayMusic(
                nextSource,
                options ?? new AudioPlaybackOptions { Scope = Scope });
            if (replacement is null)
                return false;

            Dispose();
            return true;
        }
    }
}
