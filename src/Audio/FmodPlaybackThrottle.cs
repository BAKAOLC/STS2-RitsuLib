using System.Diagnostics;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Per-key cooldown for rapid triggers (ms, via <see cref="Stopwatch" /> ticks).
    ///     按 key 记录快速触发的冷却时间（毫秒，通过 <see cref="Stopwatch" /> tick 计算）。
    /// </summary>
    public static class FmodPlaybackThrottle
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<string, long> LastTicks = new(StringComparer.Ordinal);

        /// <summary>
        ///     Returns false if <paramref name="key" /> was used within <paramref name="cooldownMs" />.
        ///     如果 <paramref name="key" /> 在 <paramref name="cooldownMs" /> 内已使用，则返回 false。
        /// </summary>
        public static bool TryEnter(string key, int cooldownMs)
        {
            if (cooldownMs <= 0)
                return true;

            var now = Stopwatch.GetTimestamp();
            var threshold = (long)(cooldownMs * Stopwatch.Frequency / 1000.0);

            lock (Gate)
            {
                if (LastTicks.TryGetValue(key, out var last) && now - last < threshold)
                    return false;

                LastTicks[key] = now;
                return true;
            }
        }

        /// <summary>
        ///     Removes cooldown state for <paramref name="key" /> so the next <see cref="TryEnter" /> may pass.
        ///     移除 <paramref name="key" /> 的 cooldown 状态，使下一次 <see cref="TryEnter" /> 可以通过。
        /// </summary>
        public static void Clear(string key)
        {
            lock (Gate)
            {
                LastTicks.Remove(key);
            }
        }

        /// <summary>
        ///     Clears all throttle keys.
        ///     清除所有 throttle key。
        /// </summary>
        public static void ClearAll()
        {
            lock (Gate)
            {
                LastTicks.Clear();
            }
        }
    }
}
