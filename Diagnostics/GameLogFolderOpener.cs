using Godot;
using STS2RitsuLib.Ui.Toast;
using Environment = System.Environment;

namespace STS2RitsuLib.Diagnostics
{
    internal static class GameLogFolderOpener
    {
        internal static void OpenFromUi(string toastTitle, string logPrefix)
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SlayTheSpire2",
                "logs");

            try
            {
                Directory.CreateDirectory(path);
                var error = OS.ShellOpen(new Uri(path + Path.DirectorySeparatorChar).AbsoluteUri);
                if (error == Error.Ok)
                {
                    RitsuLibFramework.Logger.Info($"{logPrefix} Opened log folder: {path}");
                    return;
                }

                RitsuLibFramework.Logger.Warn($"{logPrefix} Failed to open log folder '{path}'. Error={error}.");
                RitsuToastService.ShowWarning(
                    $"Could not open log folder: {error}",
                    toastTitle);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"{logPrefix} Failed to open log folder '{path}': {ex.Message}");
                RitsuToastService.ShowWarning(
                    $"Could not open log folder: {ex.Message}",
                    toastTitle);
            }
        }
    }
}
