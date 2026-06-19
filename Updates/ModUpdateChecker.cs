using System.Net;
using System.Text.Json;
using Godot;
using STS2RitsuLib.Platform.Steam;
using STS2RitsuLib.Ui.Toast;
using STS2RitsuLib.Utils;
using HttpClient = System.Net.Http.HttpClient;

namespace STS2RitsuLib.Updates
{
    /// <summary>
    ///     Lightweight update checker for self-hosted or mirrored release manifests.
    ///     用于自托管或镜像发布 manifest 的轻量更新检查器。
    /// </summary>
    public static class ModUpdateChecker
    {
        private const string ExpectedSchema = "ritsulib.update.v1";
        private const int MaxManifestBytes = 512 * 1024;
        private static readonly HttpClient Client = new();
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private static readonly Lock SessionLock = new();
        private static readonly HashSet<string> ScheduledSessionChecks = new(StringComparer.Ordinal);

        /// <summary>
        ///     Schedules one update check when the first main menu is ready, then shows a normal non-persistent toast
        ///     when a newer version is available.
        ///     在首次主菜单就绪时安排一次更新检查；发现新版本时显示普通、非持久 toast。
        /// </summary>
        /// <returns>
        ///     A disposable lifecycle subscription. Disposing it before the first main menu cancels the scheduled check.
        ///     生命周期订阅。首次主菜单前释放它可取消已安排的检查。
        /// </returns>
        public static IDisposable RegisterOnFirstMainMenu(ModUpdateCheckOptions options)
        {
            ValidateOptions(options);

            var key = NormalizeSessionKey(options.ModId);
            lock (SessionLock)
            {
                if (!ScheduledSessionChecks.Add(key))
                    return new NoopDisposable();
            }

            return RitsuLibFramework.SubscribeLifecycleOnce<MainMenuReadyEvent>(evt =>
            {
                _ = CheckAndToastAsync(options);
            });
        }

        /// <summary>
        ///     Schedules one update check using string URLs for the common call path.
        ///     使用字符串 URL 为常见调用路径安排一次更新检查。
        /// </summary>
        public static IDisposable RegisterOnFirstMainMenu(
            string modId,
            string displayName,
            string currentVersion,
            string manifestUrl,
            string? releasePageUrl = null)
        {
            return RegisterOnFirstMainMenu(ModUpdateCheckOptions.Create(
                modId,
                displayName,
                currentVersion,
                manifestUrl,
                releasePageUrl));
        }

