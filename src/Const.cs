namespace STS2RitsuLib
{
    /// <summary>
    ///     Stable identifiers and version constants for the RitsuLib mod assembly.
    ///     RitsuLib mod 程序集的稳定标识符和版本常量。
    /// </summary>
    public static class Const
    {
        /// <summary>
        ///     Human-readable mod name.
        ///     可读的 mod 名称。
        /// </summary>
        public const string Name = "RitsuLib";

        /// <summary>
        ///     Unique mod id used by the game and persistence.
        ///     游戏和持久化使用的唯一 mod id。
        /// </summary>
        public const string ModId = "com.ritsukage.sts2-RitsuLib";

        /// <summary>
        ///     Assembly / manifest version string.
        ///     程序集/清单版本字符串。
        /// </summary>
        public const string Version = "0.4.52";

        /// <summary>
        ///     Steam Workshop item id for the official RitsuLib release.
        ///     官方 RitsuLib 发布使用的 Steam Workshop item id。
        /// </summary>
        public const ulong SteamWorkshopItemId = 3747602295;

        /// <summary>
        ///     Steam app id for Slay the Spire 2.
        ///     Slay the Spire 2 的 Steam app id。
        /// </summary>
        public const uint Sts2SteamAppId = 2868840;

        /// <summary>
        ///     Root key for RitsuLib JSON settings under the mod’s user folder.
        ///     mod 用户文件夹下 RitsuLib JSON 设置的根键。
        /// </summary>
        public const string SettingsKey = "settings";

        /// <summary>
        ///     On-disk settings file name.
        ///     磁盘上的设置文件名。
        /// </summary>
        public const string SettingsFileName = "settings.json";

        /// <summary>
        ///     Subdirectory under global mod data for shell theme JSON (next to <see cref="SettingsFileName" />).
        ///     全局 mod 数据下用于 shell 主题 JSON 的子目录（与 <see cref="SettingsFileName" /> 相邻）。
        /// </summary>
        public const string ShellThemesDirectoryName = "shell_themes";

        /// <summary>
        ///     BaseLib main Harmony instance id.
        ///     BaseLib 主 Harmony 实例 id。
        /// </summary>
        public const string BaseLibHarmonyId = "BaseLib";

        /// <summary>
        ///     Harmony id used by RitsuLib content-registry patcher.
        ///     RitsuLib 内容注册表补丁器使用的 Harmony id。
        /// </summary>
        public const string FrameworkContentRegistryHarmonyId = ModId + ".framework-content-registry";
    }
}
