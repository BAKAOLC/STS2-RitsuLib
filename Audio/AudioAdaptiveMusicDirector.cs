using System.Collections.Concurrent;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Coordinates adaptive room/combat/victory music playback in response to game lifecycle transitions.
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
        ///     Shared singleton director.
        /// </summary>
        public static AudioAdaptiveMusicDirector Shared { get; } = new();

        /// <summary>
        ///     Disposes framework lifecycle subscriptions owned by this director.
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
        ///     Starts following the supplied adaptive music plan and returns a handle for later shutdown.
        /// </summary>
        public AudioAdaptiveMusicHandle Attach(AudioAdaptiveMusicPlan plan)
        {
            var handle = new AudioAdaptiveMusicHandle(plan);
            _active[handle] = plan;
            RefreshRoomState(handle, plan);
            return handle;
        }

        /// <summary>
        ///     Removes a previously attached adaptive music handle from lifecycle tracking.
        /// </summary>
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
