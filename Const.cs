namespace STS2RitsuLib
{
    /// <summary>
    ///     Stable identifiers and version constants for the RitsuLib mod assembly.
    /// </summary>
    public static class Const
    {
        /// <summary>
        ///     Human-readable mod name.
        /// </summary>
        public const string Name = "RitsuLib";

        /// <summary>
        ///     Unique mod id used by the game and persistence.
        /// </summary>
        public const string ModId = "com.ritsukage.sts2-RitsuLib";

        /// <summary>
        ///     Assembly / manifest version string.
        /// </summary>
        public const string Version = "0.0.30";

        /// <summary>
        ///     Root key for RitsuLib JSON settings under the mod’s user folder.
        /// </summary>
        public const string SettingsKey = "settings";

        /// <summary>
        ///     On-disk settings file name.
        /// </summary>
        public const string SettingsFileName = "settings.json";
    }
}
