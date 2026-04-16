namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsMirrorRegistrarBootstrap
    {
        public static int TryRegisterMirroredPages()
        {
            var added = 0;
            added += BaseLibMirrorSource.TryRegisterMirroredPages();
            added += ModConfigMirrorSource.TryRegisterMirroredPages();
            added += RuntimeInteropMirrorSource.TryRegisterMirroredPages();
            added += BaseLibToRitsuGeneratedMirrorSource.TryRegisterMirroredPages();
            return added;
        }
    }
}