        /// <summary>
        ///     Runs the update check immediately without showing UI.
        ///     立即运行更新检查，但不显示 UI。
        /// </summary>
        public static async Task<ModUpdateCheckResult> CheckAsync(
            ModUpdateCheckOptions options,
            CancellationToken cancellationToken = default)
        {
            ValidateOptions(options);

            if (ShouldSkipForSteamWorkshop(options))
                return new(
                    ModUpdateCheckStatus.Skipped,
                    options.CurrentVersion,
                    Message: "External update check skipped because this install is managed by Steam Workshop.");

            if (!SimpleSemanticVersion.TryParse(options.CurrentVersion, out var currentVersion))
                return new(
                    ModUpdateCheckStatus.InvalidData,
                    options.CurrentVersion,
                    Message: $"Current version is not a supported semantic version: {options.CurrentVersion}");

            ModUpdateCheckManifest? manifest;
            try
            {
                manifest = await FetchManifestAsync(options, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when
                (ex is HttpRequestException or IOException or TaskCanceledException or OperationCanceledException)
            {
                return new(
                    ModUpdateCheckStatus.RequestFailed,
                    options.CurrentVersion,
                    Message: ex.Message);
            }
            catch (JsonException ex)
            {
                return new(
                    ModUpdateCheckStatus.InvalidData,
                    options.CurrentVersion,
                    Message: $"Update manifest JSON is invalid: {ex.Message}");
            }

            if (manifest == null)
                return new(
                    ModUpdateCheckStatus.InvalidData,
                    options.CurrentVersion,
                    Message: "Update manifest is empty.");

            if (!string.IsNullOrWhiteSpace(manifest.Schema) &&
                !string.Equals(manifest.Schema.Trim(), ExpectedSchema, StringComparison.Ordinal))
                return new(
                    ModUpdateCheckStatus.InvalidData,
                    options.CurrentVersion,
                    Message: $"Unsupported update manifest schema: {manifest.Schema}");

            if (string.IsNullOrWhiteSpace(manifest.LatestVersion) ||
                !SimpleSemanticVersion.TryParse(manifest.LatestVersion, out var latestVersion))
                return new(
                    ModUpdateCheckStatus.InvalidData,
                    options.CurrentVersion,
                    Message: "Update manifest latest_version is missing or invalid.");

            if (latestVersion.CompareTo(currentVersion) <= 0)
                return new(
                    ModUpdateCheckStatus.UpToDate,
                    options.CurrentVersion,
                    manifest.LatestVersion);

            var releasePageUri = ResolveReleasePageUri(options, manifest);
            if (releasePageUri == null)
                return new(
                    ModUpdateCheckStatus.InvalidData,
                    options.CurrentVersion,
                    manifest.LatestVersion,
                    Message: "Update is available, but no release page URL was provided.");

            return new(
                ModUpdateCheckStatus.UpdateAvailable,
                options.CurrentVersion,
                manifest.LatestVersion,
                releasePageUri,
                BuildToastTitle(options, manifest),
                BuildToastMessage(options, manifest));
        }

        /// <summary>
        ///     Runs the update check immediately using string URLs, without showing UI.
        ///     使用字符串 URL 立即运行更新检查，但不显示 UI。
        /// </summary>
        public static Task<ModUpdateCheckResult> CheckAsync(
            string modId,
            string displayName,
            string currentVersion,
            string manifestUrl,
            string? releasePageUrl = null,
            CancellationToken cancellationToken = default)
        {
            return CheckAsync(
                ModUpdateCheckOptions.Create(
                    modId,
                    displayName,
                    currentVersion,
                    manifestUrl,
                    releasePageUrl),
                cancellationToken);
        }

        /// <summary>
        ///     Runs the update check immediately and shows a toast when a newer version is available.
        ///     立即运行更新检查；发现新版本时显示 toast。
        /// </summary>
        public static async Task<ModUpdateCheckResult> CheckAndToastAsync(
            ModUpdateCheckOptions options,
            bool showCompletionToast = false,
            CancellationToken cancellationToken = default)
        {
            var result = await CheckAsync(options, cancellationToken).ConfigureAwait(false);
            if (result.Status != ModUpdateCheckStatus.UpdateAvailable || result.ReleasePageUri == null)
            {
                LogNonSuccess(options, result);
                if (showCompletionToast)
                    PostToMainLoop(() => ShowCompletionToast(options, result));
                return result;
            }

            PostToMainLoop(() => ShowUpdateToast(options, result));
            return result;
        }

        /// <summary>
        ///     Runs the update check immediately using string URLs and shows a toast when a newer version is available.
        ///     使用字符串 URL 立即运行更新检查；发现新版本时显示 toast。
        /// </summary>
        public static Task<ModUpdateCheckResult> CheckAndToastAsync(
            string modId,
            string displayName,
            string currentVersion,
            string manifestUrl,
            string? releasePageUrl = null,
            bool showCompletionToast = false,
            CancellationToken cancellationToken = default)
        {
            return CheckAndToastAsync(
                ModUpdateCheckOptions.Create(
                    modId,
                    displayName,
                    currentVersion,
                    manifestUrl,
                    releasePageUrl),
                showCompletionToast,
                cancellationToken);
        }

        private static async Task<ModUpdateCheckManifest?> FetchManifestAsync(
            ModUpdateCheckOptions options,
            CancellationToken cancellationToken)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(options.Timeout);

            using var request = new HttpRequestMessage(HttpMethod.Get, options.ManifestUri);
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.UserAgent.ParseAdd($"STS2-RitsuLib/{Const.Version}");
            if (options.Headers != null)
                foreach (var header in options.Headers)
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            using var response = await Client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeout.Token)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotModified)
                return null;

