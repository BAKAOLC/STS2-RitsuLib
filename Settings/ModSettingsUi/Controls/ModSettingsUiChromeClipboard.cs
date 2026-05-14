using System.Text.Json;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Raised before page copy; when <see cref="SuppressDefaultClipboardWrite" /> is true, the default JSON envelope is
    ///     not written.
    ///     页面复制前触发；当 <see cref="SuppressDefaultClipboardWrite" /> 为 true 时，默认 JSON 信封不会写入。
    /// </summary>
    public sealed class ModSettingsPageCopyEventArgs(ModSettingsPageUiContext context) : EventArgs
    {
        /// <summary>
        ///     Page context being copied.
        ///     正在复制的页面上下文。
        /// </summary>
        public ModSettingsPageUiContext Context { get; } = context;

        /// <summary>
        ///     When true, <see cref="ModSettingsUiChromeClipboard" /> skips writing the default envelope.
        ///     为 true 时，<see cref="ModSettingsUiChromeClipboard" /> 跳过默认信封写入。
        /// </summary>
        public bool SuppressDefaultClipboardWrite { get; set; }
    }

    /// <summary>
    ///     Page paste: subscribers run first; if none handle, default applies binding values from
    ///     <see cref="ModSettingsPageDataClipboardPayload" />.
    ///     页面粘贴：先运行订阅者；如果无人处理，则默认从
    ///     <see cref="ModSettingsPageDataClipboardPayload" /> 应用 binding 值。
    /// </summary>
    public sealed class ModSettingsPagePasteEventArgs(
        ModSettingsPageUiContext target,
        ModSettingsPageDataClipboardPayload? payload)
        : EventArgs
    {
        /// <summary>
        ///     Page receiving the paste.
        ///     接收粘贴的页面。
        /// </summary>
        public ModSettingsPageUiContext Target { get; } = target;

        /// <summary>
        ///     Deserialized page payload from the clipboard, when valid.
        ///     剪贴板中反序列化出的页面载荷，前提是有效。
        /// </summary>
        public ModSettingsPageDataClipboardPayload? Payload { get; } = payload;

        /// <summary>
        ///     When true, this paste was consumed and later subscribers should not run.
        ///     为 true 时，此次粘贴已被消费，后续订阅者不应运行。
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Outcome after handling (whether paste applied successfully).
        ///     处理后的结果（粘贴是否成功应用）。
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    ///     Raised before section copy.
    ///     section 复制前触发。
    /// </summary>
    public sealed class ModSettingsSectionCopyEventArgs(ModSettingsSectionUiContext context) : EventArgs
    {
        /// <summary>
        ///     Section context being copied.
        ///     正在复制的 section 上下文。
        /// </summary>
        public ModSettingsSectionUiContext Context { get; } = context;

        /// <summary>
        ///     When true, default envelope write is skipped.
        ///     为 true 时，跳过默认信封写入。
        /// </summary>
        public bool SuppressDefaultClipboardWrite { get; set; }
    }

    /// <summary>
    ///     Section paste: subscribers first, then default applies binding snapshots by entry id.
    ///     section 粘贴：先运行订阅者，然后默认按条目 id 应用 binding 快照。
    /// </summary>
    public sealed class ModSettingsSectionPasteEventArgs(
        ModSettingsSectionUiContext target,
        ModSettingsSectionDataClipboardPayload? payload)
        : EventArgs
    {
        /// <summary>
        ///     Section receiving the paste.
        ///     接收粘贴的 section。
        /// </summary>
        public ModSettingsSectionUiContext Target { get; } = target;

        /// <summary>
        ///     Deserialized section payload when the clipboard is valid.
        ///     剪贴板有效时反序列化出的 section 载荷。
        /// </summary>
        public ModSettingsSectionDataClipboardPayload? Payload { get; } = payload;

        /// <summary>
        ///     When true, a subscriber handled the paste and defaults should not run.
        ///     为 true 时，某个订阅者已处理粘贴，默认逻辑不应运行。
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Whether the handler or default paste reported success.
        ///     处理程序或默认粘贴是否报告成功。
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    ///     Clipboard helpers for page/section chrome: copy serializes binding values; paste restores matching entry ids.
    ///     页面 / section chrome 的剪贴板辅助方法：复制会序列化 binding 值；粘贴会恢复匹配的条目 id。
    /// </summary>
    public static class ModSettingsUiChromeClipboard
    {
        /// <summary>
        ///     Clipboard envelope kind for whole-page chrome data.
        ///     整页 chrome 数据的剪贴板信封种类。
        /// </summary>
        public const string PageKind = "ritsulib.settings.ui.page";

        /// <summary>
        ///     Clipboard envelope kind for single-section chrome data.
        ///     单个 section chrome 数据的剪贴板信封种类。
        /// </summary>
        public const string SectionKind = "ritsulib.settings.ui.section";

        private const string PageDataTypeName = "ritsulib.settings.ui.page.data.v1";
        private const string SectionDataTypeName = "ritsulib.settings.ui.section.data.v1";

        /// <summary>
        ///     When true, page Paste is enabled when clipboard matches and ModId/PageId match the page.
        ///     为 true 时，若剪贴板匹配且 ModId/PageId 与页面一致，则启用页面粘贴。
        /// </summary>
        public static bool EnablePagePasteUi { get; set; } = true;

        /// <summary>
        ///     When true, section Paste is enabled when clipboard matches and ModId/PageId match.
        ///     为 true 时，若剪贴板匹配且 ModId/PageId 一致，则启用 section 粘贴。
        /// </summary>
        public static bool EnableSectionPasteUi { get; set; } = true;

        /// <summary>
        ///     Raised before default page copy; handlers may suppress the default clipboard write.
        ///     默认页面复制前触发；处理程序可以抑制默认剪贴板写入。
        /// </summary>
        public static event Action<ModSettingsPageCopyEventArgs>? PageCopyRequested;

        /// <summary>
        ///     Raised before default page paste; set <see cref="ModSettingsPagePasteEventArgs.Handled" /> to take over.
        ///     默认页面粘贴前触发；设置 <see cref="ModSettingsPagePasteEventArgs.Handled" /> 以接管处理。
        /// </summary>
        public static event Action<ModSettingsPagePasteEventArgs>? PagePasteRequested;

        /// <summary>
        ///     Raised before default section copy.
        ///     默认 section 复制前触发。
        /// </summary>
        public static event Action<ModSettingsSectionCopyEventArgs>? SectionCopyRequested;

        /// <summary>
        ///     Raised before default section paste.
        ///     默认 section 粘贴前触发。
        /// </summary>
        public static event Action<ModSettingsSectionPasteEventArgs>? SectionPasteRequested;

        /// <summary>
        ///     Serializes all binding snapshots on <paramref name="context" />.Page to the clipboard unless suppressed.
        ///     除非被抑制，否则将 <paramref name="context" />.Page 上的所有 binding 快照序列化到剪贴板。
        /// </summary>
        public static bool TryCopyPage(ModSettingsPageUiContext context)
        {
            var args = new ModSettingsPageCopyEventArgs(context);
            PageCopyRequested?.Invoke(args);
            if (args.SuppressDefaultClipboardWrite)
                return true;

            var sections =
                new Dictionary<string, Dictionary<string, ModSettingsChromeBindingSnapshot>>(
                    StringComparer.OrdinalIgnoreCase);
            foreach (var section in context.Page.Sections)
            {
                var map = new Dictionary<string, ModSettingsChromeBindingSnapshot>(StringComparer.Ordinal);
                foreach (var entry in section.Entries)
                    entry.CollectChromeBindingSnapshots(map);

                sections[section.Id] = map;
            }

            var payload = new ModSettingsPageDataClipboardPayload(
                context.Page.ModId,
                context.Page.Id,
                sections);

            ModSettingsClipboardData.WriteClipboardEnvelope(new(
                PageKind,
                PageDataTypeName,
                $"{context.Page.ModId}|{context.Page.Id}",
                string.Empty,
                ModSettingsClipboardScope.Self,
                JsonSerializer.Serialize(payload)));

            return true;
        }

        /// <summary>
        ///     Parses <paramref name="clipboardText" /> into a page payload when kind and type match.
        ///     当 kind 和类型匹配时，将 <paramref name="clipboardText" /> 解析为页面载荷。
        /// </summary>
        public static bool TryGetPageDataPayload(string clipboardText, out ModSettingsPageDataClipboardPayload? payload)
        {
            payload = null;
            if (!ModSettingsClipboardData.TryDeserializeEnvelope(clipboardText, out var env) || env == null)
                return false;

            if (!string.Equals(env.Kind, PageKind, StringComparison.Ordinal))
                return false;

            if (!string.Equals(env.TypeName, PageDataTypeName, StringComparison.Ordinal))
                return false;

            try
            {
                payload = JsonSerializer.Deserialize<ModSettingsPageDataClipboardPayload>(env.Payload);
                return payload != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     True when paste UI should be enabled and clipboard payload targets the same mod and page as
        ///     <paramref name="context" />.
        ///     当应启用粘贴 UI，且剪贴板载荷目标与 <paramref name="context" /> 的 mod 和页面相同时为 true。
        /// </summary>
        public static bool CanPastePage(ModSettingsPageUiContext context)
        {
            if (!EnablePagePasteUi)
                return false;

            if (!ModSettingsClipboardAccess.TryGetText(out var clip) ||
                !TryGetPageDataPayload(clip, out var payload) || payload == null)
                return false;

            return string.Equals(payload.ModId, context.Page.ModId, StringComparison.Ordinal) &&
                   string.Equals(payload.PageId, context.Page.Id, StringComparison.Ordinal);
        }

        /// <summary>
        ///     Invokes paste subscribers then applies default binding restore when unhandled.
        ///     调用粘贴订阅者；未被处理时应用默认 binding 恢复。
        /// </summary>
        public static bool TryPastePage(ModSettingsPageUiContext context)
        {
            ModSettingsClipboardAccess.TryGetText(out var clip);
            TryGetPageDataPayload(clip, out var payload);

            var args = new ModSettingsPagePasteEventArgs(context, payload);
            var h = PagePasteRequested;
            if (h == null) return TryApplyDefaultPageDataPaste(context, payload);
            foreach (var @delegate in h.GetInvocationList())
            {
                var d = (Action<ModSettingsPagePasteEventArgs>)@delegate;
                d(args);
                if (args.Handled)
                    return args.Success;
            }

            return TryApplyDefaultPageDataPaste(context, payload);
        }

        /// <summary>
        ///     Copies binding snapshots for one section to the clipboard unless suppressed.
        ///     除非被抑制，否则将一个 section 的 binding 快照复制到剪贴板。
        /// </summary>
        public static bool TryCopySection(ModSettingsSectionUiContext context)
        {
            var args = new ModSettingsSectionCopyEventArgs(context);
            SectionCopyRequested?.Invoke(args);
            if (args.SuppressDefaultClipboardWrite)
                return true;

            var map = new Dictionary<string, ModSettingsChromeBindingSnapshot>(StringComparer.Ordinal);
            foreach (var entry in context.Section.Entries)
                entry.CollectChromeBindingSnapshots(map);

            var payload = new ModSettingsSectionDataClipboardPayload(
                context.Page.ModId,
                context.Page.Id,
                context.Section.Id,
                map);

            ModSettingsClipboardData.WriteClipboardEnvelope(new(
                SectionKind,
                SectionDataTypeName,
                $"{context.Page.ModId}|{context.Page.Id}|{context.Section.Id}",
                string.Empty,
                ModSettingsClipboardScope.Self,
                JsonSerializer.Serialize(payload)));

            return true;
        }

        /// <summary>
        ///     Parses <paramref name="clipboardText" /> into a section payload when valid.
        ///     有效时，将 <paramref name="clipboardText" /> 解析为 section 载荷。
        /// </summary>
        public static bool TryGetSectionDataPayload(string clipboardText,
            out ModSettingsSectionDataClipboardPayload? payload)
        {
            payload = null;
            if (!ModSettingsClipboardData.TryDeserializeEnvelope(clipboardText, out var env) || env == null)
                return false;

            if (!string.Equals(env.Kind, SectionKind, StringComparison.Ordinal))
                return false;

            if (!string.Equals(env.TypeName, SectionDataTypeName, StringComparison.Ordinal))
                return false;

            try
            {
                payload = JsonSerializer.Deserialize<ModSettingsSectionDataClipboardPayload>(env.Payload);
                return payload != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     True when section paste UI is allowed and clipboard matches the page’s mod and page id.
        ///     当允许 section 粘贴 UI 且剪贴板与页面的 mod 和 page id 匹配时为 true。
        /// </summary>
        public static bool CanPasteSection(ModSettingsSectionUiContext context)
        {
            if (!EnableSectionPasteUi)
                return false;

            if (!ModSettingsClipboardAccess.TryGetText(out var clip) ||
                !TryGetSectionDataPayload(clip, out var payload) || payload == null)
                return false;

            return string.Equals(payload.ModId, context.Page.ModId, StringComparison.Ordinal) &&
                   string.Equals(payload.PageId, context.Page.Id, StringComparison.Ordinal);
        }

        /// <summary>
        ///     Invokes section paste subscribers then restores bindings by entry id when unhandled.
        ///     调用 section 粘贴订阅者；未被处理时按条目 id 恢复 binding。
        /// </summary>
        public static bool TryPasteSection(ModSettingsSectionUiContext context)
        {
            ModSettingsClipboardAccess.TryGetText(out var clip);
            TryGetSectionDataPayload(clip, out var payload);

            var args = new ModSettingsSectionPasteEventArgs(context, payload);
            var h = SectionPasteRequested;
            if (h == null) return TryApplyDefaultSectionDataPaste(context, payload);
            foreach (var @delegate in h.GetInvocationList())
            {
                var d = (Action<ModSettingsSectionPasteEventArgs>)@delegate;
                d(args);
                if (args.Handled)
                    return args.Success;
            }

            return TryApplyDefaultSectionDataPaste(context, payload);
        }

        private static bool TryApplyDefaultPageDataPaste(ModSettingsPageUiContext target,
            ModSettingsPageDataClipboardPayload? payload)
        {
            if (payload?.Sections.Count is not > 0)
                return false;

            var any = false;
            foreach (var section in target.Page.Sections)
            {
                if (!payload.Sections.TryGetValue(section.Id, out var map) || map.Count == 0)
                    continue;

                foreach (var entry in section.Entries)
                {
                    if (!map.TryGetValue(entry.Id, out var snap))
                        continue;
                    if (entry.TryPasteChromeBindingSnapshot(snap, target.Host))
                        any = true;
                }
            }

            return any;
        }

        private static bool TryApplyDefaultSectionDataPaste(ModSettingsSectionUiContext target,
            ModSettingsSectionDataClipboardPayload? payload)
        {
            if (payload?.Bindings.Count is not > 0)
                return false;

            var any = false;
            foreach (var entry in target.Section.Entries)
            {
                if (!payload.Bindings.TryGetValue(entry.Id, out var snap))
                    continue;
                if (entry.TryPasteChromeBindingSnapshot(snap, target.Host))
                    any = true;
            }

            return any;
        }
    }
}
