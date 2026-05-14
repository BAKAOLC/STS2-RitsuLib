namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     One persisted setting value captured for page/section chrome copy (paste uses same rules as single-value
    ///     clipboard).
    ///     为页面/section chrome 复制捕获的单个持久化设置值（粘贴使用与单值剪贴板相同的规则）。
    /// </summary>
    public sealed record ModSettingsChromeBindingSnapshot(
        string TypeFullName,
        string SchemaSignature,
        string JsonPayload);

    /// <summary>
    ///     Clipboard payload: all binding values in one section, keyed by entry id.
    ///     剪贴板载荷：一个 section 中的全部绑定值，以条目 id 为键。
    /// </summary>
    public sealed record ModSettingsSectionDataClipboardPayload(
        string ModId,
        string PageId,
        string SectionId,
        Dictionary<string, ModSettingsChromeBindingSnapshot> Bindings);

    /// <summary>
    ///     Clipboard payload: binding values for an entire page, keyed by section id then entry id.
    ///     剪贴板载荷：整个页面的绑定值，先按 section id、再按条目 id 作为键。
    /// </summary>
    public sealed record ModSettingsPageDataClipboardPayload(
        string ModId,
        string PageId,
        Dictionary<string, Dictionary<string, ModSettingsChromeBindingSnapshot>> Sections);
}