            response.EnsureSuccessStatusCode();

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength is > MaxManifestBytes)
                throw new JsonException($"Update manifest is too large: {contentLength} bytes.");

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            var manifestBytes = await ReadManifestBytesAsync(stream, timeout.Token).ConfigureAwait(false);
            return JsonSerializer.Deserialize<ModUpdateCheckManifest>(manifestBytes, JsonOptions);
        }

        private static async Task<byte[]> ReadManifestBytesAsync(Stream stream, CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];
            using var content = new MemoryStream();
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;

                if (content.Length + read > MaxManifestBytes)
                    throw new JsonException($"Update manifest is too large: more than {MaxManifestBytes} bytes.");

                content.Write(buffer, 0, read);
            }

            return content.ToArray();
        }

        private static Uri? ResolveReleasePageUri(ModUpdateCheckOptions options, ModUpdateCheckManifest manifest)
        {
            if (!string.IsNullOrWhiteSpace(manifest.ReleasePageUrl) &&
                Uri.TryCreate(manifest.ReleasePageUrl.Trim(), UriKind.Absolute, out var manifestUri) &&
                IsWebUri(manifestUri))
                return manifestUri;

            return options.ReleasePageUri;
        }

        private static string BuildToastMessage(ModUpdateCheckOptions options, ModUpdateCheckManifest manifest)
        {
            if (!string.IsNullOrWhiteSpace(options.ToastBody))
                return FormatTemplate(options.ToastBody.Trim(), options, manifest);
            var localized = ResolveLocalizedText(manifest);
            if (!string.IsNullOrWhiteSpace(localized?.Message))
                return FormatTemplate(localized.Message.Trim(), options, manifest);
            if (!string.IsNullOrWhiteSpace(manifest.Message))
                return FormatTemplate(manifest.Message.Trim(), options, manifest);

            var latest = string.IsNullOrWhiteSpace(manifest.LatestVersion)
                ? "a newer version"
                : $"version {manifest.LatestVersion.Trim()}";
            return $"{latest} of {options.DisplayName.Trim()} is available. Click to open the release page.";
        }

        private static string BuildToastTitle(ModUpdateCheckOptions options, ModUpdateCheckManifest manifest)
        {
            if (!string.IsNullOrWhiteSpace(options.ToastTitle))
                return FormatTemplate(options.ToastTitle.Trim(), options, manifest);
            var localized = ResolveLocalizedText(manifest);
            if (!string.IsNullOrWhiteSpace(localized?.Title))
                return FormatTemplate(localized.Title.Trim(), options, manifest);
            return !string.IsNullOrWhiteSpace(manifest.Title)
                ? FormatTemplate(manifest.Title.Trim(), options, manifest)
                : $"{options.DisplayName.Trim()} update available";
        }

        private static string FormatTemplate(
            string template,
            ModUpdateCheckOptions options,
            ModUpdateCheckManifest manifest)
        {
            return template
                .Replace("{display_name}", options.DisplayName.Trim(), StringComparison.OrdinalIgnoreCase)
                .Replace("{current_version}", options.CurrentVersion.Trim(), StringComparison.OrdinalIgnoreCase)
                .Replace("{latest_version}", manifest.LatestVersion?.Trim() ?? "", StringComparison.OrdinalIgnoreCase);
        }

        private static void ShowUpdateToast(ModUpdateCheckOptions options, ModUpdateCheckResult result)
        {
            if (result.ReleasePageUri == null)
                return;

            RitsuToastService.Show(new(
                result.Message ?? $"{options.DisplayName.Trim()} update available.",
                result.Title ?? $"{options.DisplayName.Trim()} update available",
                null,
                RitsuToastLevel.Info,
                options.ToastDurationSeconds,
                () => OpenReleasePage(result.ReleasePageUri))
            {
                IsPersistent = false,
            });
        }

        private static void ShowCompletionToast(ModUpdateCheckOptions options, ModUpdateCheckResult result)
        {
            switch (result.Status)
            {
                case ModUpdateCheckStatus.UpToDate:
                    RitsuToastService.ShowInfo(
                        $"{options.DisplayName.Trim()} is up to date ({result.CurrentVersion}).",
                        $"{options.DisplayName.Trim()} update check");
                    break;
                case ModUpdateCheckStatus.Skipped:
                    break;
                case ModUpdateCheckStatus.InvalidData:
                case ModUpdateCheckStatus.RequestFailed:
                    RitsuToastService.ShowWarning(
                        result.Message ?? "Update check failed.",
                        $"{options.DisplayName.Trim()} update check");
                    break;
            }
        }

        private static ModUpdateCheckLocalizedText? ResolveLocalizedText(ModUpdateCheckManifest manifest)
        {
            if (manifest.Localized is not { Count: > 0 })
                return null;

            var language = I18N.ResolveCurrentLanguageCode();
            if (TryGetLocalized(language, out var exact))
                return exact;

            return language switch
            {
                "zhs" when TryGetLocalized("zh-CN", out var zhCn) => zhCn,
                "zhs" when TryGetLocalized("zh", out var zh) => zh,
                "eng" when TryGetLocalized("en", out var en) => en,
                "eng" when TryGetLocalized("en-US", out var enUs) => enUs,
                _ => TryGetLocalized("eng", out var fallbackEng)
                    ? fallbackEng
                    : TryGetLocalized("en", out var fallbackEn)
                        ? fallbackEn
                        : null,
            };

            bool TryGetLocalized(string key, out ModUpdateCheckLocalizedText? text)
            {
                if (manifest.Localized.TryGetValue(key, out text))
                    return true;

                var normalized = I18N.NormalizeLanguageCode(key);
                foreach (var pair in manifest.Localized.Where(pair => string.Equals(
                             I18N.NormalizeLanguageCode(pair.Key), normalized,
                             StringComparison.OrdinalIgnoreCase)))
                {
                    text = pair.Value;
                    return true;
                }

                text = null;
                return false;
            }
        }

        private static void OpenReleasePage(Uri releasePageUri)
        {
            var error = OS.ShellOpen(releasePageUri.ToString());
            if (error != Error.Ok)
                RitsuLibFramework.Logger.Warn(
                    $"[UpdateCheck] Failed to open release page '{releasePageUri}': {error}");
        }

        private static void LogNonSuccess(ModUpdateCheckOptions options, ModUpdateCheckResult result)
        {
            switch (result.Status)
            {
                case ModUpdateCheckStatus.UpToDate:
                    RitsuLibFramework.Logger.Debug(
                        $"[UpdateCheck] {options.ModId} is up to date ({result.CurrentVersion}).");
                    break;
                case ModUpdateCheckStatus.Skipped:
                    RitsuLibFramework.Logger.Debug(
                        $"[UpdateCheck] {options.ModId} skipped: {result.Message ?? result.Status.ToString()}");
                    break;
                case ModUpdateCheckStatus.InvalidData:
                case ModUpdateCheckStatus.RequestFailed:
                    RitsuLibFramework.Logger.Warn(
                        $"[UpdateCheck] {options.ModId} check skipped: {result.Message ?? result.Status.ToString()}");
                    break;
            }
        }

        private static void PostToMainLoop(Action action)
        {
            if (Engine.GetMainLoop() is SceneTree)
            {
                Callable.From(action).CallDeferred();
                return;
            }

            action();
        }

        private static void ValidateOptions(ModUpdateCheckOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.ModId);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.DisplayName);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.CurrentVersion);
            ArgumentNullException.ThrowIfNull(options.ManifestUri);

            if (!options.ManifestUri.IsAbsoluteUri)
                throw new ArgumentException("ManifestUri must be absolute.", nameof(options));
            if (options.ManifestUri.Scheme is not ("http" or "https"))
                throw new ArgumentException("ManifestUri must use http or https.", nameof(options));
            if (options.ReleasePageUri is { IsAbsoluteUri: false })
                throw new ArgumentException("ReleasePageUri must be absolute when provided.", nameof(options));
            if (options.ReleasePageUri != null && !IsWebUri(options.ReleasePageUri))
                throw new ArgumentException("ReleasePageUri must use http or https when provided.", nameof(options));
            if (options.Timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(options), "Timeout must be positive.");
            if (options.SteamWorkshopItemId is 0)
                throw new ArgumentOutOfRangeException(nameof(options),
                    "SteamWorkshopItemId must be positive when provided.");
        }

        private static bool IsWebUri(Uri uri)
        {
            return uri is { IsAbsoluteUri: true, Scheme: "http" or "https" };
        }

        private static bool ShouldSkipForSteamWorkshop(ModUpdateCheckOptions options)
        {
            if (!options.SkipWhenLoadedFromSteamWorkshop)
                return false;

            if (options.SteamWorkshopItemId is { } itemId)
            {
                if (!string.IsNullOrWhiteSpace(options.InstallSourcePath))
                    return SteamWorkshopInstallSource.IsPathLoadedFromSteamWorkshopItem(
                        options.InstallSourcePath,
                        itemId);

                return options.InstallSourceAssembly != null &&
                       SteamWorkshopInstallSource.IsAssemblyLoadedFromSteamWorkshopItem(
                           options.InstallSourceAssembly,
                           itemId);
            }

            if (!string.IsNullOrWhiteSpace(options.InstallSourcePath))
                return SteamWorkshopInstallSource.IsPathLoadedFromSteamWorkshop(options.InstallSourcePath);

            return options.InstallSourceAssembly != null &&
                   SteamWorkshopInstallSource.IsAssemblyLoadedFromSteamWorkshop(options.InstallSourceAssembly);
        }

        private static string NormalizeSessionKey(string modId)
        {
            return modId.Trim();
        }

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
