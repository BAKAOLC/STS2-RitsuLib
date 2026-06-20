using Godot;

namespace STS2RitsuLib.Updates
{
    internal static class UpdateCheckNotificationQueue
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<string, Action> PendingByKey = new(StringComparer.Ordinal);

        internal static void ShowWhenMainMenu(string key, Action action)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(action);
            UpdateCheckSessionState.Initialize();

            if (UpdateCheckSessionState.IsMainMenuActive)
            {
                PostToMainLoop(action);
                return;
            }

            lock (SyncRoot)
            {
                PendingByKey[key] = action;
            }
        }

        internal static void FlushPending()
        {
            Action[] pending;
            lock (SyncRoot)
            {
                if (PendingByKey.Count == 0)
                    return;

                pending = PendingByKey.Values.ToArray();
                PendingByKey.Clear();
            }

            foreach (var action in pending)
                PostToMainLoop(action);
        }

        private static void PostToMainLoop(Action action)
        {
            if (Engine.GetMainLoop() is SceneTree)
            {
                Callable.From(action).CallDeferred();
                return;
            }

            action();
        }
    }
}
