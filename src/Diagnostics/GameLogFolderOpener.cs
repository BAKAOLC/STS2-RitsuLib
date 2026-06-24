namespace STS2RitsuLib.Diagnostics
{
    internal static class GameLogFolderOpener
    {
        internal static void OpenFromUi(string toastTitle, string logPrefix)
        {
            GameCommonFolderOpener.OpenLogsFromUi(toastTitle, logPrefix);
        }
    }
}
