namespace STS2RitsuLib.Platform
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Detects mobile launchers that run the PC assembly with a no-op Steam native stub and patched platform
    ///         initialization. Such sessions can appear Steam-backed, but Steamworks.NET entry points and Steam
    ///         transport Sidecar hooks are unsafe.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         检测使用无操作 Steam 原生桩和已修补平台初始化来运行 PC 程序集的移动端启动器。这类会话可能显示为
    ///         Steam 支持，但 Steamworks.NET 入口点和 Steam 传输 Sidecar 钩子均不安全。
    ///     </para>
    /// </summary>
    internal static class RitsuLibMobileSteamRuntime
    {
        /// <summary>
        ///     <para xml:lang="en">Gets whether native Steam integration must be suppressed for the current mobile host.</para>
        ///     <para xml:lang="zh-CN">获取当前移动端宿主是否必须禁用原生 Steam 集成。</para>
        /// </summary>
        internal static bool SuppressNativeSteamIntegration =>
            OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

        /// <summary>
        ///     <para xml:lang="en">Logs the mobile-host Steam restrictions once startup begins.</para>
        ///     <para xml:lang="zh-CN">在启动阶段记录移动端宿主的 Steam 功能限制。</para>
        /// </summary>
        internal static void LogSuppressedSteamFeaturesAtStartup()
        {
            if (!SuppressNativeSteamIntegration)
                return;

            RitsuLibFramework.Logger.Info(
                "[MobileSteam] Native Steamworks calls are disabled on this mobile host. " +
                "Mod data cloud sync remains available when the host provides the game's cloud save store.");
        }
    }
}
