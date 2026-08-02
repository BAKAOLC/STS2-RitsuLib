using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Supplies the current player selection and refresh capability to a registered developer-tools page.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         向已注册的开发者工具页面提供当前玩家选择与刷新能力。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Page callbacks and controls run on Godot's main thread. The player list is an immutable snapshot taken
    ///         when the page is evaluated; request a refresh before relying on later run state.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         页面回调与控件在 Godot 主线程运行。玩家列表是页面求值时取得的不可变快照；如需依赖后续游戏状态，
    ///         应先请求刷新。
    ///     </para>
    /// </remarks>
    public sealed class RitsuDebugToolsPageContext
    {
        private readonly Action _requestRefresh;

        internal RitsuDebugToolsPageContext(
            Player? targetPlayer,
            IReadOnlyList<Player> players,
            Action requestRefresh)
        {
            TargetPlayer = targetPlayer;
            Players = players;
            _requestRefresh = requestRefresh;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the player selected by the developer-tools target control, if available.</para>
        ///     <para xml:lang="zh-CN">获取开发者工具目标控件当前选中的玩家；当前没有玩家时为 null。</para>
        /// </summary>
        public Player? TargetPlayer { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets an immutable snapshot of players currently available to the workspace.</para>
        ///     <para xml:lang="zh-CN">获取工作区当前可用玩家的不可变快照。</para>
        /// </summary>
        public IReadOnlyList<Player> Players { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Schedules a workspace refresh after the current callback or UI event completes. Repeated requests
        ///         before the deferred refresh runs are coalesced.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在当前回调或界面事件完成后安排一次工作区刷新；延迟刷新执行前的重复请求会被合并。
        ///     </para>
        /// </summary>
        public void RequestRefresh()
        {
            _requestRefresh();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines one independently owned page in RitsuLib's visual developer-tools dock.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义 RitsuLib 可视化开发者工具停靠面板中的一个独立所有权页面。
    ///     </para>
    /// </summary>
    public sealed class RitsuDebugToolsPageDefinition
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a page definition with required identity, title, and content factory.</para>
        ///     <para xml:lang="zh-CN">使用必需的标识、标题和内容工厂创建页面定义。</para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The owning manifest ID. It participates in the globally unique page identity.</para>
        ///     <para xml:lang="zh-CN">所属模组的清单 ID；它会参与组成全局唯一页面标识。</para>
        /// </param>
        /// <param name="id">
        ///     <para xml:lang="en">The stable page ID within <paramref name="modId" />.</para>
        ///     <para xml:lang="zh-CN">页面在 <paramref name="modId" /> 内的稳定 ID。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">Deferred localized or literal text used by the rail and page header.</para>
        ///     <para xml:lang="zh-CN">侧边栏与页面标题使用的延迟本地化文本或字面文本。</para>
        /// </param>
        /// <param name="contentFactory">
        ///     <para xml:lang="en">
        ///         Creates a new, unattached control whenever the page is rebuilt. Returning null, an invalid control,
        ///         or a control that already has a parent is rejected by the host.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         页面重建时创建一个新的未挂载控件。返回 null、无效控件或已有父节点的控件会被宿主拒绝。
        ///     </para>
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">Thrown when <paramref name="modId" /> or <paramref name="id" /> is blank.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="modId" /> 或 <paramref name="id" /> 为空时抛出。</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="title" /> or <paramref name="contentFactory" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="title" /> 或 <paramref name="contentFactory" /> 为 null 时抛出。</para>
        /// </exception>
        public RitsuDebugToolsPageDefinition(
            string modId,
            string id,
            ModSettingsText title,
            Func<RitsuDebugToolsPageContext, Control> contentFactory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(title);
            ArgumentNullException.ThrowIfNull(contentFactory);
            ModId = modId.Trim();
            Id = id.Trim();
            Title = title;
            ContentFactory = contentFactory;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the owning mod manifest ID.</para>
        ///     <para xml:lang="zh-CN">获取所属模组的清单 ID。</para>
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the stable page ID within the owning mod.</para>
        ///     <para xml:lang="zh-CN">获取页面在所属模组内的稳定 ID。</para>
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the globally unique ID in <c>modId:pageId</c> form.</para>
        ///     <para xml:lang="zh-CN">获取采用 <c>modId:pageId</c> 格式的全局唯一 ID。</para>
        /// </summary>
        public string QualifiedId => $"{ModId}:{Id}";

        /// <summary>
        ///     <para xml:lang="en">Gets the deferred localized or literal display title.</para>
        ///     <para xml:lang="zh-CN">获取延迟解析的本地化或字面显示标题。</para>
        /// </summary>
        public ModSettingsText Title { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the page order. Lower values appear first; values from -100,000 through 100,000 are accepted.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取页面顺序；数值较小的页面排在前面，允许范围为 -100,000 至 100,000。
        ///     </para>
        /// </summary>
        public int SortOrder { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the preferred fraction of viewport width used by the joined dock, from 0.35 through 0.90.
        ///         The host still constrains the page to the space available beside the rail. The default is 0.62.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取组合式停靠面板期望占用的视口宽度比例，范围为 0.35 至 0.90。宿主仍会将页面限制在侧边栏旁的
        ///         可用空间内；默认值为 0.62。
        ///     </para>
        /// </summary>
        public float PreferredWidthFraction { get; init; } = 0.62f;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets an optional side-effect-free icon factory. The host accepts an existing texture up to 2,048
        ///         pixels per axis; null or invalid results use the fallback page icon. The caller retains ownership
        ///         and must keep returned textures valid while the page remains registered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取可选且无副作用的图标工厂。宿主接受单边不超过 2,048 像素的现有纹理；返回 null 或无效结果时
        ///         使用页面回退图标。调用方仍持有返回纹理，并须在页面保持注册期间确保纹理有效。
        ///     </para>
        /// </summary>
        public Func<Texture2D?>? IconFactory { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets an optional visibility predicate, re-evaluated when the workspace refreshes. Exceptions hide
        ///         only this page and are logged once per page instance.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取可选可见性谓词；工作区刷新时重新求值。异常只会隐藏当前页面，并按页面实例记录一次日志。
        ///     </para>
        /// </summary>
        public Func<RitsuDebugToolsPageContext, bool>? VisibleWhen { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the factory that creates a fresh page control.</para>
        ///     <para xml:lang="zh-CN">获取用于创建全新页面控件的工厂。</para>
        /// </summary>
        public Func<RitsuDebugToolsPageContext, Control> ContentFactory { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers independently owned pages for the visual developer-tools dock without replacing built-in or
    ///         other mods' pages.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为可视化开发者工具停靠面板注册独立所有权页面，且不会替换内置页面或其他模组的页面。
    ///     </para>
    /// </summary>
    public static class RitsuDebugToolsPageRegistry
    {
        private const int MaximumPageCount = 256;
        private const int MaximumIdentityLength = 128;
        private const int MaximumSortOrder = 100_000;
        private static readonly Dictionary<string, Registration> Pages = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ReservedQualifiedIds = new(StringComparer.OrdinalIgnoreCase)
        {
            $"{Const.ModId}:cards",
            $"{Const.ModId}:pile-cards",
            $"{Const.ModId}:relics",
            $"{Const.ModId}:potions",
            $"{Const.ModId}:powers",
            $"{Const.ModId}:players",
            $"{Const.ModId}:creatures",
            $"{Const.ModId}:monsters",
            $"{Const.ModId}:rooms",
            $"{Const.ModId}:encounters",
            $"{Const.ModId}:events",
        };
        private static readonly Lock SyncRoot = new();

        internal static event Action? Changed;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a page and returns an ownership handle. Disposing the handle removes only that exact
        ///         registration. Duplicate qualified IDs are rejected rather than replaced.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册页面并返回所有权句柄。释放句柄时只移除该次注册；重复的全局 ID 会被拒绝而不会覆盖原页面。
        ///     </para>
        /// </summary>
        /// <param name="definition">
        ///     <para xml:lang="en">The immutable page definition to validate and register.</para>
        ///     <para xml:lang="zh-CN">要校验并注册的不可变页面定义。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">An idempotent handle that unregisters this page when disposed.</para>
        ///     <para xml:lang="zh-CN">释放时取消注册当前页面的幂等句柄。</para>
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">
        ///         Thrown when identity text is malformed, the qualified ID is reserved, or it already exists.
        ///     </para>
        ///     <para xml:lang="zh-CN">当标识文本格式无效、全局 ID 已保留或已存在时抛出。</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="definition" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="definition" /> 为 null 时抛出。</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">
        ///         Thrown when the sort order, preferred width, or total page count exceeds its supported bound.
        ///     </para>
        ///     <para xml:lang="zh-CN">当排序值、期望宽度或页面总数超过支持范围时抛出。</para>
        /// </exception>
        public static IDisposable Register(RitsuDebugToolsPageDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);
            ValidateIdentity(definition.ModId, nameof(definition.ModId));
            ValidateIdentity(definition.Id, nameof(definition.Id));
            if (definition.SortOrder is < -MaximumSortOrder or > MaximumSortOrder)
                throw new ArgumentOutOfRangeException(nameof(definition), definition.SortOrder,
                    $"Developer-tools page order must be between {-MaximumSortOrder} and {MaximumSortOrder}.");
            if (definition.PreferredWidthFraction is < 0.35f or > 0.9f ||
                !float.IsFinite(definition.PreferredWidthFraction))
                throw new ArgumentOutOfRangeException(nameof(definition), definition.PreferredWidthFraction,
                    "Developer-tools page width fraction must be between 0.35 and 0.90.");
            if (ReservedQualifiedIds.Contains(definition.QualifiedId))
                throw new ArgumentException(
                    $"Developer-tools page '{definition.QualifiedId}' is reserved by RitsuLib.",
                    nameof(definition));

            var registration = new Registration(definition);
            lock (SyncRoot)
            {
                if (Pages.ContainsKey(definition.QualifiedId))
                    throw new ArgumentException(
                        $"Developer-tools page '{definition.QualifiedId}' is already registered.",
                        nameof(definition));
                if (Pages.Count >= MaximumPageCount)
                    throw new ArgumentOutOfRangeException(nameof(definition), Pages.Count,
                        $"No more than {MaximumPageCount} developer-tools pages may be registered.");
                Pages.Add(definition.QualifiedId, registration);
            }

            NotifyChanged();
            return registration;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns an immutable, deterministically ordered snapshot of registered pages.</para>
        ///     <para xml:lang="zh-CN">返回已注册页面的不可变确定顺序快照。</para>
        /// </summary>
        public static IReadOnlyList<RitsuDebugToolsPageDefinition> GetPages()
        {
            lock (SyncRoot)
            {
                return Array.AsReadOnly(Pages.Values
                    .Select(static registration => registration.Definition)
                    .OrderBy(static definition => definition.SortOrder)
                    .ThenBy(static definition => definition.ModId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static definition => definition.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray());
            }
        }

        private static void ValidateIdentity(string value, string paramName)
        {
            if (value.Length > MaximumIdentityLength)
                throw new ArgumentException(
                    $"Developer-tools identity components cannot exceed {MaximumIdentityLength} characters.",
                    paramName);
            if (value.Any(static character =>
                    !(char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-')))
                throw new ArgumentException(
                    "Developer-tools identity components may contain only ASCII letters, digits, '.', '_', and '-'.",
                    paramName);
        }

        private static void NotifyChanged()
        {
            try
            {
                Changed?.Invoke();
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[DebugToolsUi] Page-registry refresh callback failed: {ex}");
            }
        }

        private sealed class Registration(RitsuDebugToolsPageDefinition definition) : IDisposable
        {
            private int _disposed;

            internal RitsuDebugToolsPageDefinition Definition { get; } = definition;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;

                var changed = false;
                lock (SyncRoot)
                {
                    if (Pages.TryGetValue(Definition.QualifiedId, out var current) && ReferenceEquals(current, this))
                        changed = Pages.Remove(Definition.QualifiedId);
                }

                if (changed)
                    NotifyChanged();
            }
        }
    }
}
