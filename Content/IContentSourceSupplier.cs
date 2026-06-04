namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     Structured content source metadata for UI display.
    ///     用于 UI 显示的结构化内容来源元数据。
    /// </summary>
    public readonly record struct ContentSourceDescriptor
    {
        /// <summary>
        ///     Creates a structured content source descriptor.
        ///     创建结构化内容来源描述。
        /// </summary>
        /// <param name="modId">
        ///     The source mod id, or <c>Vanilla</c> for base-game content.
        ///     来源 mod id；原版内容使用 <c>Vanilla</c>。
        /// </param>
        /// <param name="displayName">
        ///     Optional display name. When omitted, RitsuLib resolves it from known mod metadata.
        ///     可选显示名；省略时 RitsuLib 会从已知 mod 元数据中解析。
        /// </param>
        public ContentSourceDescriptor(string modId, string? displayName = null)
        {
            ModId = modId;
            DisplayName = displayName;
        }

        /// <summary>
        ///     The source mod id, or <c>Vanilla</c> for base-game content.
        ///     来源 mod id；原版内容使用 <c>Vanilla</c>。
        /// </summary>
        public string ModId { get; init; }

        /// <summary>
        ///     Optional display name. When omitted, RitsuLib resolves it from known mod metadata.
        ///     可选显示名；省略时 RitsuLib 会从已知 mod 元数据中解析。
        /// </summary>
        public string? DisplayName { get; init; }
    }

    /// <summary>
    ///     Allows a model to dynamically supply structured content source metadata for UI displays,
    ///     overriding the default mod source resolution.
    ///     允许模型动态提供用于 UI 显示的结构化内容来源元数据，
    ///     从而覆盖其默认 mod 内容来源解析。
    /// </summary>
    public interface IContentSourceSupplier
    {
        /// <summary>
        ///     The source metadata to display.
        ///     要显示的来源元数据。
        /// </summary>
        ContentSourceDescriptor ContentSource { get; }
    }
}
