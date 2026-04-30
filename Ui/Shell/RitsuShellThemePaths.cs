using Godot;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Ui.Shell
{
    /// <summary>
    ///     Paths for the shell themes folder under global mod data.
    /// </summary>
    public static class RitsuShellThemePaths
    {
        /// <summary>
        ///     Virtual <c>user://</c> path to the shell themes directory.
        /// </summary>
        public static string GetShellThemesDirectoryVirtual()
        {
            var basePath = ProfileManager.GetBasePath(SaveScope.Global, 0);
            return $"{basePath}/{Const.ShellThemesDirectoryName}";
        }

        /// <summary>
        ///     Creates the shell themes directory if needed; returns the globalized absolute path.
        /// </summary>
        public static bool TryEnsureShellThemesDirectory(out string absolutePath)
        {
            absolutePath = "";
            try
            {
                var virtualPath = GetShellThemesDirectoryVirtual();
                absolutePath = ProjectSettings.GlobalizePath(virtualPath);
                Directory.CreateDirectory(absolutePath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
