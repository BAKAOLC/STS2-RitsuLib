namespace STS2RitsuLib
{
    /// <summary>
    ///     <para xml:lang="en">Provides date-based policy checks for RitsuLib Easter-egg behavior.</para>
    ///     <para xml:lang="zh-CN">提供 RitsuLib 彩蛋行为的日期策略检查。</para>
    /// </summary>
    internal static class RitsuLibEasterEggPolicy
    {
        private static readonly TimeSpan BeijingOffset = TimeSpan.FromHours(8);

        /// <summary>
        ///     <para xml:lang="en">Returns whether the current date is June 27 in the UTC+08:00 Beijing time zone.</para>
        ///     <para xml:lang="zh-CN">返回当前日期在 UTC+08:00 北京时区是否为 6 月 27 日。</para>
        /// </summary>
        public static bool IsJuneTwentySeventhInBeijing()
        {
            return IsJuneTwentySeventhInBeijing(DateTimeOffset.UtcNow);
        }

        internal static bool IsJuneTwentySeventhInBeijing(DateTimeOffset utcNow)
        {
            var beijingNow = utcNow.ToUniversalTime().ToOffset(BeijingOffset);
            return beijingNow is { Month: 6, Day: 27 };
        }
    }
}
