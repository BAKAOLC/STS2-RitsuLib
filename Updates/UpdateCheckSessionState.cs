using MegaCrit.Sts2.Core.Rooms;

namespace STS2RitsuLib.Updates
{
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
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
                return;

            RitsuLibFramework.SubscribeLifecycle<MainMenuReadyEvent>(_ =>
            {
                _isCombatRoomActive = false;
                _isMainMenuActive = true;
                UpdateCheckNotificationQueue.FlushPending();
            });
            RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(_ => _isMainMenuActive = false);
            RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(_ => _isMainMenuActive = false);
            RitsuLibFramework.SubscribeLifecycle<RoomEnteringEvent>(evt =>
            {
                _isMainMenuActive = false;
                if (evt.Room is CombatRoom)
                    _isCombatRoomActive = true;
            });
            RitsuLibFramework.SubscribeLifecycle<RoomExitedEvent>(evt =>
            {
                if (evt.Room is CombatRoom)
                    _isCombatRoomActive = false;
            });
            RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ => _isCombatRoomActive = false);
        }
    }
}
