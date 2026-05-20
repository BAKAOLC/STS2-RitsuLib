using System.Text.Json.Serialization;

namespace STS2RitsuLib.Updates
{
    /// <summary>
    ///     Options for a non-blocking mod update check.
    ///     非阻塞 Mod 更新检查的选项。
    /// </summary>
    public sealed record ModUpdateCheckOptions
    {
        /// <summary>
        ///     Stable mod identifier used for diagnostics and one-check-per-session de-duplication.
        ///     用于诊断和单会话去重的稳定 Mod 标识符。
        /// </summary>
        public required string ModId { get; init; }

        /// <summary>
        ///     Display name shown in the default update toast.
        ///     默认更新 toast 中显示的名称。
        /// </summary>
        public required string DisplayName { get; init; }

        /// <summary>
        ///     Current installed version, for example <c>1.2.3</c> or <c>v1.2.3-beta.1</c>.
        ///     当前安装版本，例如 <c>1.2.3</c> 或 <c>v1.2.3-beta.1</c>。
        /// </summary>
        public required string CurrentVersion { get; init; }

        /// <summary>
        ///     Absolute URL for the small JSON update manifest. Prefer a mirror or self-hosted endpoint when broad
        ///     player reachability matters.
        ///     小型 JSON 更新 manifest 的绝对 URL。若需要照顾更广泛的玩家连接可达性，建议使用镜像或自托管端点。
        /// </summary>
        public required Uri ManifestUri { get; init; }

        /// <summary>
        ///     Fallback release page opened when the toast is clicked. The manifest can override this per release.
        ///     点击 toast 时打开的备用发布页；manifest 可为单次发布覆盖它。
        /// </summary>
        public Uri? ReleasePageUri { get; init; }

        /// <summary>
        ///     Optional request headers for mirrors or self-hosted endpoints.
        ///     用于镜像或自托管端点的可选请求头。
        /// </summary>
        public IReadOnlyDictionary<string, string>? Headers { get; init; }

        /// <summary>
        ///     Network timeout. Defaults to eight seconds.
        ///     网络超时。默认八秒。
        /// </summary>
        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(8d);

        /// <summary>
        ///     Optional toast duration override in seconds. Leave null to use the normal RitsuLib toast duration.
        ///     可选 toast 显示时长覆盖值，单位秒。留空时使用 RitsuLib 的普通 toast 时长。
        /// </summary>
        public double? ToastDurationSeconds { get; init; }

        /// <summary>
        ///     Optional title override. Leave null to use the manifest title or a default title.
        ///     可选标题覆盖。留空时使用 manifest 标题或默认标题。
        /// </summary>
        public string? ToastTitle { get; init; }

        /// <summary>
        ///     Optional body override. Leave null to use the manifest message or a default body.
        ///     可选正文覆盖。留空时使用 manifest 消息或默认正文。
        /// </summary>
        public string? ToastBody { get; init; }

        /// <summary>
        ///     Creates update-check options from string URLs for the common call path.
        ///     使用字符串 URL 创建常见更新检查选项。
        /// </summary>
        public static ModUpdateCheckOptions Create(
            string modId,
            string displayName,
            string currentVersion,
            string manifestUrl,
            string? releasePageUrl = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(manifestUrl);
            return Create(
                modId,
                displayName,
                currentVersion,
                new(manifestUrl.Trim(), UriKind.Absolute),
                string.IsNullOrWhiteSpace(releasePageUrl)
                    ? null
                    : new Uri(releasePageUrl.Trim(), UriKind.Absolute));
        }

        /// <summary>
        ///     Creates update-check options for the common call path.
        ///     创建常见更新检查选项。
        /// </summary>
        public static ModUpdateCheckOptions Create(
            string modId,
            string displayName,
            string currentVersion,
            Uri manifestUri,
            Uri? releasePageUri = null)
        {
            return new()
            {
                ModId = modId,
                DisplayName = displayName,
                CurrentVersion = currentVersion,
                ManifestUri = manifestUri,
                ReleasePageUri = releasePageUri,
            };
        }
    }

    /// <summary>
    ///     JSON shape served by a mod update manifest endpoint.
    ///     Mod 更新 manifest 端点提供的 JSON 形状。
    /// </summary>
    public sealed record ModUpdateCheckManifest
    {
        /// <summary>
        ///     Optional JSON Schema URL for editors and manifest validation tools.
        ///     可选 JSON Schema URL，用于编辑器和 manifest 校验工具。
        /// </summary>
        [JsonPropertyName("$schema")]
        public string? JsonSchema { get; init; }

        /// <summary>
        ///     Optional schema marker. When present, use <c>ritsulib.update.v1</c>.
        ///     可选 schema 标记。存在时请使用 <c>ritsulib.update.v1</c>。
        /// </summary>
        [JsonPropertyName("schema")]
        public string? Schema { get; init; }

        /// <summary>
        ///     Latest published version.
        ///     最新发布版本。
        /// </summary>
        [JsonPropertyName("latest_version")]
        public string? LatestVersion { get; init; }

        /// <summary>
        ///     Optional release page URL opened when the update toast is clicked.
        ///     点击更新 toast 时打开的可选发布页 URL。
        /// </summary>
        [JsonPropertyName("release_page_url")]
        public string? ReleasePageUrl { get; init; }

        /// <summary>
        ///     Optional fallback toast title.
        ///     可选 fallback toast 标题。
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        /// <summary>
        ///     Optional fallback toast body.
        ///     可选 fallback toast 正文。
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; init; }

        /// <summary>
        ///     Optional localized toast title/body keyed by locale code, for example <c>eng</c>, <c>zhs</c>,
        ///     <c>en</c>, or <c>zh-CN</c>.
        ///     可选本地化 toast 标题/正文，按语言代码索引，例如 <c>eng</c>、<c>zhs</c>、
        ///     <c>en</c> 或 <c>zh-CN</c>。
        /// </summary>
        [JsonPropertyName("localized")]
        public Dictionary<string, ModUpdateCheckLocalizedText>? Localized { get; init; }
    }

    /// <summary>
    ///     Localized update-check toast text.
    ///     更新检查 toast 的本地化文本。
    /// </summary>
    public sealed record ModUpdateCheckLocalizedText
    {
        /// <summary>
        ///     Optional localized toast title.
        ///     可选本地化 toast 标题。
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        /// <summary>
        ///     Optional localized toast body.
        ///     可选本地化 toast 正文。
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }

    /// <summary>
    ///     Result category for a completed update check.
    ///     已完成更新检查的结果类别。
    /// </summary>
    public enum ModUpdateCheckStatus
    {
        /// <summary>
        ///     The manifest reports a newer version than the installed version.
        ///     manifest 报告了比当前安装版本更新的版本。
        /// </summary>
        UpdateAvailable,

        /// <summary>
        ///     The installed version is current.
        ///     当前安装版本已是最新。
        /// </summary>
        UpToDate,

        /// <summary>
        ///     The check could not run because options or manifest data were invalid.
        ///     因选项或 manifest 数据无效，检查无法运行。
        /// </summary>
        InvalidData,

        /// <summary>
        ///     The endpoint could not be reached or returned an unsuccessful response.
        ///     端点无法连接或返回了非成功响应。
        /// </summary>
        RequestFailed,
    }

    /// <summary>
    ///     Completed update check result.
    ///     已完成的更新检查结果。
    /// </summary>
    public sealed record ModUpdateCheckResult(
        ModUpdateCheckStatus Status,
        string CurrentVersion,
        string? LatestVersion = null,
        Uri? ReleasePageUri = null,
        string? Title = null,
        string? Message = null
    );
}
