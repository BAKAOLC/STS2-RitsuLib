using STS2RitsuLib.Content;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Represents a declarative content-pack keyword entry that can be applied to a
    ///         <see cref="ModKeywordRegistry" /> in one call.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         表示内容包使用的声明式关键词条目，可通过一次调用应用到
    ///         <see cref="ModKeywordRegistry" />。
    ///     </para>
    /// </summary>
    public sealed record KeywordRegistrationEntry
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Initializes an entry with card-description placement and hover-tip inclusion behavior.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用卡牌描述插入位置和悬停提示包含行为初始化条目。
        ///     </para>
        /// </summary>
        public KeywordRegistrationEntry(
            string Id,
            string TitleTable,
            string TitleKey,
            string DescriptionTable,
            string DescriptionKey,
            string? IconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            this.Id = Id;
            this.TitleTable = TitleTable;
            this.TitleKey = TitleKey;
            this.DescriptionTable = DescriptionTable;
            this.DescriptionKey = DescriptionKey;
            this.IconPath = IconPath;
            CardDescriptionPlacement = cardDescriptionPlacement;
            IncludeInCardHoverTip = includeInCardHoverTip;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Initializes an entry through the six-parameter constructor retained for compatibility with older
        ///         mods.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过为兼容旧版模组而保留的六参数构造函数初始化条目。
        ///     </para>
        /// </summary>
        public KeywordRegistrationEntry(
            string Id,
            string TitleTable,
            string TitleKey,
            string DescriptionTable,
            string DescriptionKey,
            string? IconPath = null)
            : this(
                Id,
                TitleTable,
                TitleKey,
                DescriptionTable,
                DescriptionKey,
                IconPath,
                ModKeywordCardDescriptionPlacement.None,
                true)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the keyword ID, which is trimmed during registration and compared case-insensitively.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取关键词 ID。注册时会移除其首尾空白，比较时不区分大小写。
        ///     </para>
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the title localization table.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取标题所在的本地化表。
        ///     </para>
        /// </summary>
        public string TitleTable { get; init; } = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the title localization key.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取标题本地化键。
        ///     </para>
        /// </summary>
        public string TitleKey { get; init; } = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the description localization table.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取描述所在的本地化表。
        ///     </para>
        /// </summary>
        public string DescriptionTable { get; init; } = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the description localization key.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取描述本地化键。
        ///     </para>
        /// </summary>
        public string DescriptionKey { get; init; } = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional icon resource path.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取可选的图标资源路径。
        ///     </para>
        /// </summary>
        public string? IconPath { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the inline keyword text's placement in card descriptions.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取内联关键词文本在卡牌描述中的插入位置。
        ///     </para>
        /// </summary>
        public ModKeywordCardDescriptionPlacement CardDescriptionPlacement { get; init; } =
            ModKeywordCardDescriptionPlacement.None;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether card and template hover-tip helpers include this keyword.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取卡牌和模板的悬停提示辅助方法是否包含此关键词。
        ///     </para>
        /// </summary>
        public bool IncludeInCardHoverTip { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers this entry with <paramref name="registry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将此条目注册到 <paramref name="registry" />。
        ///     </para>
        /// </summary>
        public void Register(ModKeywordRegistry registry)
        {
            registry.RegisterCore(
                Id,
                TitleTable,
                TitleKey,
                DescriptionTable,
                DescriptionKey,
                IconPath,
                CardDescriptionPlacement,
                IncludeInCardHoverTip);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a <c>card_keywords</c> entry whose ID and localization-key stem are both produced by
        ///         <see cref="ModContentRegistry.GetQualifiedKeywordId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建 <c>card_keywords</c> 条目，其 ID 和本地化键前缀均由
        ///         <see cref="ModContentRegistry.GetQualifiedKeywordId" /> 生成。
        ///     </para>
        /// </summary>
        public static KeywordRegistrationEntry OwnedCardByLocNamespace(
            string modId,
            string localKeywordStem,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            var id = ModContentRegistry.GetQualifiedKeywordId(modId, localKeywordStem);

            return new(
                id,
                "card_keywords",
                $"{id}.title",
                "card_keywords",
                $"{id}.description",
                iconPath,
                cardDescriptionPlacement,
                includeInCardHoverTip);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an owned <c>card_keywords</c> entry using the legacy hover-tip defaults.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用旧版悬停提示默认值创建归属当前模组的 <c>card_keywords</c> 条目。
        ///     </para>
        /// </summary>
        public static KeywordRegistrationEntry OwnedCardByLocNamespace(
            string modId,
            string localKeywordStem,
            string? iconPath = null)
        {
            return OwnedCardByLocNamespace(
                modId,
                localKeywordStem,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a <c>card_keywords</c> entry from a raw global ID and localization-key stem.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         根据未经限定的全局 ID 和本地化键前缀创建 <c>card_keywords</c> 条目。
        ///     </para>
        /// </summary>
        [Obsolete(
            "Prefer OwnedCardByLocNamespace(modId, localKeywordStem, ...) so the keyword id is mod-qualified like fixed model entries; flat ids collide globally.")]
        public static KeywordRegistrationEntry Card(
            string id,
            string entryStem,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            return new(
                id,
                "card_keywords",
                $"{entryStem}.title",
                "card_keywords",
                $"{entryStem}.description",
                iconPath,
                cardDescriptionPlacement,
                includeInCardHoverTip);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a raw-ID <c>card_keywords</c> entry through the factory signature retained for older mods.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过为兼容旧版模组而保留的工厂签名，创建使用原始 ID 的 <c>card_keywords</c> 条目。
        ///     </para>
        /// </summary>
        [Obsolete(
            "Prefer OwnedCardByLocNamespace(modId, localKeywordStem, ...) so the keyword id is mod-qualified like fixed model entries; flat ids collide globally.")]
        public static KeywordRegistrationEntry Card(string id, string entryStem, string? iconPath = null)
        {
            return Card(
                id,
                entryStem,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }
    }
}
