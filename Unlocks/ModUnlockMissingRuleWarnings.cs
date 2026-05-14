namespace STS2RitsuLib.Unlocks
{
    /// <summary>
    ///     Emits at most one warning per key so mod characters missing unlock-rule registration stay playable
    ///     without spamming logs every combat or frame.
    ///     每个键最多输出一次警告，使缺少解锁规则注册的 mod 角色仍可游玩，同时避免每场战斗或每帧刷日志。
    /// </summary>
    internal static class ModUnlockMissingRuleWarnings
    {
        private static readonly Lock SyncRoot = new();
        private static readonly HashSet<string> WarnedKeys = [];

        internal static void WarnOnce(string key, string message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            lock (SyncRoot)
            {
                if (!WarnedKeys.Add(key))
                    return;
            }

            RitsuLibFramework.Logger.Warn(message);
        }
    }
}
