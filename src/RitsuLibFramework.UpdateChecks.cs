using System.Reflection;
using STS2RitsuLib.Platform.Steam;
using STS2RitsuLib.Updates;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     Returns whether an assembly appears to be loaded from a Steam Workshop content directory.
        ///     返回程序集是否看起来从 Steam Workshop content 目录加载。
        /// </summary>
        public static bool IsAssemblyLoadedFromSteamWorkshop(Assembly assembly)
        {
            return SteamWorkshopInstallSource.IsAssemblyLoadedFromSteamWorkshop(assembly);
        }

        /// <summary>
        ///     Returns whether an assembly appears to be loaded from the specified Steam Workshop item.
        ///     返回程序集是否看起来从指定 Steam Workshop item 加载。
        /// </summary>
        public static bool IsAssemblyLoadedFromSteamWorkshopItem(Assembly assembly, ulong workshopItemId)
        {
            return SteamWorkshopInstallSource.IsAssemblyLoadedFromSteamWorkshopItem(assembly, workshopItemId);
        }

        /// <summary>
        ///     Tries to read the Steam Workshop item id from an assembly load path.
        ///     尝试从程序集加载路径读取 Steam Workshop item id。
        /// </summary>
        public static bool TryGetSteamWorkshopItemId(Assembly assembly, out ulong workshopItemId)
        {
            return SteamWorkshopInstallSource.TryGetWorkshopItemIdFromAssembly(assembly, out workshopItemId);
        }

        /// <summary>
        ///     Returns whether a path appears to be under a Steam Workshop content directory.
        ///     返回路径是否看起来位于 Steam Workshop content 目录下。
        /// </summary>
        public static bool IsPathLoadedFromSteamWorkshop(string path)
        {
            return SteamWorkshopInstallSource.IsPathLoadedFromSteamWorkshop(path);
        }

        /// <summary>
        ///     Returns whether a path appears to be under the specified Steam Workshop item.
        ///     返回路径是否看起来位于指定 Steam Workshop item 下。
        /// </summary>
        public static bool IsPathLoadedFromSteamWorkshopItem(string path, ulong workshopItemId)
        {
            return SteamWorkshopInstallSource.IsPathLoadedFromSteamWorkshopItem(path, workshopItemId);
        }

        /// <summary>
        ///     Tries to read the Steam Workshop item id from a path.
        ///     尝试从路径读取 Steam Workshop item id。
        /// </summary>
        public static bool TryGetSteamWorkshopItemId(string path, out ulong workshopItemId)
        {
            return SteamWorkshopInstallSource.TryGetWorkshopItemIdFromPath(path, out workshopItemId);
        }

        /// <summary>
        ///     Returns update-check options that skip the external manifest check when the given assembly is loaded from
        ///     Steam Workshop.
        ///     返回更新检查选项；当给定程序集从 Steam Workshop 加载时会跳过外部 manifest 检查。
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
        ///     Returns update-check options that skip the external manifest check when the given install path is under
        ///     Steam Workshop content.
        ///     返回更新检查选项；当给定安装路径位于 Steam Workshop content 下时会跳过外部 manifest 检查。
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
        ///     Registers a periodic non-blocking update check for a mod. Automatic checks start before essential game
        ///     initialization, continue while the startup error dialog is active, and show update toasts only while the
        ///     main menu is active.
        ///     为 Mod 注册周期性非阻塞更新检查。自动检查会在游戏必要初始化前开始，在启动报错菜单期间继续运行，
        ///     并且只在主菜单处于活动状态时显示更新 toast。
        /// </summary>
        /// <returns>
        ///     A disposable registration. Disposing it cancels later automatic checks.
        ///     可释放的注册。释放后会取消后续自动检查。
        /// </returns>
        public static IDisposable RegisterModUpdateCheck(ModUpdateCheckOptions options)
        {
            return ModUpdateChecker.RegisterOnFirstMainMenu(options);
        }

        /// <summary>
        ///     Registers a periodic update check using string URLs for the common mod call path.
        ///     使用字符串 URL 为常见 Mod 调用路径注册周期性更新检查。
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
        ///     Runs a non-blocking update check immediately without showing UI.
        ///     立即运行非阻塞更新检查，但不显示 UI。
        /// </summary>
        public static Task<ModUpdateCheckResult> CheckForModUpdateAsync(
            ModUpdateCheckOptions options,
            CancellationToken cancellationToken = default)
        {
            return ModUpdateChecker.CheckAsync(options, cancellationToken);
        }

        /// <summary>
        ///     Runs an update check immediately using string URLs, without showing UI.
        ///     使用字符串 URL 立即运行更新检查，但不显示 UI。
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
        ///     Runs an update check immediately and shows a toast when an update is available.
        ///     立即运行更新检查；发现更新时显示 toast。
        /// </summary>
        public static Task<ModUpdateCheckResult> CheckForModUpdateAndToastAsync(
            ModUpdateCheckOptions options,
            bool showCompletionToast = false,
            CancellationToken cancellationToken = default)
        {
            return ModUpdateChecker.CheckAndToastAsync(options, showCompletionToast, cancellationToken);
        }

        /// <summary>
        ///     Runs an update check immediately using string URLs and shows a toast when an update is available.
        ///     使用字符串 URL 立即运行更新检查；发现更新时显示 toast。
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
