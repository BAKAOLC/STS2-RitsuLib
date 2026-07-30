using System.Collections.Concurrent;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Tracks adaptive music plans and switches their room, combat, and victory tracks in response to
    ///         run lifecycle events.
    ///     </para>
    ///     <para xml:lang="zh-CN">跟踪自适应音乐方案，并响应跑局生命周期事件切换其房间、战斗和胜利曲目。</para>
    /// </summary>
    public sealed class AudioAdaptiveMusicDirector : IDisposable
    {
        private readonly ConcurrentDictionary<AudioAdaptiveMusicHandle, AudioAdaptiveMusicPlan> _active = new();
        private readonly IDisposable _combatEndedSubscription;
        private readonly IDisposable _combatStartingSubscription;
        private readonly IDisposable _combatVictorySubscription;
        private readonly IDisposable _roomEnteredSubscription;
        private readonly IDisposable _runEndedSubscription;
        private readonly IDisposable _runLoadedSubscription;
        private readonly IDisposable _runStartedSubscription;

        private AudioAdaptiveMusicDirector()
        {
            _runStartedSubscription = RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(_ => RefreshRoomState());
            _runLoadedSubscription = RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(_ => RefreshRoomState());
            _roomEnteredSubscription = RitsuLibFramework.SubscribeLifecycle<RoomEnteredEvent>(_ => RefreshRoomState());
            _combatStartingSubscription =
                RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(_ => SwitchCombatState());
            _combatVictorySubscription =
                RitsuLibFramework.SubscribeLifecycle<CombatVictoryEvent>(_ => SwitchVictoryState());
            _combatEndedSubscription =
                RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(_ => RestoreAfterCombat());
            _runEndedSubscription = RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ => ClearAll(false));
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the shared adaptive music director.</para>
        ///     <para xml:lang="zh-CN">获取共享的自适应音乐调度器。</para>
        /// </summary>
        public static AudioAdaptiveMusicDirector Shared { get; } = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Disposes this director's lifecycle subscriptions without stopping or detaching its currently
        ///         tracked handles.
        ///     </para>
        ///     <para xml:lang="zh-CN">释放此调度器的生命周期订阅，但不会停止或分离当前跟踪的句柄。</para>
        /// </summary>
        public void Dispose()
        {
            _runStartedSubscription.Dispose();
            _runLoadedSubscription.Dispose();
            _roomEnteredSubscription.Dispose();
            _combatStartingSubscription.Dispose();
            _combatVictorySubscription.Dispose();
            _combatEndedSubscription.Dispose();
            _runEndedSubscription.Dispose();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attaches an adaptive plan, immediately applies its room state, and returns its controlling
        ///         handle.
        ///     </para>
        ///     <para xml:lang="zh-CN">附加自适应方案，立即应用其房间状态，并返回控制该方案的句柄。</para>
        /// </summary>
        /// <param name="plan">
        ///     <para xml:lang="en">The lifecycle-driven music plan to track.</para>
        ///     <para xml:lang="zh-CN">要跟踪的生命周期驱动音乐方案。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A handle that can stop the current override or dispose the attachment.</para>
        ///     <para xml:lang="zh-CN">可停止当前覆盖或释放该附加关系的句柄。</para>
        /// </returns>
        public AudioAdaptiveMusicHandle Attach(AudioAdaptiveMusicPlan plan)
        {
            var handle = new AudioAdaptiveMusicHandle(plan);
            _active[handle] = plan;
            RefreshRoomState(handle, plan);
            return handle;
        }

        /// <summary>
        ///     <para xml:lang="en">Removes a handle from lifecycle tracking without stopping its current music.</para>
        ///     <para xml:lang="zh-CN">从生命周期跟踪中移除句柄，但不停止其当前音乐。</para>
        /// </summary>
        /// <param name="handle">
        ///     <para xml:lang="en">The handle to detach.</para>
        ///     <para xml:lang="zh-CN">要分离的句柄。</para>
        /// </param>
        public void Detach(AudioAdaptiveMusicHandle handle)
        {
            _active.TryRemove(handle, out _);
        }

        private void RefreshRoomState()
        {
            foreach (var pair in _active)
                RefreshRoomState(pair.Key, pair.Value);
        }

        private static void RefreshRoomState(AudioAdaptiveMusicHandle handle, AudioAdaptiveMusicPlan plan)
        {
            if (plan.RoomSource is null)
            {
                handle.Stop(false);
                if (plan.RefreshVanillaRoomStateOnRoomEnter)
                    AudioVanillaBridge.RefreshTrackAndAmbience();
                return;
            }

            var music = GameFmod.Playback.PlayMusic(plan.RoomSource, plan.RoomOptions);
            handle.SwitchTo(music);
        }

        private void SwitchCombatState()
        {
            foreach (var pair in _active)
            {
                if (pair.Value.CombatSource is null)
                    continue;

                var music = GameFmod.Playback.PlayMusic(pair.Value.CombatSource, pair.Value.CombatOptions);
                pair.Key.SwitchTo(music);
            }
        }

        private void SwitchVictoryState()
        {
            foreach (var pair in _active)
            {
                if (pair.Value.VictorySource is null)
                    continue;

                var music = GameFmod.Playback.PlayMusic(pair.Value.VictorySource, pair.Value.VictoryOptions);
                pair.Key.SwitchTo(music);
            }
        }

        private void RestoreAfterCombat()
        {
            foreach (var pair in _active)
            {
                if (pair.Value.RestoreVanillaMusicOnCombatEnd)
                {
                    pair.Key.Stop();
                    continue;
                }

                RefreshRoomState(pair.Key, pair.Value);
            }
        }

        private void ClearAll(bool restoreVanillaMusic)
        {
            foreach (var handle in _active.Keys)
                handle.Stop(restoreVanillaMusic);

            _active.Clear();
        }
    }
}
