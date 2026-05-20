using STS2RitsuLib.Updates;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     Schedules one non-blocking update check for a mod when the first main menu is ready. The check reads a
        ///     small JSON manifest and shows a normal toast when a newer version is available.
        ///     在首次主菜单就绪时为 Mod 安排一次非阻塞更新检查。检查会读取一个小型 JSON manifest，
        ///     并在发现新版本时显示普通 toast。
        /// </summary>
        /// <returns>
        ///     A disposable lifecycle subscription. Disposing it before the first main menu cancels the scheduled check.
        ///     生命周期订阅。首次主菜单前释放它可取消已安排的检查。
        /// </returns>
        public static IDisposable RegisterModUpdateCheck(ModUpdateCheckOptions options)
        {
            return ModUpdateChecker.RegisterOnFirstMainMenu(options);
        }

        /// <summary>
        ///     Schedules one update check using string URLs for the common mod call path.
        ///     使用字符串 URL 为常见 Mod 调用路径安排一次更新检查。
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
