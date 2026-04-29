namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     One persisted setting value captured for page/section chrome copy (paste uses same rules as single-value
    ///     clipboard).
    /// </summary>
    public sealed record ModSettingsChromeBindingSnapshot(
        string TypeFullName,
        string SchemaSignature,
        string JsonPayload);

    /// <summary>
    ///     Clipboard payload: all binding values in one section, keyed by entry id.
    /// </summary>
    public sealed record ModSettingsSectionDataClipboardPayload(
        string ModId,
        string PageId,
        string SectionId,
        Dictionary<string, ModSettingsChromeBindingSnapshot> Bindings);

    /// <summary>
    ///     Clipboard payload: binding values for an entire page, keyed by section id then entry id.
    /// </summary>
    public sealed record ModSettingsPageDataClipboardPayload(
        string ModId,
        string PageId,
        Dictionary<string, Dictionary<string, ModSettingsChromeBindingSnapshot>> Sections);
}
