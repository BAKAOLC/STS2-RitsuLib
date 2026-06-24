using System.Runtime.InteropServices;

namespace STS2RitsuLib.Platform
{
    internal static class SteamCompatibilityRuntime
    {
        private static readonly bool HasSteamCompatDataPath = HasEnvironmentValue("STEAM_COMPAT_DATA_PATH");

        private static readonly bool HasSteamCompatClientInstallPath =
            HasEnvironmentValue("STEAM_COMPAT_CLIENT_INSTALL_PATH");

        private static readonly bool HasWinePrefix = HasEnvironmentValue("WINEPREFIX");

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
