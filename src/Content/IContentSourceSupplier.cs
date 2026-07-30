namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">Describes the source displayed for a piece of content.</para>
    ///     <para xml:lang="zh-CN">描述为一项内容显示的来源。</para>
    /// </summary>
    public readonly record struct ContentSourceDescriptor
    {
        /// <summary>
        ///     <para xml:lang="en">Initializes a content-source descriptor.</para>
        ///     <para xml:lang="zh-CN">初始化内容来源描述。</para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The source mod ID, or <c>Vanilla</c> for base-game content.</para>
        ///     <para xml:lang="zh-CN">来源模组 ID；原版内容使用 <c>Vanilla</c>。</para>
        /// </param>
        /// <param name="displayName">
        ///     <para xml:lang="en">
        ///         The optional display name. RitsuLib resolves a missing name from known mod metadata.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选显示名称。未提供时，RitsuLib 会根据已知模组元数据解析名称。
        ///     </para>
        /// </param>
        public ContentSourceDescriptor(string modId, string? displayName = null)
        {
            ModId = modId;
            DisplayName = displayName;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the source mod ID, or <c>Vanilla</c> for base-game content.</para>
        ///     <para xml:lang="zh-CN">获取来源模组 ID；原版内容使用 <c>Vanilla</c>。</para>
        /// </summary>
        public string ModId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional source display name.</para>
        ///     <para xml:lang="zh-CN">获取可选的来源显示名称。</para>
        /// </summary>
        public string? DisplayName { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Allows a model to override the content-source metadata displayed by the UI.
    ///     </para>
    ///     <para xml:lang="zh-CN">允许模型覆盖界面显示的内容来源信息。</para>
    /// </summary>
    public interface IContentSourceSupplier
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the content-source metadata to display.</para>
        ///     <para xml:lang="zh-CN">获取要显示的内容来源信息。</para>
        /// </summary>
        ContentSourceDescriptor ContentSource { get; }
    }
}
