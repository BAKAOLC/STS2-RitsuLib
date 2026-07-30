using System.Text.Json;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Supplies context for a page-copy request. Setting <see cref="SuppressDefaultClipboardWrite" /> prevents
    ///         the default JSON envelope from replacing a subscriber-provided clipboard value.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供页面复制请求的上下文。设置 <see cref="SuppressDefaultClipboardWrite" /> 可阻止默认 JSON 信封覆盖
    ///         订阅者写入的剪贴板内容。
    ///     </para>
    /// </summary>
    public sealed class ModSettingsPageCopyEventArgs(ModSettingsPageUiContext context) : EventArgs
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the page context being copied.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取正在复制的页面上下文。
        ///     </para>
        /// </summary>
        public ModSettingsPageUiContext Context { get; } =
            context ?? throw new ArgumentNullException(nameof(context));

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether <see cref="ModSettingsUiChromeClipboard" /> skips the default envelope write.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置 <see cref="ModSettingsUiChromeClipboard" /> 是否跳过默认信封写入。
        ///     </para>
        /// </summary>
        public bool SuppressDefaultClipboardWrite { get; set; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Supplies context for a page-paste request. Subscribers run in registration order until one sets
    ///         <see cref="Handled" />; otherwise the default path restores matching setting values from
    ///         <see cref="ModSettingsPageDataClipboardPayload" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供页面粘贴请求的上下文。订阅者按注册顺序运行，直至其中一个设置 <see cref="Handled" />；
    ///         若无人处理，默认流程会从 <see cref="ModSettingsPageDataClipboardPayload" /> 恢复 ID 匹配的设置值。
    ///     </para>
    /// </summary>
    public sealed class ModSettingsPagePasteEventArgs(
        ModSettingsPageUiContext target,
        ModSettingsPageDataClipboardPayload? payload)
        : EventArgs
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the page receiving the paste.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取接收粘贴的页面。
        ///     </para>
        /// </summary>
        public ModSettingsPageUiContext Target { get; } =
            target ?? throw new ArgumentNullException(nameof(target));

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the validated page payload deserialized from the clipboard, or <see langword="null" /> when the
        ///         clipboard does not contain a compatible envelope.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取从剪贴板反序列化并通过验证的页面载荷；剪贴板不包含兼容信封时为
        ///         <see langword="null" />。
        ///     </para>
        /// </summary>
        public ModSettingsPageDataClipboardPayload? Payload { get; } = payload;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether this request has been handled. Setting it stops later subscribers and default
        ///         handling.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置该请求是否已处理。设为 <see langword="true" /> 后不再运行后续订阅者和默认逻辑。
        ///     </para>
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets the result returned when <see cref="Handled" /> is
        ///         <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置 <see cref="Handled" /> 为 <see langword="true" /> 时返回的处理结果。
        ///     </para>
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Supplies context for a section-copy request.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供节复制请求的上下文。
    ///     </para>
    /// </summary>
    public sealed class ModSettingsSectionCopyEventArgs(ModSettingsSectionUiContext context) : EventArgs
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the section context being copied.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取正在复制的节上下文。
        ///     </para>
        /// </summary>
        public ModSettingsSectionUiContext Context { get; } =
            context ?? throw new ArgumentNullException(nameof(context));

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether the default envelope write is skipped.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置是否跳过默认信封写入。
        ///     </para>
        /// </summary>
        public bool SuppressDefaultClipboardWrite { get; set; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Supplies context for a section-paste request. Subscribers run in registration order until one sets
    ///         <see cref="Handled" />; otherwise the default path restores setting snapshots by entry ID.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供节粘贴请求的上下文。订阅者按注册顺序运行，直至其中一个设置 <see cref="Handled" />；
    ///         若无人处理，默认流程会按条目 ID 恢复设置快照。
    ///     </para>
    /// </summary>
    public sealed class ModSettingsSectionPasteEventArgs(
        ModSettingsSectionUiContext target,
        ModSettingsSectionDataClipboardPayload? payload)
        : EventArgs
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the section receiving the paste.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取接收粘贴的节。
        ///     </para>
        /// </summary>
        public ModSettingsSectionUiContext Target { get; } =
            target ?? throw new ArgumentNullException(nameof(target));

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the validated section payload deserialized from the clipboard, or <see langword="null" /> when
        ///         the clipboard does not contain a compatible envelope.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取从剪贴板反序列化并通过验证的节载荷；剪贴板不包含兼容信封时为
        ///         <see langword="null" />。
        ///     </para>
        /// </summary>
        public ModSettingsSectionDataClipboardPayload? Payload { get; } = payload;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether this request has been handled. Setting it stops later subscribers and default
        ///         handling.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置该请求是否已处理。设为 <see langword="true" /> 后不再运行后续订阅者和默认逻辑。
        ///     </para>
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets the result returned when <see cref="Handled" /> is
        ///         <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置 <see cref="Handled" /> 为 <see langword="true" /> 时返回的处理结果。
        ///     </para>
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Copies page or section setting snapshots into typed clipboard envelopes and restores values whose target
    ///         and entry IDs match.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将页面或节的设置快照复制到带类型的剪贴板信封中，并恢复目标与条目 ID 均匹配的值。
    ///     </para>
    /// </summary>
    public static class ModSettingsUiChromeClipboard
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Clipboard-envelope discriminator for whole-page snapshots.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         整页快照的剪贴板信封类型标识。
        ///     </para>
        /// </summary>
        public const string PageKind = "ritsulib.settings.ui.page";

        /// <summary>
        ///     <para xml:lang="en">
        ///         Clipboard-envelope discriminator for single-section snapshots.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         单个节快照的剪贴板信封类型标识。
        ///     </para>
        /// </summary>
        public const string SectionKind = "ritsulib.settings.ui.section";

        private const string PageDataTypeName = "ritsulib.settings.ui.page.data.v1";
        private const string SectionDataTypeName = "ritsulib.settings.ui.section.data.v1";

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether page paste controls may be enabled for a compatible clipboard payload whose mod
        ///         and page IDs match the target.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置是否可为兼容且模组与页面 ID 均匹配目标的剪贴板载荷启用页面粘贴控件。
        ///     </para>
        /// </summary>
        public static bool EnablePagePasteUi { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether section paste controls may be enabled for a compatible clipboard payload whose
        ///         mod, page, and section IDs match the target.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置是否可为兼容且模组、页面与节 ID 均匹配目标的剪贴板载荷启用节粘贴控件。
        ///     </para>
        /// </summary>
        public static bool EnableSectionPasteUi { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Occurs before the default page snapshot is written to the clipboard.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在默认页面快照写入剪贴板前发生。
        ///     </para>
        /// </summary>
        public static event Action<ModSettingsPageCopyEventArgs>? PageCopyRequested;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Occurs before the default page paste. A subscriber can set
        ///         <see cref="ModSettingsPagePasteEventArgs.Handled" /> to provide the result and stop further handling.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在默认页面粘贴前发生。订阅者可设置 <see cref="ModSettingsPagePasteEventArgs.Handled" />
        ///         以提供结果并停止后续处理。
        ///     </para>
        /// </summary>
        public static event Action<ModSettingsPagePasteEventArgs>? PagePasteRequested;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Occurs before the default section snapshot is written to the clipboard.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在默认节快照写入剪贴板前发生。
        ///     </para>
        /// </summary>
        public static event Action<ModSettingsSectionCopyEventArgs>? SectionCopyRequested;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Occurs before the default section paste. A subscriber can set
        ///         <see cref="ModSettingsSectionPasteEventArgs.Handled" /> to provide the result and stop further
        ///         handling.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在默认节粘贴前发生。订阅者可设置 <see cref="ModSettingsSectionPasteEventArgs.Handled" />
        ///         以提供结果并停止后续处理。
        ///     </para>
        /// </summary>
        public static event Action<ModSettingsSectionPasteEventArgs>? SectionPasteRequested;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Requests a page copy, then writes snapshots of all settings on the page to the clipboard unless a
        ///         subscriber suppresses the default write.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         请求复制页面；除非订阅者阻止默认写入，否则将页面中全部设置的快照写入剪贴板。
        ///     </para>
        /// </summary>
        /// <param name="context">
        ///     <para xml:lang="en">The page context to copy.</para>
        ///     <para xml:lang="zh-CN">要复制的页面上下文。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         Always <see langword="true" /> after the request is suppressed or the default clipboard write
        ///         completes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         请求被阻止默认写入或默认剪贴板写入完成后始终为 <see langword="true" />。
        ///     </para>
        /// </returns>
        public static bool TryCopyPage(ModSettingsPageUiContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            var args = new ModSettingsPageCopyEventArgs(context);
            PageCopyRequested?.Invoke(args);
            if (args.SuppressDefaultClipboardWrite)
                return true;

            var sections =
                new Dictionary<string, Dictionary<string, ModSettingsChromeBindingSnapshot>>(
                    StringComparer.OrdinalIgnoreCase);
            foreach (var section in context.Page.Sections)
            {
                var map = new Dictionary<string, ModSettingsChromeBindingSnapshot>(
                    StringComparer.OrdinalIgnoreCase);
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
        ///     <para xml:lang="en">
        ///         Attempts to read a structurally valid page snapshot from a clipboard envelope with the expected kind
        ///         and payload type.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试从类型标识与载荷类型均符合预期的剪贴板信封中读取结构有效的页面快照。
        ///     </para>
        /// </summary>
        /// <param name="clipboardText">
        ///     <para xml:lang="en">The clipboard text to parse.</para>
        ///     <para xml:lang="zh-CN">要解析的剪贴板文本。</para>
        /// </param>
        /// <param name="payload">
        ///     <para xml:lang="en">The parsed page payload when successful; otherwise, <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN">成功时为解析出的页面载荷；否则为 <see langword="null" />。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when the envelope contains valid identifiers, collections, and binding
        ///         snapshots; otherwise, <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         信封包含有效标识、集合和绑定快照时为 <see langword="true" />；否则为
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryGetPageDataPayload(string clipboardText, out ModSettingsPageDataClipboardPayload? payload)
        {
            ArgumentNullException.ThrowIfNull(clipboardText);
            payload = null;
            if (!ModSettingsClipboardData.TryDeserializeEnvelope(clipboardText, out var env) || env == null)
                return false;

            if (!string.Equals(env.Kind, PageKind, StringComparison.Ordinal))
                return false;

            if (!string.Equals(env.TypeName, PageDataTypeName, StringComparison.Ordinal))
                return false;

            try
            {
                var parsed = JsonSerializer.Deserialize<ModSettingsPageDataClipboardPayload>(env.Payload);
                if (parsed is not { Sections: not null } ||
                    string.IsNullOrWhiteSpace(parsed.ModId) ||
                    string.IsNullOrWhiteSpace(parsed.PageId) ||
                    !TryNormalizeSections(parsed.Sections, out var sections))
                    return false;

                payload = parsed with { Sections = sections };
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Determines whether page paste controls should be enabled for the current clipboard contents and
        ///         target page.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         确定是否应针对当前剪贴板内容与目标页面启用页面粘贴控件。
        ///     </para>
        /// </summary>
        /// <param name="context">
        ///     <para xml:lang="en">The target page context.</para>
        ///     <para xml:lang="zh-CN">目标页面上下文。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when page paste UI is enabled and a valid payload has matching mod and page
        ///         IDs; otherwise, <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         页面粘贴界面已启用且有效载荷的模组与页面 ID 均匹配时为 <see langword="true" />；
        ///         否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool CanPastePage(ModSettingsPageUiContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (!EnablePagePasteUi)
                return false;

            if (!ModSettingsClipboardAccess.TryGetText(out var clip) ||
                !TryGetPageDataPayload(clip, out var payload) || payload == null)
                return false;

            return IsPagePayloadForTarget(payload, context);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Requests a page paste and returns the first subscriber-provided result; if no subscriber handles the
        ///         request, restores matching setting snapshots from the clipboard.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         请求粘贴页面并返回首个处理该请求的订阅者所提供的结果；若无人处理，则从剪贴板恢复匹配的设置快照。
        ///     </para>
        /// </summary>
        /// <param name="context">
        ///     <para xml:lang="en">The page context receiving the paste.</para>
        ///     <para xml:lang="zh-CN">接收粘贴的页面上下文。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The handling subscriber's result, or <see langword="true" /> when the default path applied at least
        ///         one matching snapshot; otherwise, <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         处理请求的订阅者所提供的结果；若使用默认流程，则至少应用一个匹配快照时为
        ///         <see langword="true" />，否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryPastePage(ModSettingsPageUiContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
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
        ///     <para xml:lang="en">
        ///         Requests a section copy, then writes snapshots of all settings in the section to the clipboard unless
        ///         a subscriber suppresses the default write.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         请求复制节；除非订阅者阻止默认写入，否则将节中全部设置的快照写入剪贴板。
        ///     </para>
        /// </summary>
        /// <param name="context">
        ///     <para xml:lang="en">The section context to copy.</para>
        ///     <para xml:lang="zh-CN">要复制的节上下文。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         Always <see langword="true" /> after the request is suppressed or the default clipboard write
        ///         completes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         请求被阻止默认写入或默认剪贴板写入完成后始终为 <see langword="true" />。
        ///     </para>
        /// </returns>
        public static bool TryCopySection(ModSettingsSectionUiContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            var args = new ModSettingsSectionCopyEventArgs(context);
            SectionCopyRequested?.Invoke(args);
            if (args.SuppressDefaultClipboardWrite)
                return true;

            var map = new Dictionary<string, ModSettingsChromeBindingSnapshot>(
                StringComparer.OrdinalIgnoreCase);
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
        ///     <para xml:lang="en">
        ///         Attempts to read a structurally valid section snapshot from a clipboard envelope with the expected
        ///         kind and payload type.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试从类型标识与载荷类型均符合预期的剪贴板信封中读取结构有效的节快照。
        ///     </para>
        /// </summary>
        /// <param name="clipboardText">
        ///     <para xml:lang="en">The clipboard text to parse.</para>
        ///     <para xml:lang="zh-CN">要解析的剪贴板文本。</para>
        /// </param>
        /// <param name="payload">
        ///     <para xml:lang="en">The parsed section payload when successful; otherwise, <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN">成功时为解析出的节载荷；否则为 <see langword="null" />。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when the envelope contains valid identifiers, a binding collection, and
        ///         binding snapshots; otherwise, <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         信封包含有效标识、绑定集合和绑定快照时为 <see langword="true" />；否则为
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryGetSectionDataPayload(string clipboardText,
            out ModSettingsSectionDataClipboardPayload? payload)
        {
            ArgumentNullException.ThrowIfNull(clipboardText);
            payload = null;
            if (!ModSettingsClipboardData.TryDeserializeEnvelope(clipboardText, out var env) || env == null)
                return false;

            if (!string.Equals(env.Kind, SectionKind, StringComparison.Ordinal))
                return false;

            if (!string.Equals(env.TypeName, SectionDataTypeName, StringComparison.Ordinal))
                return false;

            try
            {
                var parsed = JsonSerializer.Deserialize<ModSettingsSectionDataClipboardPayload>(env.Payload);
                if (parsed is not { Bindings: not null } ||
                    string.IsNullOrWhiteSpace(parsed.ModId) ||
                    string.IsNullOrWhiteSpace(parsed.PageId) ||
                    string.IsNullOrWhiteSpace(parsed.SectionId) ||
                    !TryNormalizeBindings(parsed.Bindings, out var bindings))
                    return false;

                payload = parsed with { Bindings = bindings };
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Determines whether section paste controls should be enabled for the current clipboard contents and
        ///         target section.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         确定是否应针对当前剪贴板内容与目标节启用节粘贴控件。
        ///     </para>
        /// </summary>
        /// <param name="context">
        ///     <para xml:lang="en">The target section context.</para>
        ///     <para xml:lang="zh-CN">目标节上下文。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when section paste UI is enabled and a valid payload has matching mod, page,
        ///         and section IDs; otherwise, <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         节粘贴界面已启用且有效载荷的模组、页面与节 ID 均匹配时为
        ///         <see langword="true" />；否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool CanPasteSection(ModSettingsSectionUiContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (!EnableSectionPasteUi)
                return false;

            if (!ModSettingsClipboardAccess.TryGetText(out var clip) ||
                !TryGetSectionDataPayload(clip, out var payload) || payload == null)
                return false;

            return IsSectionPayloadForTarget(payload, context);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Requests a section paste and returns the first subscriber-provided result; if no subscriber handles
        ///         the request, restores matching setting snapshots from the clipboard.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         请求粘贴节并返回首个处理该请求的订阅者所提供的结果；若无人处理，则从剪贴板恢复匹配的设置快照。
        ///     </para>
        /// </summary>
        /// <param name="context">
        ///     <para xml:lang="en">The section context receiving the paste.</para>
        ///     <para xml:lang="zh-CN">接收粘贴的节上下文。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The handling subscriber's result, or <see langword="true" /> when the default path applied at least
        ///         one matching snapshot; otherwise, <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         处理请求的订阅者所提供的结果；若使用默认流程，则至少应用一个匹配快照时为
        ///         <see langword="true" />，否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryPasteSection(ModSettingsSectionUiContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
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
            if (payload?.Sections is not { Count: > 0 } || !IsPagePayloadForTarget(payload, target))
                return false;

            var any = false;
            foreach (var section in target.Page.Sections)
            {
                if (!payload.Sections.TryGetValue(section.Id, out var map) || map is not { Count: > 0 })
                    continue;

                foreach (var entry in section.Entries)
                {
                    if (!map.TryGetValue(entry.Id, out var snap) || snap == null)
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
            if (payload?.Bindings is not { Count: > 0 } || !IsSectionPayloadForTarget(payload, target))
                return false;

            var any = false;
            foreach (var entry in target.Section.Entries)
            {
                if (!payload.Bindings.TryGetValue(entry.Id, out var snap) || snap == null)
                    continue;
                if (entry.TryPasteChromeBindingSnapshot(snap, target.Host))
                    any = true;
            }

            return any;
        }

        private static bool IsPagePayloadForTarget(
            ModSettingsPageDataClipboardPayload payload,
            ModSettingsPageUiContext target)
        {
            return string.Equals(payload.ModId, target.Page.ModId, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(payload.PageId, target.Page.Id, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSectionPayloadForTarget(
            ModSettingsSectionDataClipboardPayload payload,
            ModSettingsSectionUiContext target)
        {
            return string.Equals(payload.ModId, target.Page.ModId, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(payload.PageId, target.Page.Id, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(payload.SectionId, target.Section.Id, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryNormalizeSections(
            Dictionary<string, Dictionary<string, ModSettingsChromeBindingSnapshot>> source,
            out Dictionary<string, Dictionary<string, ModSettingsChromeBindingSnapshot>> sections)
        {
            sections = new(StringComparer.OrdinalIgnoreCase);
            foreach (var (sectionId, sourceBindings) in source)
            {
                if (string.IsNullOrWhiteSpace(sectionId) ||
                    sourceBindings == null ||
                    !TryNormalizeBindings(sourceBindings, out var bindings) ||
                    !sections.TryAdd(sectionId, bindings))
                    return false;
            }

            return true;
        }

        private static bool TryNormalizeBindings(
            Dictionary<string, ModSettingsChromeBindingSnapshot> source,
            out Dictionary<string, ModSettingsChromeBindingSnapshot> bindings)
        {
            bindings = new(StringComparer.OrdinalIgnoreCase);
            foreach (var (entryId, snapshot) in source)
            {
                if (string.IsNullOrWhiteSpace(entryId) ||
                    snapshot is not
                    {
                        TypeFullName: not null,
                        SchemaSignature: not null,
                        JsonPayload: not null,
                    } ||
                    !bindings.TryAdd(entryId, snapshot))
                    return false;
            }

            return true;
        }
    }
}
