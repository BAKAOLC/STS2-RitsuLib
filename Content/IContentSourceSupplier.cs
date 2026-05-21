namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     Allows a model to dynamically supply its own content source format string for UI displays,
    ///     overriding the default mod source resolution.
    ///     允许模型动态提供用于 UI 显示的内容来源格式化字符串，
    ///     从而覆盖其默认 mod 内容来源解析。
    /// </summary>
    public interface IContentSourceSupplier
    {
        /// <summary>
        ///     The pre-formatted source string to display (e.g., "[Vanilla]", "My Mod").
        ///     要显示的预格式化来源字符串（例如 "[Vanilla]"、"My Mod"）。
        /// </summary>
        string ContentSource { get; }
    }
}
