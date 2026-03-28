using System.Text.Json;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Raised before page copy; when <see cref="SuppressDefaultClipboardWrite" /> is true, the default JSON envelope is
    ///     not written.
    /// </summary>
    public sealed class ModSettingsPageCopyEventArgs(ModSettingsPageUiContext context) : EventArgs
    {
        /// <summary>
        ///     Page context being copied.
        /// </summary>
        public ModSettingsPageUiContext Context { get; } = context;

        /// <summary>
        ///     When true, <see cref="ModSettingsUiChromeClipboard" /> skips writing the default envelope.
        /// </summary>
        public bool SuppressDefaultClipboardWrite { get; set; }
    }

    /// <summary>
    ///     Page paste: subscribers run first; if none handle, default applies binding values from
    ///     <see cref="ModSettingsPageDataClipboardPayload" />.
    /// </summary>
    public sealed class ModSettingsPagePasteEventArgs(
        ModSettingsPageUiContext target,
        ModSettingsPageDataClipboardPayload? payload)
        : EventArgs
    {
        /// <summary>
        ///     Page receiving the paste.
        /// </summary>
        public ModSettingsPageUiContext Target { get; } = target;

        /// <summary>
        ///     Deserialized page payload from the clipboard, when valid.
        /// </summary>
        public ModSettingsPageDataClipboardPayload? Payload { get; } = payload;

        /// <summary>
        ///     When true, this paste was consumed and later subscribers should not run.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Outcome after handling (whether paste applied successfully).
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    ///     Raised before section copy.
    /// </summary>
    public sealed class ModSettingsSectionCopyEventArgs(ModSettingsSectionUiContext context) : EventArgs
    {
        /// <summary>
        ///     Section context being copied.
        /// </summary>
        public ModSettingsSectionUiContext Context { get; } = context;

        /// <summary>
        ///     When true, default envelope write is skipped.
        /// </summary>
        public bool SuppressDefaultClipboardWrite { get; set; }
    }

    /// <summary>
    ///     Section paste: subscribers first, then default applies binding snapshots by entry id.
    /// </summary>
    public sealed class ModSettingsSectionPasteEventArgs(
        ModSettingsSectionUiContext target,
        ModSettingsSectionDataClipboardPayload? payload)
        : EventArgs
    {
        /// <summary>
        ///     Section receiving the paste.
        /// </summary>
        public ModSettingsSectionUiContext Target { get; } = target;

        /// <summary>
        ///     Deserialized section payload when the clipboard is valid.
        /// </summary>
        public ModSettingsSectionDataClipboardPayload? Payload { get; } = payload;

        /// <summary>
        ///     When true, a subscriber handled the paste and defaults should not run.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Whether the handler or default paste reported success.
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    ///     Clipboard helpers for page/section chrome: copy serializes binding values; paste restores matching entry ids.
    /// </summary>
    public static class ModSettingsUiChromeClipboard
    {
        /// <summary>
        ///     Clipboard envelope kind for whole-page chrome data.
        /// </summary>
        public const string PageKind = "ritsulib.settings.ui.page";

        /// <summary>
        ///     Clipboard envelope kind for single-section chrome data.
        /// </summary>
        public const string SectionKind = "ritsulib.settings.ui.section";

        private const string PageDataTypeName = "ritsulib.settings.ui.page.data.v1";
        private const string SectionDataTypeName = "ritsulib.settings.ui.section.data.v1";

        /// <summary>
        ///     When true, page Paste is enabled when clipboard matches and ModId/PageId match the page.
        /// </summary>
        public static bool EnablePagePasteUi { get; set; } = true;

        /// <summary>
        ///     When true, section Paste is enabled when clipboard matches and ModId/PageId match.
        /// </summary>
        public static bool EnableSectionPasteUi { get; set; } = true;

        /// <summary>
        ///     Raised before default page copy; handlers may suppress the default clipboard write.
        /// </summary>
        public static event Action<ModSettingsPageCopyEventArgs>? PageCopyRequested;

        /// <summary>
        ///     Raised before default page paste; set <see cref="ModSettingsPagePasteEventArgs.Handled" /> to take over.
        /// </summary>
        public static event Action<ModSettingsPagePasteEventArgs>? PagePasteRequested;

        /// <summary>
        ///     Raised before default section copy.
        /// </summary>
        public static event Action<ModSettingsSectionCopyEventArgs>? SectionCopyRequested;

        /// <summary>
        ///     Raised before default section paste.
        /// </summary>
        public static event Action<ModSettingsSectionPasteEventArgs>? SectionPasteRequested;

        /// <summary>
        ///     Serializes all binding snapshots on <paramref name="context" />.Page to the clipboard unless suppressed.
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
