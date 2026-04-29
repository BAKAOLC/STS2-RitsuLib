namespace STS2RitsuLib.Settings
{
    internal static class SettingsIntegrationEntry
    {
        public static void RegisterBuiltInMirrors()
        {
            ModSettingsMirrorRegistrarBootstrap.TryRegisterMirroredPages();
        }
    }
}
