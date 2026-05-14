namespace STS2RitsuLib.Platform
{
    /// <summary>
    ///     Mobile launchers run the PC assembly with a no-op Steam native stub and patched platform init; the session may
    ///     appear Steam-backed while Steamworks entry points are unsafe. RitsuLib must not call Steamworks.NET or register
    ///     Steam transport sidecar hooks on these hosts.
    ///     移动端启动器使用带无操作 Steam native stub 和已 patch 平台 init 的 PC 程序集；会话可能
    ///     看起来由 Steam 支持，但 Steamworks 入口点并不安全。RitsuLib 不得在这些宿主上调用 Steamworks.NET 或注册
    ///     Steam 传输 sidecar 钩子。
    /// </summary>
    internal static class RitsuLibMobileSteamRuntime
    {
        internal static bool SuppressNativeSteamIntegration =>
            OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

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
