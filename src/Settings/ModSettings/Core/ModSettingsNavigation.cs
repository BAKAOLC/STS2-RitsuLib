using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Identifies a stable location in the RitsuLib mod settings UI, from a mod down to an optional entry.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         标识 RitsuLib 模组设置界面中从模组到可选条目的稳定位置。
    ///     </para>
    /// </summary>
    /// <param name="ModId">
    ///     <para xml:lang="en">The target mod ID.</para>
    ///     <para xml:lang="zh-CN">目标模组 ID。</para>
    /// </param>
    /// <param name="PageId">
    ///     <para xml:lang="en">The optional target page ID.</para>
    ///     <para xml:lang="zh-CN">可选的目标页面 ID。</para>
    /// </param>
    /// <param name="SectionId">
    ///     <para xml:lang="en">The optional target section ID.</para>
    ///     <para xml:lang="zh-CN">可选的目标节 ID。</para>
    /// </param>
    /// <param name="EntryId">
    ///     <para xml:lang="en">The optional target entry ID.</para>
    ///     <para xml:lang="zh-CN">可选的目标条目 ID。</para>
    /// </param>
    public sealed record ModSettingsLocation(
        string ModId,
        string? PageId = null,
        string? SectionId = null,
        string? EntryId = null);

    /// <summary>
    ///     <para xml:lang="en">Configures presentation behavior when opening a settings location.</para>
    ///     <para xml:lang="zh-CN">配置打开设置位置时的呈现行为。</para>
    /// </summary>
    public sealed class ModSettingsOpenOptions
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes whether to briefly pulse-highlight the target section or entry after navigation.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化导航后是否短暂脉冲高亮目标节或条目。</para>
        /// </summary>
        public bool Highlight { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes whether to move UI focus into the target area when possible.</para>
        ///     <para xml:lang="zh-CN">获取或初始化是否在可行时将界面焦点移入目标区域。</para>
        /// </summary>
        public bool Focus { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes whether to expand a collapsed target section before scrolling to an entry inside
        ///         it.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或初始化滚动到折叠目标节中的条目前是否先展开该节。
        ///     </para>
        /// </summary>
        public bool ExpandCollapsedSection { get; init; } = true;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes the acceptance, resolution, or completion of a mod settings navigation request.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述模组设置导航请求的接受、解析或完成结果。
    ///     </para>
    /// </summary>
    public sealed class ModSettingsOpenResult
    {
        /// <summary>
        ///     <para xml:lang="en">Gets whether the reported navigation stage succeeded.</para>
        ///     <para xml:lang="zh-CN">获取所报告的导航阶段是否成功。</para>
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the stable machine-readable result code.</para>
        ///     <para xml:lang="zh-CN">获取稳定的机器可读结果代码。</para>
        /// </summary>
        public string Code { get; init; } = "";

        /// <summary>
        ///     <para xml:lang="en">Gets a human-readable result message.</para>
        ///     <para xml:lang="zh-CN">获取便于阅读的结果消息。</para>
        /// </summary>
        public string Message { get; init; } = "";

        /// <summary>
        ///     <para xml:lang="en">Gets the target mod ID reported for this stage.</para>
        ///     <para xml:lang="zh-CN">获取此阶段所报告的目标模组 ID。</para>
        /// </summary>
        public string ModId { get; init; } = "";

        /// <summary>
        ///     <para xml:lang="en">Gets the reported target page ID, when present.</para>
        ///     <para xml:lang="zh-CN">获取所报告的目标页面 ID（如果存在）。</para>
        /// </summary>
        public string? PageId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the reported target section ID, when present.</para>
        ///     <para xml:lang="zh-CN">获取所报告的目标节 ID（如果存在）。</para>
        /// </summary>
        public string? SectionId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the reported target entry ID, when present.</para>
        ///     <para xml:lang="zh-CN">获取所报告的目标条目 ID（如果存在）。</para>
        /// </summary>
        public string? EntryId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether navigation was queued for later completion.</para>
        ///     <para xml:lang="zh-CN">获取导航是否已排队等待稍后完成。</para>
        /// </summary>
        public bool IsDeferred { get; init; }

        internal static ModSettingsOpenResult Ok(
            string code,
            string message,
            ModSettingsLocation location,
            bool isDeferred = false)
        {
            return new()
            {
                Success = true,
                Code = code,
                Message = message,
                ModId = location.ModId,
                PageId = location.PageId,
                SectionId = location.SectionId,
                EntryId = location.EntryId,
                IsDeferred = isDeferred,
            };
        }

        internal static ModSettingsOpenResult Error(string code, string message, ModSettingsLocation location)
        {
            return new()
            {
                Success = false,
                Code = code,
                Message = message,
                ModId = location.ModId,
                PageId = location.PageId,
                SectionId = location.SectionId,
                EntryId = location.EntryId,
            };
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides public entry points for opening RitsuLib mod settings locations from mods, reflection, or
    ///         console commands.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供从模组、反射调用或控制台命令打开 RitsuLib 模组设置位置的公共入口。
    ///     </para>
    /// </summary>
    public static class ModSettingsNavigator
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves a location supplied as individual IDs, opens an available settings host, and queues the
        ///         visible navigation for a later frame. Pass <see langword="null" /> for unspecified IDs.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析以各个 ID 提供的位置，打开可用的设置宿主，并将可见界面的导航排到后续帧执行。
        ///         未指定的 ID 应传入 <see langword="null" />。
        ///     </para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The target mod ID.</para>
        ///     <para xml:lang="zh-CN">目标模组 ID。</para>
        /// </param>
        /// <param name="pageId">
        ///     <para xml:lang="en">The optional target page ID.</para>
        ///     <para xml:lang="zh-CN">可选的目标页面 ID。</para>
        /// </param>
        /// <param name="sectionId">
        ///     <para xml:lang="en">The optional target section ID.</para>
        ///     <para xml:lang="zh-CN">可选的目标节 ID。</para>
        /// </param>
        /// <param name="entryId">
        ///     <para xml:lang="en">The optional target entry ID.</para>
        ///     <para xml:lang="zh-CN">可选的目标条目 ID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         A deferred success result when the location is resolved and queued; otherwise, an input, registry,
        ///         resolution, or host error. Deferred execution failures are logged and are not reflected in this
        ///         immediate result.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         位置成功解析并排队时返回延迟成功结果；否则返回输入、注册表、位置解析或宿主错误。
        ///         延迟执行期间的失败只会被记录，不会反映在此即时结果中。
        ///     </para>
        /// </returns>
        public static ModSettingsOpenResult RequestOpenByIds(
            string modId,
            string? pageId,
            string? sectionId,
            string? entryId)
        {
            var requested = Normalize(new(modId, pageId, sectionId, entryId));
            var resolved = ResolveLocation(requested);
            if (!resolved.Success)
                return resolved;

            var location = ToResolvedLocation(resolved);
            if (!TryOpenHost(out var submenu, out var hostError))
                return ModSettingsOpenResult.Error("no-settings-host", hostError, location);

            Callable.From(() => { _ = RunDeferredOpenAsync(submenu, location); }).CallDeferred();

            return ModSettingsOpenResult.Ok(
                "requested",
                $"Requested to open settings location '{FormatLocation(location)}'.",
                location,
                true);
        }

        private static async Task RunDeferredOpenAsync(RitsuModSettingsSubmenu submenu, ModSettingsLocation location)
        {
            try
            {
                if (!GodotObject.IsInstanceValid(submenu))
                    return;

                await submenu.OpenToAsync(location, new());
            }
            catch (OperationCanceledException)
            {
                // UI lifetime ended while the deferred navigation was waiting for layout.
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Settings] Deferred navigation failed: {ex}");
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Opens a settings location supplied as individual IDs and waits for the visible UI to finish navigating
        ///         to it.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         打开以各个 ID 提供的设置位置，并等待可见界面完成导航。
        ///     </para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The target mod ID.</para>
        ///     <para xml:lang="zh-CN">目标模组 ID。</para>
        /// </param>
        /// <param name="pageId">
        ///     <para xml:lang="en">The optional target page ID.</para>
        ///     <para xml:lang="zh-CN">可选的目标页面 ID。</para>
        /// </param>
        /// <param name="sectionId">
        ///     <para xml:lang="en">The optional target section ID.</para>
        ///     <para xml:lang="zh-CN">可选的目标节 ID。</para>
        /// </param>
        /// <param name="entryId">
        ///     <para xml:lang="en">The optional target entry ID.</para>
        ///     <para xml:lang="zh-CN">可选的目标条目 ID。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">Optional navigation presentation behavior.</para>
        ///     <para xml:lang="zh-CN">可选的导航呈现行为。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A task whose result describes resolution, host opening, and visible navigation.</para>
        ///     <para xml:lang="zh-CN">其结果描述位置解析、宿主打开与可见界面导航的任务。</para>
        /// </returns>
        public static Task<ModSettingsOpenResult> OpenByIdsAsync(
            string modId,
            string? pageId = null,
            string? sectionId = null,
            string? entryId = null,
            ModSettingsOpenOptions? options = null)
        {
            return OpenAsync(new(modId, pageId, sectionId, entryId), options);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves and opens a settings location, then waits for the visible UI to finish navigating to it.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析并打开设置位置，然后等待可见界面完成导航。
        ///     </para>
        /// </summary>
        /// <param name="location">
        ///     <para xml:lang="en">The settings location to open.</para>
        ///     <para xml:lang="zh-CN">要打开的设置位置。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">Optional navigation presentation behavior.</para>
        ///     <para xml:lang="zh-CN">可选的导航呈现行为。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A task whose result describes resolution, host opening, and visible navigation.</para>
        ///     <para xml:lang="zh-CN">其结果描述位置解析、宿主打开与可见界面导航的任务。</para>
        /// </returns>
        public static async Task<ModSettingsOpenResult> OpenAsync(
            ModSettingsLocation location,
            ModSettingsOpenOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(location);

            var requested = Normalize(location);
            var resolved = ResolveLocation(requested);
            if (!resolved.Success)
                return resolved;

            var target = ToResolvedLocation(resolved);
            if (!TryOpenHost(out var submenu, out var hostError))
                return ModSettingsOpenResult.Error("no-settings-host", hostError, target);

            return await submenu.OpenToAsync(target, options ?? new());
        }

        internal static ModSettingsOpenResult ResolveLocation(ModSettingsLocation requested)
        {
            if (string.IsNullOrWhiteSpace(requested.ModId))
                return ModSettingsOpenResult.Error("invalid-location", "A mod id is required.", requested);
            if (string.IsNullOrWhiteSpace(requested.PageId) &&
                (!string.IsNullOrWhiteSpace(requested.SectionId) || !string.IsNullOrWhiteSpace(requested.EntryId)))
                return ModSettingsOpenResult.Error(
                    "invalid-location",
                    "A page id is required when opening a section or entry.",
                    requested);

            try
            {
                RitsuLibModSettingsBootstrap.EnsureFrameworkPagesRegistered();
                ModSettingsMirrorRegistrarBootstrap.TryRegisterMirroredPages();
                RitsuLibModSettingsBootstrap.RefreshDynamicPages();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Settings] Failed to refresh page registry before navigation: {ex}");
                return ModSettingsOpenResult.Error(
                    "registry-refresh-failed",
                    "Settings pages could not be refreshed.",
                    requested);
            }

            var pages = ModSettingsRegistry.GetPages()
                .Where(page => string.Equals(page.ModId, requested.ModId, StringComparison.OrdinalIgnoreCase))
                .Where(IsPageCurrentlyVisible)
                .ToArray();
            if (pages.Length == 0)
                return ModSettingsOpenResult.Error(
                    "mod-not-found",
                    $"No visible settings pages were found for mod '{requested.ModId}'.",
                    requested);

            ModSettingsPage? page;
            if (string.IsNullOrWhiteSpace(requested.PageId))
            {
                page = pages
                    .Where(p => string.IsNullOrWhiteSpace(p.ParentPageId))
                    .OrderBy(ModSettingsRegistry.GetEffectivePageSortOrder)
                    .ThenBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault() ?? pages[0];
            }
            else
            {
                page = pages.FirstOrDefault(p => string.Equals(p.Id, requested.PageId,
                    StringComparison.OrdinalIgnoreCase));
                if (page == null)
                    return ModSettingsOpenResult.Error(
                        "page-not-found",
                        $"Settings page '{requested.ModId}:{requested.PageId}' was not found or is hidden here.",
                        requested);
            }

            ModSettingsSection? section = null;
            if (!string.IsNullOrWhiteSpace(requested.SectionId))
            {
                section = page.Sections.FirstOrDefault(s => string.Equals(s.Id, requested.SectionId,
                    StringComparison.OrdinalIgnoreCase));
                if (section == null || !IsSectionCurrentlyVisible(page, section))
                    return ModSettingsOpenResult.Error(
                        "section-not-found",
                        $"Settings section '{requested.SectionId}' was not found or is hidden.",
                        requested with { PageId = page.Id });
            }

            if (!string.IsNullOrWhiteSpace(requested.EntryId))
            {
                var candidateSections = section == null ? page.Sections : [section];
                var matches = candidateSections
                    .Where(s => IsSectionCurrentlyVisible(page, s))
                    .SelectMany(s => s.Entries
                        .Where(e => string.Equals(e.Id, requested.EntryId, StringComparison.OrdinalIgnoreCase))
                        .Select(e => (Section: s, Entry: e)))
                    .ToArray();

                switch (matches.Length)
                {
                    case 0:
                        return ModSettingsOpenResult.Error(
                            "entry-not-found",
                            $"Settings entry '{requested.EntryId}' was not found.",
                            requested with { PageId = page.Id, SectionId = section?.Id });
                    case > 1:
                        return ModSettingsOpenResult.Error(
                            "entry-ambiguous",
                            $"Settings entry '{requested.EntryId}' exists in multiple sections; pass a section id.",
                            requested with { PageId = page.Id });
                }

                section = matches[0].Section;
                if (!IsEntryCurrentlyVisible(page, matches[0].Entry))
                    return ModSettingsOpenResult.Error(
                        "entry-hidden",
                        $"Settings entry '{requested.EntryId}' is currently hidden.",
                        requested with { PageId = page.Id, SectionId = section.Id });
            }

            var resolved = requested with
            {
                PageId = page.Id,
                SectionId = section?.Id ?? requested.SectionId,
            };
            return ModSettingsOpenResult.Ok("resolved", $"Resolved settings location '{FormatLocation(resolved)}'.",
                resolved);
        }

        internal static string FormatLocation(ModSettingsLocation location)
        {
            return string.Join("/",
                new[] { location.ModId, location.PageId, location.SectionId, location.EntryId }
                    .Where(static part => !string.IsNullOrWhiteSpace(part)));
        }

        private static ModSettingsLocation Normalize(ModSettingsLocation location)
        {
            return new(
                location.ModId?.Trim() ?? "",
                NormalizeOptional(location.PageId),
                NormalizeOptional(location.SectionId),
                NormalizeOptional(location.EntryId));
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static ModSettingsLocation ToResolvedLocation(ModSettingsOpenResult result)
        {
            return new(result.ModId, result.PageId, result.SectionId, result.EntryId);
        }

        private static bool TryOpenHost(out RitsuModSettingsSubmenu submenu, out string error)
        {
            if (Engine.GetMainLoop() is SceneTree { Root: { } root })
            {
                var visible = FindVisibleSubmenu(root);
                if (visible != null)
                {
                    submenu = visible;
                    error = "";
                    return true;
                }
            }

            var game = NGame.Instance;
            if (game?.MainMenu?.SubmenuStack is { } mainMenuStack)
            {
                submenu = mainMenuStack.PushSubmenuType<RitsuModSettingsSubmenu>();
                error = "";
                return true;
            }

            if (game?.CurrentRunNode?.GlobalUi?.SubmenuStack is { } runCapstoneStack)
            {
                runCapstoneStack.ShowScreen(CapstoneSubmenuType.Settings);
                submenu = runCapstoneStack.Stack.PushSubmenuType<RitsuModSettingsSubmenu>();
                error = "";
                return true;
            }

            submenu = null!;
            error = "No active main-menu or run settings host is available.";
            return false;
        }

        private static RitsuModSettingsSubmenu? FindVisibleSubmenu(Node root)
        {
            var queue = new Queue<Node>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node is RitsuModSettingsSubmenu { Visible: true } submenu && submenu.IsInsideTree())
                    return submenu;

                foreach (var child in node.GetChildren())
                    queue.Enqueue(child);
            }

            return null;
        }

        private static bool IsPageCurrentlyVisible(ModSettingsPage page)
        {
            return ModSettingsVisibility.IsPageVisible(page);
        }

        private static bool IsSectionCurrentlyVisible(ModSettingsPage page, ModSettingsSection section)
        {
            return ModSettingsVisibility.IsSectionVisible(page, section);
        }

        private static bool IsEntryCurrentlyVisible(ModSettingsPage page, ModSettingsEntryDefinition entry)
        {
            return ModSettingsVisibility.IsEntryVisible(page, entry);
        }
    }
}
