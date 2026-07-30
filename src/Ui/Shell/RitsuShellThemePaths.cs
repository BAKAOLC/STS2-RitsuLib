using Godot;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Ui.Shell
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides paths to the shell-theme directory within global mod data.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供全局模组数据中外壳主题目录的路径。
    ///     </para>
    /// </summary>
    public static class RitsuShellThemePaths
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the virtual <c>user://</c> path to the shell-theme directory.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取外壳主题目录的虚拟 <c>user://</c> 路径。
        ///     </para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The virtual path to the shell-theme directory.</para>
        ///     <para xml:lang="zh-CN">外壳主题目录的虚拟路径。</para>
        /// </returns>
        public static string GetShellThemesDirectoryVirtual()
        {
            var basePath = ProfileManager.GetBasePath(SaveScope.Global, 0);
            return $"{basePath}/{Const.ShellThemesDirectoryName}";
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves the shell-theme directory to an absolute path and creates the directory when necessary.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将外壳主题目录解析为绝对路径，并在需要时创建该目录。
        ///     </para>
        /// </summary>
        /// <param name="absolutePath">
        ///     <para xml:lang="en">
        ///         Receives the resolved absolute path. The value may contain the attempted path when the method
        ///         returns <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         接收解析后的绝对路径。方法返回 <see langword="false" /> 时，该值仍可能包含尝试使用的路径。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if the directory is available; otherwise, <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若目录可用，则为 <see langword="true" />；否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
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
