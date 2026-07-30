namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Controls one adaptive music-plan attachment and the music handle currently selected for it.</para>
    ///     <para xml:lang="zh-CN">控制一个自适应音乐方案附加关系及其当前选中的音乐句柄。</para>
    /// </summary>
    public sealed class AudioAdaptiveMusicHandle : IDisposable
    {
        private readonly AudioAdaptiveMusicPlan _plan;
        private AudioMusicHandle? _current;
        private int _disposed;

        internal AudioAdaptiveMusicHandle(AudioAdaptiveMusicPlan plan)
        {
            _plan = plan;
        }

        /// <summary>
        ///     <para xml:lang="en">Stops the current override, applies the plan's stop-restoration policy, and permanently detaches this handle from the shared director.</para>
        ///     <para xml:lang="zh-CN">停止当前覆盖，应用方案的停止恢复策略，并将此句柄永久从共享调度器中分离。</para>
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            StopCore(true);
            AudioAdaptiveMusicDirector.Shared.Detach(this);
        }

        internal void SwitchTo(AudioMusicHandle? handle)
        {
            if (handle is null)
                return;

            if (Volatile.Read(ref _disposed) != 0)
            {
                handle.Dispose();
                return;
            }

            Interlocked.Exchange(ref _current, handle)?.Dispose();
            if (Volatile.Read(ref _disposed) != 0)
                Interlocked.Exchange(ref _current, null)?.Dispose();
        }

        internal void RefreshVolume(float volume)
        {
            Volatile.Read(ref _current)?.TrySetVolume(volume);
        }

        /// <summary>
        ///     <para xml:lang="en">Stops the current override without detaching the plan, allowing a later lifecycle event to start it again.</para>
        ///     <para xml:lang="zh-CN">停止当前覆盖但不分离方案，因此后续生命周期事件仍可再次启动该方案。</para>
        /// </summary>
        /// <param name="restoreVanillaMusic">
        ///     <para xml:lang="en">Whether to request restoration of run music when the plan's <see cref="AudioAdaptiveMusicPlan.RestoreVanillaMusicOnStop" /> policy permits it.</para>
        ///     <para xml:lang="zh-CN">在方案的 <see cref="AudioAdaptiveMusicPlan.RestoreVanillaMusicOnStop" /> 策略允许时，是否请求恢复跑局音乐。</para>
        /// </param>
        public void Stop(bool restoreVanillaMusic = true)
        {
            if (Volatile.Read(ref _disposed) != 0)
                return;

            StopCore(restoreVanillaMusic);
        }

        private void StopCore(bool restoreVanillaMusic)
        {
            Interlocked.Exchange(ref _current, null)?.Dispose();

            if (restoreVanillaMusic && _plan.RestoreVanillaMusicOnStop)
                AudioVanillaBridge.RefreshRunMusic();
        }
    }
}
