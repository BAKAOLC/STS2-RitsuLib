using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Stable location inside the RitsuLib mod settings UI.
    ///     RitsuLib Mod 设置 UI 内的稳定位置。
    /// </summary>
    public sealed record ModSettingsLocation(
        string ModId,
        string? PageId = null,
        string? SectionId = null,
        string? EntryId = null);

    /// <summary>
    ///     Optional behavior for opening a settings location.
    ///     打开设置位置时的可选行为。
    /// </summary>
    public sealed class ModSettingsOpenOptions
    {
        /// <summary>
        ///     Briefly pulse the target section or entry after navigation.
        ///     跳转后短暂高亮目标 section 或条目。
        /// </summary>
        public bool Highlight { get; init; } = true;

        /// <summary>
        ///     Move UI focus into the target area when possible.
        ///     可行时将 UI 焦点移入目标区域。
        /// </summary>
        public bool Focus { get; init; } = true;

        /// <summary>
        ///     Expand a collapsible target section before scrolling to an entry inside it.
        ///     滚动到折叠 section 内的条目前，先展开该 section。
        /// </summary>
        public bool ExpandCollapsedSection { get; init; } = true;
    }

    /// <summary>
    ///     Result returned by mod settings navigation requests.
    ///     Mod 设置导航请求返回的结果。
    /// </summary>
    public sealed class ModSettingsOpenResult
    {
        /// <summary>
        ///     True when the request was accepted or completed.
        ///     请求已接受或已完成时为 true。
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        ///     Stable machine-readable result code.
        ///     稳定的机器可读结果代码。
        /// </summary>
        public string Code { get; init; } = "";

        /// <summary>
        ///     Human-readable result message.
        ///     面向人的结果消息。
        /// </summary>
        public string Message { get; init; } = "";

        /// <summary>
        ///     Target mod id.
        ///     目标 Mod id。
        /// </summary>
        public string ModId { get; init; } = "";

        /// <summary>
        ///     Resolved target page id, when any.
        ///     解析后的目标页面 id（如果有）。
        /// </summary>
        public string? PageId { get; init; }

        /// <summary>
        ///     Resolved target section id, when any.
        ///     解析后的目标 section id（如果有）。
        /// </summary>
        public string? SectionId { get; init; }

        /// <summary>
        ///     Target entry id, when any.
        ///     目标条目 id（如果有）。
        /// </summary>
        public string? EntryId { get; init; }

        /// <summary>
        ///     True when the navigation has been queued and will finish later.
        ///     导航已排队、稍后才会完成时为 true。
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
    ///     Public entry points for opening RitsuLib mod settings pages from mods, reflection, or console commands.
    ///     从 mod、反射或控制台命令打开 RitsuLib Mod 设置页面的公共入口。
    /// </summary>
    public static class ModSettingsNavigator
    {
        /// <summary>
        ///     Reflection-friendly request entry point. Pass <see langword="null" /> for unspecified ids.
        ///     反射友好的请求入口；未指定的 id 传 <see langword="null" />。
        /// </summary>
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

            Callable.From(async () =>
            {
                try
                {
                    await submenu.OpenToAsync(location, new());
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn($"[Settings] Deferred navigation failed: {ex.Message}");
                }
            }).CallDeferred();

            return ModSettingsOpenResult.Ok(
                "requested",
                $"Requested to open settings location '{FormatLocation(location)}'.",
                location,
                true);
        }

        /// <summary>
        ///     Opens a settings location and waits until the visible UI has navigated to it.
        ///     打开设置位置，并等待可见 UI 完成导航。
        /// </summary>
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
        ///     Opens a settings location and waits until the visible UI has navigated to it.
        ///     打开设置位置，并等待可见 UI 完成导航。
        /// </summary>
        public static async Task<ModSettingsOpenResult> OpenAsync(
            ModSettingsLocation location,
            ModSettingsOpenOptions? options = null)
        {
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
            try
            {
                RitsuLibModSettingsBootstrap.EnsureFrameworkPagesRegistered();
                ModSettingsMirrorRegistrarBootstrap.TryRegisterMirroredPages();
                RitsuLibModSettingsBootstrap.RefreshDynamicPages();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Settings] Failed to refresh page registry before navigation: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(requested.ModId))
                return ModSettingsOpenResult.Error("invalid-location", "A mod id is required.", requested);
            if (string.IsNullOrWhiteSpace(requested.PageId) &&
                (!string.IsNullOrWhiteSpace(requested.SectionId) || !string.IsNullOrWhiteSpace(requested.EntryId)))
                return ModSettingsOpenResult.Error(
                    "invalid-location",
                    "A page id is required when opening a section or entry.",
                    requested);

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
                if (section == null || !IsSectionCurrentlyVisible(section))
                    return ModSettingsOpenResult.Error(
                        "section-not-found",
                        $"Settings section '{requested.SectionId}' was not found or is hidden.",
                        requested with { PageId = page.Id });
            }

            if (!string.IsNullOrWhiteSpace(requested.EntryId))
            {
                var candidateSections = section == null ? page.Sections : [section];
                var matches = candidateSections
                    .Where(IsSectionCurrentlyVisible)
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
                if (!IsEntryCurrentlyVisible(matches[0].Entry))
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
            return ModSettingsHostSurfaceResolver.IsVisibleOnCurrentHost(page.VisibleOnHostSurfaces) &&
                   SafePredicate(page.VisibleWhen);
        }

        private static bool IsSectionCurrentlyVisible(ModSettingsSection section)
        {
            return ModSettingsHostSurfaceResolver.IsVisibleOnCurrentHost(section.VisibleOnHostSurfaces) &&
                   SafePredicate(section.VisibleWhen);
        }

        private static bool IsEntryCurrentlyVisible(ModSettingsEntryDefinition entry)
        {
            return SafePredicate(entry.VisibilityPredicate);
        }

        private static bool SafePredicate(Func<bool>? predicate)
        {
            if (predicate == null)
                return true;

            try
            {
                return predicate();
            }
            catch
            {
                return true;
            }
        }
    }
}
