namespace STS2RitsuLib.Localization
{
    /// <summary>
    ///     <para xml:lang="en">Emits at most one warning for each missing ancient-event dialogue key while allowing the current run to continue.</para>
    ///     <para xml:lang="zh-CN">先古之民事件对话键缺失但当前局内流程仍需继续时，每个键至多发出一次警告。</para>
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
