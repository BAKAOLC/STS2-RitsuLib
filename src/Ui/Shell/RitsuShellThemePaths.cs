using Godot;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Ui.Shell
{
    /// <summary>
    ///     Paths for the shell themes folder under global mod data.
    ///     全局 mod 数据下 shell 主题文件夹的路径。
    /// </summary>
    public static class RitsuShellThemePaths
    {
        /// <summary>
        ///     Virtual <c>user://</c> path to the shell themes directory.
        ///     指向 shell 主题目录的虚拟 <c>user://</c> 路径。
        /// </summary>
        public static string GetShellThemesDirectoryVirtual()
        {
            var basePath = ProfileManager.GetBasePath(SaveScope.Global, 0);
            return $"{basePath}/{Const.ShellThemesDirectoryName}";
        }

        /// <summary>
        ///     Creates the shell themes directory if needed; returns the globalized absolute path.
        ///     需要时创建 shell 主题目录；返回全局化后的绝对路径。
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
