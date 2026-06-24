namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsMirrorDiagnostics
    {
        public static InvalidOperationException InvalidConfig(string message)
        {
            return new(message);
        }
    }
}
