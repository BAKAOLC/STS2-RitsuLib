namespace STS2RitsuLib
{
    /// <summary>
    ///     <para xml:lang="en">Defines stable identifiers and version constants for the RitsuLib mod assembly.</para>
    ///     <para xml:lang="zh-CN">定义 RitsuLib 模组程序集的稳定标识符和版本常量。</para>
    /// </summary>
    public static class Const
    {
        /// <summary>
        ///     <para xml:lang="en">Human-readable mod name.</para>
        ///     <para xml:lang="zh-CN">供人阅读的模组名称。</para>
        /// </summary>
        public const string Name = "RitsuLib";

        /// <summary>
        ///     <para xml:lang="en">Unique mod ID used by the game and persistence.</para>
        ///     <para xml:lang="zh-CN">游戏和持久化使用的唯一模组 ID。</para>
        /// </summary>
        public const string ModId = "com.ritsukage.sts2-RitsuLib";

        /// <summary>
        ///     <para xml:lang="en">Assembly and manifest version string.</para>
        ///     <para xml:lang="zh-CN">程序集和清单版本字符串。</para>
        /// </summary>
        public const string Version = "0.5.14";

        /// <summary>
        ///     <para xml:lang="en">Steam Workshop item ID for the official RitsuLib release.</para>
        ///     <para xml:lang="zh-CN">RitsuLib 正式发布对应的 Steam 创意工坊物品 ID。</para>
        /// </summary>
        public const ulong SteamWorkshopItemId = 3747602295;

        /// <summary>
        ///     <para xml:lang="en">Steam application ID for <c>Slay the Spire 2</c>.</para>
        ///     <para xml:lang="zh-CN"><c>Slay the Spire 2</c> 的 Steam 应用 ID。</para>
        /// </summary>
        public const uint Sts2SteamAppId = 2868840;

        /// <summary>
        ///     <para xml:lang="en">Root key for RitsuLib JSON settings in the mod's user-data directory.</para>
        ///     <para xml:lang="zh-CN">模组用户数据目录中 RitsuLib JSON 设置的根键。</para>
        /// </summary>
        public const string SettingsKey = "settings";

        /// <summary>
        ///     <para xml:lang="en">On-disk settings file name.</para>
        ///     <para xml:lang="zh-CN">磁盘上的设置文件名。</para>
        /// </summary>
        public const string SettingsFileName = "settings.json";

        /// <summary>
        ///     <para xml:lang="en">Global mod-data subdirectory for Shell theme JSON, next to <see cref="SettingsFileName" />.</para>
        ///     <para xml:lang="zh-CN">全局模组数据中用于 Shell 主题 JSON 的子目录，与 <see cref="SettingsFileName" /> 相邻。</para>
        /// </summary>
        public const string ShellThemesDirectoryName = "shell_themes";

        /// <summary>
        ///     <para xml:lang="en">BaseLib's primary Harmony instance ID.</para>
        ///     <para xml:lang="zh-CN">BaseLib 主 Harmony 实例的 ID。</para>
        /// </summary>
        public const string BaseLibHarmonyId = "BaseLib";

        /// <summary>
        ///     <para xml:lang="en">Harmony ID used by the RitsuLib content-registry patcher.</para>
        ///     <para xml:lang="zh-CN">RitsuLib 内容注册表补丁器使用的 Harmony ID。</para>
        /// </summary>
        public const string FrameworkContentRegistryHarmonyId = ModId + ".framework-content-registry";
    }
}
