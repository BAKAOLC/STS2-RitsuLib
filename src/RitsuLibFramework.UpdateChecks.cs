using System.Reflection;
using STS2RitsuLib.Platform.Steam;
using STS2RitsuLib.Updates;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     <para xml:lang="en">Returns whether an assembly appears to be loaded from a Steam Workshop content directory.</para>
        ///     <para xml:lang="zh-CN">返回程序集是否看起来从 Steam Workshop 内容目录加载。</para>
        /// </summary>
        public static bool IsAssemblyLoadedFromSteamWorkshop(Assembly assembly)
        {
            return SteamWorkshopInstallSource.IsAssemblyLoadedFromSteamWorkshop(assembly);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns whether an assembly appears to be loaded from the specified Steam Workshop item.</para>
        ///     <para xml:lang="zh-CN">返回程序集是否看起来从指定的 Steam Workshop 物品加载。</para>
        /// </summary>
        public static bool IsAssemblyLoadedFromSteamWorkshopItem(Assembly assembly, ulong workshopItemId)
        {
            return SteamWorkshopInstallSource.IsAssemblyLoadedFromSteamWorkshopItem(assembly, workshopItemId);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to read a Steam Workshop item ID from an assembly load path.</para>
        ///     <para xml:lang="zh-CN">尝试从程序集加载路径读取 Steam Workshop 物品 ID。</para>
        /// </summary>
        public static bool TryGetSteamWorkshopItemId(Assembly assembly, out ulong workshopItemId)
        {
            return SteamWorkshopInstallSource.TryGetWorkshopItemIdFromAssembly(assembly, out workshopItemId);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns whether a path appears to be under a Steam Workshop content directory.</para>
        ///     <para xml:lang="zh-CN">返回路径是否看起来位于 Steam Workshop 内容目录下。</para>
        /// </summary>
        public static bool IsPathLoadedFromSteamWorkshop(string path)
        {
            return SteamWorkshopInstallSource.IsPathLoadedFromSteamWorkshop(path);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns whether a path appears to be under the specified Steam Workshop item.</para>
        ///     <para xml:lang="zh-CN">返回路径是否看起来位于指定的 Steam Workshop 物品下。</para>
        /// </summary>
        public static bool IsPathLoadedFromSteamWorkshopItem(string path, ulong workshopItemId)
        {
            return SteamWorkshopInstallSource.IsPathLoadedFromSteamWorkshopItem(path, workshopItemId);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to read a Steam Workshop item ID from a path.</para>
        ///     <para xml:lang="zh-CN">尝试从路径读取 Steam Workshop 物品 ID。</para>
        /// </summary>
        public static bool TryGetSteamWorkshopItemId(string path, out ulong workshopItemId)
        {
            return SteamWorkshopInstallSource.TryGetWorkshopItemIdFromPath(path, out workshopItemId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns update-check options that skip the external manifest check when the specified assembly is loaded
        ///         from Steam Workshop.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回更新检查选项；指定程序集从 Steam Workshop 加载时会跳过外部清单检查。</para>
        /// </summary>
        public static ModUpdateCheckOptions SkipModUpdateCheckWhenLoadedFromSteamWorkshop(
            ModUpdateCheckOptions options,
            Assembly installSourceAssembly,
            ulong workshopItemId)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(installSourceAssembly);
            ArgumentOutOfRangeException.ThrowIfZero(workshopItemId);
            return options with
            {
                SkipWhenLoadedFromSteamWorkshop = true,
                InstallSourceAssembly = installSourceAssembly,
                InstallSourcePath = null,
                SteamWorkshopItemId = workshopItemId,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns update-check options that skip the external manifest check when the specified install path is
        ///         under Steam Workshop content.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回更新检查选项；指定安装路径位于 Steam Workshop 内容下时会跳过外部清单检查。</para>
        /// </summary>
        public static ModUpdateCheckOptions SkipModUpdateCheckWhenLoadedFromSteamWorkshop(
            ModUpdateCheckOptions options,
            string installSourcePath,
            ulong workshopItemId)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrWhiteSpace(installSourcePath);
            ArgumentOutOfRangeException.ThrowIfZero(workshopItemId);
            return options with
            {
                SkipWhenLoadedFromSteamWorkshop = true,
                InstallSourcePath = installSourcePath,
                InstallSourceAssembly = null,
                SteamWorkshopItemId = workshopItemId,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a periodic non-blocking update check for a mod. Automatic checks begin before essential game
        ///         initialization, continue while the startup error dialog is active, and show update toasts only at the main
        ///         menu.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为模组注册周期性非阻塞更新检查。自动检查会在游戏必要初始化前开始，在启动错误对话框活动期间继续运行，
        ///         并且仅在主菜单显示更新提示。
        ///     </para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">Disposable registration; disposing it cancels later automatic checks.</para>
        ///     <para xml:lang="zh-CN">可释放的注册；释放后会取消后续自动检查。</para>
        /// </returns>
        public static IDisposable RegisterModUpdateCheck(ModUpdateCheckOptions options)
        {
            return ModUpdateChecker.RegisterOnFirstMainMenu(options);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a periodic update check using string URLs for the common mod call path.</para>
        ///     <para xml:lang="zh-CN">使用字符串 URL 为常见模组调用方式注册周期性更新检查。</para>
        /// </summary>
        public static IDisposable RegisterModUpdateCheck(
            string modId,
            string displayName,
            string currentVersion,
            string manifestUrl,
            string? releasePageUrl = null)
        {
            return RegisterModUpdateCheck(ModUpdateCheckOptions.Create(
                modId,
                displayName,
                currentVersion,
                manifestUrl,
                releasePageUrl));
        }

        /// <summary>
        ///     <para xml:lang="en">Runs a non-blocking update check immediately without showing UI.</para>
        ///     <para xml:lang="zh-CN">立即运行非阻塞更新检查，但不显示界面。</para>
        /// </summary>
        public static Task<ModUpdateCheckResult> CheckForModUpdateAsync(
            ModUpdateCheckOptions options,
            CancellationToken cancellationToken = default)
        {
            return ModUpdateChecker.CheckAsync(options, cancellationToken);
        }

        /// <summary>
        ///     <para xml:lang="en">Runs an update check immediately using string URLs, without showing UI.</para>
        ///     <para xml:lang="zh-CN">使用字符串 URL 立即运行更新检查，但不显示界面。</para>
        /// </summary>
        public static Task<ModUpdateCheckResult> CheckForModUpdateAsync(
            string modId,
            string displayName,
            string currentVersion,
            string manifestUrl,
            string? releasePageUrl = null,
            CancellationToken cancellationToken = default)
        {
            return CheckForModUpdateAsync(
                ModUpdateCheckOptions.Create(
                    modId,
                    displayName,
                    currentVersion,
                    manifestUrl,
                    releasePageUrl),
                cancellationToken);
        }

        /// <summary>
        ///     <para xml:lang="en">Runs an update check immediately and shows a toast when an update is available.</para>
        ///     <para xml:lang="zh-CN">立即运行更新检查；发现更新时显示提示。</para>
        /// </summary>
        public static Task<ModUpdateCheckResult> CheckForModUpdateAndToastAsync(
            ModUpdateCheckOptions options,
            bool showCompletionToast = false,
            CancellationToken cancellationToken = default)
        {
            return ModUpdateChecker.CheckAndToastAsync(options, showCompletionToast, cancellationToken);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Runs an update check immediately using string URLs and shows a toast when an update is
        ///         available.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用字符串 URL 立即运行更新检查；发现更新时显示提示。</para>
        /// </summary>
        public static Task<ModUpdateCheckResult> CheckForModUpdateAndToastAsync(
            string modId,
            string displayName,
            string currentVersion,
            string manifestUrl,
            string? releasePageUrl = null,
            bool showCompletionToast = false,
            CancellationToken cancellationToken = default)
        {
            return CheckForModUpdateAndToastAsync(
                ModUpdateCheckOptions.Create(
                    modId,
                    displayName,
                    currentVersion,
                    manifestUrl,
                    releasePageUrl),
                showCompletionToast,
                cancellationToken);
        }
    }
}
