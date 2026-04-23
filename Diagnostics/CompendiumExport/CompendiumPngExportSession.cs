namespace STS2RitsuLib.Diagnostics.CompendiumExport
{
    internal static class CompendiumPngExportSession
    {
        private static int _stop;

        internal static bool IsStopRequested => Volatile.Read(ref _stop) != 0;

        internal static void ResetForNewRun()
        {
            Volatile.Write(ref _stop, 0);
        }

        internal static void RequestStop()
        {
            Volatile.Write(ref _stop, 1);
        }
    }
}
