namespace STS2RitsuLib.Unlocks
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Emits at most one warning per key, keeping mod characters with missing unlock-rule registration playable
    ///         without repeating the warning every combat or frame.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         每个键最多发出一次警告，使缺少解锁规则注册的模组角色仍可游玩，同时避免每场战斗或每帧重复记录。
    ///     </para>
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
