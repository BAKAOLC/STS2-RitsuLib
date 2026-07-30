namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies where a registered mod keyword's inline card text (gold title followed by a period) is merged
    ///         into the rendered card description, following the native <c>CardKeywordOrder</c> behavior.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         指定已注册模组关键词的内联卡牌文本（金色标题及其后的句号）在渲染后卡牌描述中的插入位置；
    ///         其行为遵循原版 <c>CardKeywordOrder</c>。
    ///     </para>
    /// </summary>
    public enum ModKeywordCardDescriptionPlacement
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Does not inject keyword text into the card description. This is the default.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         不向卡牌描述注入关键词文本。此项为默认值。
        ///     </para>
        /// </summary>
        None = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Inserts the keyword text before the main description block, like native “before description”
        ///         keywords.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将关键词文本插入主要描述文本之前，对应原版的“描述前关键词”。
        ///     </para>
        /// </summary>
        BeforeCardDescription = 1,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends the keyword text after the main description block, like native “after description”
        ///         keywords.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将关键词文本追加到主要描述文本之后，对应原版的“描述后关键词”。
        ///     </para>
        /// </summary>
        AfterCardDescription = 2,
    }
}
