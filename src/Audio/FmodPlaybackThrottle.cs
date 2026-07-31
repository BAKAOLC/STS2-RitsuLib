using System.Diagnostics;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides thread-safe, case-sensitive per-key cooldown gates measured with monotonic
    ///         <see cref="Stopwatch" /> timestamps.
    ///     </para>
    ///     <para xml:lang="zh-CN">提供线程安全且区分大小写的逐键冷却门控，并使用单调递增的 <see cref="Stopwatch" /> 时间戳计时。</para>
    /// </summary>
    public static class FmodPlaybackThrottle
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<string, long> LastTicks = new(StringComparer.Ordinal);

        /// <summary>
        ///     <para xml:lang="en">Attempts to enter a cooldown group, recording the current timestamp only when accepted.</para>
        ///     <para xml:lang="zh-CN">尝试进入冷却分组，并且仅在请求通过时记录当前时间戳。</para>
        /// </summary>
        /// <param name="key">
        ///     <para xml:lang="en">The case-sensitive cooldown-group key.</para>
        ///     <para xml:lang="zh-CN">区分大小写的冷却分组键。</para>
        /// </param>
        /// <param name="cooldownMs">
        ///     <para xml:lang="en">
        ///         The cooldown duration in milliseconds; zero or a negative value always passes without recording
        ///         state.
        ///     </para>
        ///     <para xml:lang="zh-CN">冷却时长（毫秒）；零或负值始终通过且不记录状态。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when the gate is disabled or the prior cooldown has elapsed; otherwise
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">门控被禁用或上次冷却已经结束时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown for a null <paramref name="key" /> when <paramref name="cooldownMs" /> is positive.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="cooldownMs" /> 为正数且 <paramref name="key" /> 为 <see langword="null" /> 时抛出。</para>
        /// </exception>
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
        ///     <para xml:lang="en">Removes the recorded timestamp for one key so its next positive-cooldown entry may pass.</para>
        ///     <para xml:lang="zh-CN">移除一个键已记录的时间戳，使其下一次正冷却时长请求可以通过。</para>
        /// </summary>
        /// <param name="key">
        ///     <para xml:lang="en">The case-sensitive cooldown-group key to clear.</para>
        ///     <para xml:lang="zh-CN">要清除的区分大小写冷却分组键。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="key" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="key" /> 为 <see langword="null" /> 时抛出。</para>
        /// </exception>
        public static void Clear(string key)
        {
            lock (Gate)
            {
                LastTicks.Remove(key);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Clears every recorded cooldown timestamp.</para>
        ///     <para xml:lang="zh-CN">清除所有已记录的冷却时间戳。</para>
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
