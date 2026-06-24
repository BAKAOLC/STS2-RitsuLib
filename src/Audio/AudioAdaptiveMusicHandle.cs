namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Represents an active adaptive music binding that can switch tracks and restore vanilla state when stopped.
    ///     表示一个活动的自适应音乐绑定，可切换曲目，并在停止时恢复原版状态。
    /// </summary>
    public sealed class AudioAdaptiveMusicHandle : IDisposable
    {
        private readonly AudioAdaptiveMusicPlan _plan;
        private AudioMusicHandle? _current;
        private bool _disposed;

        internal AudioAdaptiveMusicHandle(AudioAdaptiveMusicPlan plan)
        {
            _plan = plan;
        }

        /// <summary>
        ///     Stops adaptive playback and unregisters this handle from the shared director.
        ///     停止自适应播放，并从共享 director 注销此句柄。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Stop();
            AudioAdaptiveMusicDirector.Shared.Detach(this);
        }

        internal void SwitchTo(AudioMusicHandle? handle)
        {
            _current?.Dispose();
            _current = handle;
        }

        internal void RefreshVolume(float volume)
        {
            _current?.TrySetVolume(volume);
        }

        /// <summary>
        ///     Stops the current adaptive override and optionally restores vanilla run music.
        ///     停止当前自适应覆盖，并可选择恢复原版跑局音乐。
        /// </summary>
        public void Stop(bool restoreVanillaMusic = true)
        {
            if (_disposed)
                return;

            _current?.Dispose();
            _current = null;

            if (restoreVanillaMusic && _plan.RestoreVanillaMusicOnStop)
                AudioVanillaBridge.RefreshRunMusic();
        }
    }
}
