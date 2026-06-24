namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsMirrorRegistrarBootstrap
    {
        private static readonly Lock Gate = new();
        private static bool _mainMenuPrewarmCompleted;

        public static int TryRegisterMirroredPages()
        {
            lock (Gate)
            {
                if (_mainMenuPrewarmCompleted)
                    return 0;
            }

            return TryRegisterMirroredPagesCore();
        }

        public static int PrewarmMirroredPagesForMainMenu()
        {
            lock (Gate)
            {
                if (_mainMenuPrewarmCompleted)
                    return 0;
            }

            var added = TryRegisterMirroredPagesCore();
            lock (Gate)
            {
                _mainMenuPrewarmCompleted = true;
            }

            return added;
        }

        private static int TryRegisterMirroredPagesCore()
        {
            var added = 0;
            added += BaseLibMirrorSource.TryRegisterMirroredPages();
            added += JmcModLibMirrorSource.TryRegisterMirroredPages();
            added += ModConfigMirrorSource.TryRegisterMirroredPages();
            added += RuntimeInteropMirrorSource.TryRegisterMirroredPages();
            added += RuntimeReflectionMirrorSource.TryRegisterMirroredPages();
            added += BaseLibToRitsuGeneratedMirrorSource.TryRegisterMirroredPages();
            return added;
        }
    }
}
