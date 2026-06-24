using System.Runtime.InteropServices;
using Godot;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Ui.Toast;

namespace STS2RitsuLib.Diagnostics
{
    internal static class GameCommonFolderOpener
    {
        internal static void OpenLogsFromUi(string toastTitle, string logPrefix)
        {
            OpenFolder(ResolveLogsFolder, toastTitle, logPrefix, "log folder");
        }

        internal static void OpenSavesFromUi(string toastTitle, string logPrefix)
        {
            OpenFolder(ResolveSavesFolder, toastTitle, logPrefix, "save folder");
        }

        internal static void OpenUserDataRootFromUi(string toastTitle, string logPrefix)
        {
            OpenFolder(ResolveUserDataRoot, toastTitle, logPrefix, "user data folder");
        }

        internal static void OpenBuildLogsFromUi(string toastTitle, string logPrefix)
        {
            OpenFolder(ResolveBuildLogsFolder, toastTitle, logPrefix, "build logs folder");
        }

        internal static void OpenLocalizationOverrideFromUi(string toastTitle, string logPrefix)
        {
            OpenFolder(ResolveLocalizationOverrideFolder, toastTitle, logPrefix, "localization override folder");
        }

        private static void OpenFolder(Func<string?> resolvePath, string toastTitle, string logPrefix, string label)
        {
            string? path = null;
            try
            {
                path = resolvePath();
                if (string.IsNullOrWhiteSpace(path))
                {
                    RitsuLibFramework.Logger.Warn($"{logPrefix} Unable to resolve {label}.");
                    RitsuToastService.ShowWarning($"Could not open {label}: path unavailable", toastTitle);
                    return;
                }

                path = NormalizePath(path);
                Directory.CreateDirectory(path);

                var error = OS.ShellShowInFileManager(path);
                if (error == Error.Ok)
                {
                    RitsuLibFramework.Logger.Info($"{logPrefix} Opened {label}: {path}");
                    return;
                }

                RitsuLibFramework.Logger.Warn($"{logPrefix} Failed to open {label} '{path}'. Error={error}.");
                RitsuToastService.ShowWarning($"Could not open {label}: {error}", toastTitle);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"{logPrefix} Failed to open {label} '{path ?? "<unresolved>"}': {ex.Message}");
                RitsuToastService.ShowWarning($"Could not open {label}: {ex.Message}", toastTitle);
            }
        }

        private static string? ResolveLogsFolder()
        {
            var userDataDir = OS.GetUserDataDir();
            return string.IsNullOrWhiteSpace(userDataDir)
                ? null
                : Path.Combine(userDataDir, "logs");
        }

        private static string? ResolveSavesFolder()
        {
            return ProjectSettings.GlobalizePath(SaveManager.Instance.GetProfileScopedPath("saves"));
        }

        private static string? ResolveUserDataRoot()
        {
            return OS.GetUserDataDir();
        }

        private static string? ResolveBuildLogsFolder()
        {
            var dataDir = OS.GetDataDir();
            return string.IsNullOrWhiteSpace(dataDir)
                ? null
                : Path.Combine(dataDir, "Godot", "mono", "build_logs");
        }

        private static string? ResolveLocalizationOverrideFolder()
        {
            return ProjectSettings.GlobalizePath("user://localization_override");
        }

        private static string NormalizePath(string path)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? path.Replace('/', '\\')
                : path;
        }
    }
}
