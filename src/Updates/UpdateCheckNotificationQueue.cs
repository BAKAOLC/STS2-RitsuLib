using Godot;

namespace STS2RitsuLib.Updates
{
    /// <summary>
    ///     <para xml:lang="en">Defers keyed update-notification actions until a main-menu scene is active.</para>
    ///     <para xml:lang="zh-CN">将带键的更新通知操作延后到主菜单场景处于活动状态时执行。</para>
    /// </summary>
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

                pending = [.. PendingByKey.Values];
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
