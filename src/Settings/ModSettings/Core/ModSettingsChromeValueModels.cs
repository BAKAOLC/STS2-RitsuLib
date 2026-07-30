namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Captures one setting value in a page or section snapshot. Restoring it uses the same type, schema, and
    ///         scalar-conversion rules as a single-value clipboard operation.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         捕获页面或节快照中的一个设置值。恢复时采用与单值剪贴板操作相同的类型、架构与标量转换规则。
    ///     </para>
    /// </summary>
    /// <param name="TypeFullName">
    ///     <para xml:lang="en">The captured value type's CLR full name.</para>
    ///     <para xml:lang="zh-CN">所捕获值类型的 CLR 全名。</para>
    /// </param>
    /// <param name="SchemaSignature">
    ///     <para xml:lang="en">The structural signature of the captured value type.</para>
    ///     <para xml:lang="zh-CN">所捕获值类型的结构签名。</para>
    /// </param>
    /// <param name="JsonPayload">
    ///     <para xml:lang="en">The serialized value payload.</para>
    ///     <para xml:lang="zh-CN">序列化后的值载荷。</para>
    /// </param>
    public sealed record ModSettingsChromeBindingSnapshot(
        string TypeFullName,
        string SchemaSignature,
        string JsonPayload);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Contains setting snapshots for one section, keyed by entry ID and scoped to its owning mod, page, and
    ///         section.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         包含一个节中按条目 ID 索引的设置快照，并限定到其所属模组、页面与节。
    ///     </para>
    /// </summary>
    /// <param name="ModId">
    ///     <para xml:lang="en">The owning mod ID.</para>
    ///     <para xml:lang="zh-CN">所属模组 ID。</para>
    /// </param>
    /// <param name="PageId">
    ///     <para xml:lang="en">The owning page ID.</para>
    ///     <para xml:lang="zh-CN">所属页面 ID。</para>
    /// </param>
    /// <param name="SectionId">
    ///     <para xml:lang="en">The source section ID.</para>
    ///     <para xml:lang="zh-CN">源节 ID。</para>
    /// </param>
    /// <param name="Bindings">
    ///     <para xml:lang="en">The setting snapshots keyed by entry ID.</para>
    ///     <para xml:lang="zh-CN">按条目 ID 索引的设置快照。</para>
    /// </param>
    public sealed record ModSettingsSectionDataClipboardPayload(
        string ModId,
        string PageId,
        string SectionId,
        Dictionary<string, ModSettingsChromeBindingSnapshot> Bindings);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Contains all setting snapshots on one page, keyed first by section ID and then by entry ID.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         包含一个页面中的全部设置快照，先按节 ID、再按条目 ID 索引。
    ///     </para>
    /// </summary>
    /// <param name="ModId">
    ///     <para xml:lang="en">The owning mod ID.</para>
    ///     <para xml:lang="zh-CN">所属模组 ID。</para>
    /// </param>
    /// <param name="PageId">
    ///     <para xml:lang="en">The source page ID.</para>
    ///     <para xml:lang="zh-CN">源页面 ID。</para>
    /// </param>
    /// <param name="Sections">
    ///     <para xml:lang="en">The setting snapshots keyed by section ID and then entry ID.</para>
    ///     <para xml:lang="zh-CN">先按节 ID、再按条目 ID 索引的设置快照。</para>
    /// </param>
    public sealed record ModSettingsPageDataClipboardPayload(
        string ModId,
        string PageId,
        Dictionary<string, Dictionary<string, ModSettingsChromeBindingSnapshot>> Sections);
}
