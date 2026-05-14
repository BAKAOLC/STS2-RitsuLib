namespace STS2RitsuLib.Localization
{
    /// <summary>
    ///     Emits at most one warning per key when ancient dialogue resolution yields nothing but the run must continue.
    ///     当远古事件 dialogue 解析结果为空但跑局必须继续时，每个 key 最多发出一次警告。
    /// </summary>
    internal static class AncientDialogueMissingWarnings
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
