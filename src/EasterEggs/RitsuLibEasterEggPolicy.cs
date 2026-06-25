namespace STS2RitsuLib
{
    internal static class RitsuLibEasterEggPolicy
    {
        private static readonly TimeSpan BeijingOffset = TimeSpan.FromHours(8);

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
