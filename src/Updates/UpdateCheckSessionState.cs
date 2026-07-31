using MegaCrit.Sts2.Core.Rooms;

namespace STS2RitsuLib.Updates
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Tracks whether the current session is at the main menu or in a combat room for update-check
    ///         scheduling.
    ///     </para>
    ///     <para xml:lang="zh-CN">跟踪当前会话是否位于主菜单或战斗房间，以供更新检查调度使用。</para>
    /// </summary>
    internal static class UpdateCheckSessionState
    {
        private static int _initialized;
        private static volatile bool _isCombatRoomActive;
        private static volatile bool _isMainMenuActive;

        internal static bool IsCombatRoomActive
        {
            get
            {
                Initialize();
                return _isCombatRoomActive;
            }
        }

        internal static bool IsMainMenuActive
        {
            get
            {
                Initialize();
                return _isMainMenuActive;
            }
        }

        internal static void Initialize()
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
                return;

            var subscriptions = new List<IDisposable>();
            try
            {
                subscriptions.Add(RitsuLibFramework.SubscribeLifecycle<MainMenuReadyEvent>(_ =>
                {
                    _isCombatRoomActive = false;
                    _isMainMenuActive = true;
                    UpdateCheckNotificationQueue.FlushPending();
                }));
                subscriptions.Add(
                    RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(_ => _isMainMenuActive = false));
                subscriptions.Add(
                    RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(_ => _isMainMenuActive = false));
                subscriptions.Add(RitsuLibFramework.SubscribeLifecycle<RoomEnteringEvent>(evt =>
                {
                    _isMainMenuActive = false;
                    if (evt.Room is CombatRoom)
                        _isCombatRoomActive = true;
                }));
                subscriptions.Add(RitsuLibFramework.SubscribeLifecycle<RoomExitedEvent>(evt =>
                {
                    if (evt.Room is CombatRoom)
                        _isCombatRoomActive = false;
                }));
                subscriptions.Add(
                    RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ => _isCombatRoomActive = false));
                Volatile.Write(ref _initialized, 2);
            }
            catch
            {
                for (var i = subscriptions.Count - 1; i >= 0; i--)
                    subscriptions[i].Dispose();
                Volatile.Write(ref _initialized, 0);
                throw;
            }
        }
    }
}
