namespace STS2RitsuLib.Platform
{
    /// <summary>
    ///     Mobile launchers run the PC assembly with a no-op Steam native stub and patched platform init; the session may
    ///     appear Steam-backed while Steamworks entry points are unsafe. RitsuLib must not call Steamworks.NET or register
    ///     Steam transport sidecar hooks on these hosts.
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
                "[MobileSteam] Native Steam integration is suppressed on this host (Android/iOS). " +
                "Sidecar: Steam lobby capability route and SteamHost/SteamClient trailer Harmony patches are not registered; " +
                "ENet sidecar is unchanged. " +
                "Mod data cloud: automatic sync, SteamRemoteStorage enumeration, and the settings Steam Cloud section are disabled " +
                "even if the game reports Steam initialized (launcher stub).");
        }
    }
}
