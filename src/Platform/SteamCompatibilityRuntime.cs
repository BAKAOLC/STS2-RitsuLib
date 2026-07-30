using System.Runtime.InteropServices;

namespace STS2RitsuLib.Platform
{
    /// <summary>
    ///     <para xml:lang="en">Detects Steam Proton launches from the compatibility environment variables supplied to the process.</para>
    ///     <para xml:lang="zh-CN">根据进程获得的兼容环境变量检测 Steam Proton 启动。</para>
    /// </summary>
    internal static class SteamCompatibilityRuntime
    {
        private static readonly bool HasSteamCompatDataPath = HasEnvironmentValue("STEAM_COMPAT_DATA_PATH");

        private static readonly bool HasSteamCompatClientInstallPath =
            HasEnvironmentValue("STEAM_COMPAT_CLIENT_INSTALL_PATH");

        private static readonly bool HasWinePrefix = HasEnvironmentValue("WINEPREFIX");

        /// <summary>
        ///     <para xml:lang="en">Gets whether the process has the Steam compatibility-data, client-install, and Wine-prefix launch markers.</para>
        ///     <para xml:lang="zh-CN">获取进程是否具有 Steam 兼容数据、客户端安装位置及 Wine 前缀启动标记。</para>
        /// </summary>
        public static bool IsProtonLaunch =>
            HasSteamCompatDataPath &&
            HasSteamCompatClientInstallPath &&
            (HasWinePrefix || RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        private static bool HasEnvironmentValue(string name)
        {
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name));
        }
    }
}
