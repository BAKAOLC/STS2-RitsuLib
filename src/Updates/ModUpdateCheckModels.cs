using System.Reflection;
using System.Text.Json.Serialization;

namespace STS2RitsuLib.Updates
{
    /// <summary>
    ///     <para xml:lang="en">Configures a non-blocking mod update check.</para>
    ///     <para xml:lang="zh-CN">配置非阻塞的模组更新检查。</para>
    /// </summary>
    public sealed record ModUpdateCheckOptions
    {
        /// <summary>
        ///     <para xml:lang="en">Stable mod ID used for diagnostics and one-check-per-session de-duplication.</para>
        ///     <para xml:lang="zh-CN">用于诊断和每会话单次检查去重的稳定模组 ID。</para>
        /// </summary>
        public required string ModId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Display name shown in the default update notification.</para>
        ///     <para xml:lang="zh-CN">默认更新通知中显示的名称。</para>
        /// </summary>
        public required string DisplayName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Currently installed version, for example <c>1.2.3</c> or <c>v1.2.3-beta.1</c>.</para>
        ///     <para xml:lang="zh-CN">当前安装的版本，例如 <c>1.2.3</c> 或 <c>v1.2.3-beta.1</c>。</para>
        /// </summary>
        public required string CurrentVersion { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Absolute URL of the compact JSON update manifest. Use a mirror or self-hosted endpoint when
        ///         broad player reachability matters.
        ///     </para>
        ///     <para xml:lang="zh-CN">精简 JSON 更新清单的绝对 URL。需要覆盖更广泛的玩家网络环境时，应使用镜像或自托管端点。</para>
        /// </summary>
        public required Uri ManifestUri { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Fallback release page opened when the update notification is selected; the manifest can
        ///         override it per release.
        ///     </para>
        ///     <para xml:lang="zh-CN">选中更新通知时打开的回退发布页；清单可按发布版本覆盖此页。</para>
        /// </summary>
        public Uri? ReleasePageUri { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Optional request headers for mirrors or self-hosted endpoints.</para>
        ///     <para xml:lang="zh-CN">镜像或自托管端点使用的可选请求头。</para>
        /// </summary>
        public IReadOnlyDictionary<string, string>? Headers { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Network timeout; defaults to eight seconds.</para>
        ///     <para xml:lang="zh-CN">网络超时时间；默认八秒。</para>
        /// </summary>
        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(8d);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional notification duration override in seconds. Leave null to use RitsuLib's default
        ///         duration.
        ///     </para>
        ///     <para xml:lang="zh-CN">可选通知显示时长覆盖值，单位秒；为 null 时使用 RitsuLib 默认时长。</para>
        /// </summary>
        public double? ToastDurationSeconds { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Optional title override. Leave null to use the manifest title or the default title.</para>
        ///     <para xml:lang="zh-CN">可选标题覆盖值；为 null 时使用清单标题或默认标题。</para>
        /// </summary>
        public string? ToastTitle { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Optional body override. Leave null to use the manifest message or the default body.</para>
        ///     <para xml:lang="zh-CN">可选正文覆盖值；为 null 时使用清单消息或默认正文。</para>
        /// </summary>
        public string? ToastBody { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         When true, skips the external manifest check when the configured installation source came from
        ///         Steam Workshop.
        ///     </para>
        ///     <para xml:lang="zh-CN">为 true 时，若配置的安装来源来自 Steam Workshop，则跳过外部清单检查。</para>
        /// </summary>
        public bool SkipWhenLoadedFromSteamWorkshop { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Steam Workshop item ID that owns this update check. When set, Workshop skipping applies only
        ///         when the installation source path belongs to this item.
        ///     </para>
        ///     <para xml:lang="zh-CN">拥有此更新检查的 Steam Workshop 条目 ID。设置后，仅当安装来源路径属于该条目时才跳过外部检查。</para>
        /// </summary>
        public ulong? SteamWorkshopItemId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Optional assembly used to determine whether this mod was loaded from Steam Workshop.</para>
        ///     <para xml:lang="zh-CN">用于确定此模组是否从 Steam Workshop 加载的可选程序集。</para>
        /// </summary>
        public Assembly? InstallSourceAssembly { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional path used to determine whether this mod was loaded from Steam Workshop. When set, it
        ///         takes precedence over <see cref="InstallSourceAssembly" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">用于确定此模组是否从 Steam Workshop 加载的可选路径。设置后优先于 <see cref="InstallSourceAssembly" />。</para>
        /// </summary>
        public string? InstallSourcePath { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Creates update-check options from string URLs for the common call path.</para>
        ///     <para xml:lang="zh-CN">从字符串 URL 创建常用调用路径所需的更新检查选项。</para>
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
        ///     <para xml:lang="en">Creates update-check options for the common call path.</para>
        ///     <para xml:lang="zh-CN">创建常用调用路径所需的更新检查选项。</para>
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
    ///     <para xml:lang="en">Describes the JSON payload served by a mod update-manifest endpoint.</para>
    ///     <para xml:lang="zh-CN">描述模组更新清单端点提供的 JSON 负载。</para>
    /// </summary>
    public sealed record ModUpdateCheckManifest
    {
        /// <summary>
        ///     <para xml:lang="en">Optional JSON Schema URL for editors and manifest validation tools.</para>
        ///     <para xml:lang="zh-CN">供编辑器和清单校验工具使用的可选 JSON Schema URL。</para>
        /// </summary>
        [JsonPropertyName("$schema")]
        public string? JsonSchema { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Optional schema marker. When present, it must be <c>ritsulib.update.v1</c>.</para>
        ///     <para xml:lang="zh-CN">可选架构标记；存在时必须为 <c>ritsulib.update.v1</c>。</para>
        /// </summary>
        [JsonPropertyName("schema")]
        public string? Schema { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Latest published version.</para>
        ///     <para xml:lang="zh-CN">最新发布的版本。</para>
        /// </summary>
        [JsonPropertyName("latest_version")]
        public string? LatestVersion { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Optional release page URL opened when the update notification is selected.</para>
        ///     <para xml:lang="zh-CN">选中更新通知时打开的可选发布页 URL。</para>
        /// </summary>
        [JsonPropertyName("release_page_url")]
        public string? ReleasePageUrl { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Optional fallback notification title.</para>
        ///     <para xml:lang="zh-CN">可选的回退通知标题。</para>
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Optional fallback notification body.</para>
        ///     <para xml:lang="zh-CN">可选的回退通知正文。</para>
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional localized notification title and body keyed by locale code, for example <c>eng</c>,
        ///         <c>zhs</c>, <c>en</c>, or <c>zh-CN</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">按语言代码索引的可选本地化通知标题和正文，例如 <c>eng</c>、<c>zhs</c>、<c>en</c> 或 <c>zh-CN</c>。</para>
        /// </summary>
        [JsonPropertyName("localized")]
        public Dictionary<string, ModUpdateCheckLocalizedText>? Localized { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Provides localized update-notification title and message text.</para>
    ///     <para xml:lang="zh-CN">提供本地化的更新通知标题和消息文本。</para>
    /// </summary>
    public sealed record ModUpdateCheckLocalizedText
    {
        /// <summary>
        ///     <para xml:lang="en">Optional localized notification title.</para>
        ///     <para xml:lang="zh-CN">可选的本地化通知标题。</para>
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Optional localized notification body.</para>
        ///     <para xml:lang="zh-CN">可选的本地化通知正文。</para>
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Identifies the outcome category of a completed update check.</para>
    ///     <para xml:lang="zh-CN">标识已完成更新检查的结果类别。</para>
    /// </summary>
    public enum ModUpdateCheckStatus
    {
        /// <summary>
        ///     <para xml:lang="en">The manifest reports a version newer than the installed version.</para>
        ///     <para xml:lang="zh-CN">清单报告的版本比当前安装版本更新。</para>
        /// </summary>
        UpdateAvailable,

        /// <summary>
        ///     <para xml:lang="en">The installed version is current.</para>
        ///     <para xml:lang="zh-CN">当前安装的版本已是最新。</para>
        /// </summary>
        UpToDate,

        /// <summary>
        ///     <para xml:lang="en">The check could not run because the options or manifest data were invalid.</para>
        ///     <para xml:lang="zh-CN">因选项或清单数据无效，无法执行检查。</para>
        /// </summary>
        InvalidData,

        /// <summary>
        ///     <para xml:lang="en">The endpoint could not be reached or returned an unsuccessful response.</para>
        ///     <para xml:lang="zh-CN">无法访问端点，或端点返回了失败响应。</para>
        /// </summary>
        RequestFailed,

        /// <summary>
        ///     <para xml:lang="en">
        ///         The check was intentionally skipped, for example because Steam Workshop manages the installed
        ///         copy.
        ///     </para>
        ///     <para xml:lang="zh-CN">检查被有意跳过，例如 Steam Workshop 正在管理当前安装副本。</para>
        /// </summary>
        Skipped,
    }

    /// <summary>
    ///     <para xml:lang="en">Contains the outcome and optional release details of a completed update check.</para>
    ///     <para xml:lang="zh-CN">包含已完成更新检查的结果及可选发布详情。</para>
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
