using STS2RitsuLib.Content;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Declarative keyword row for content packs: register with a <see cref="ModKeywordRegistry" /> in one call.
    ///     content pack 使用的声明式 keyword 行：一次调用即可通过 <see cref="ModKeywordRegistry" /> 注册。
    /// </summary>
    public sealed record KeywordRegistrationEntry
    {
        /// <summary>
        ///     Full constructor including placement and hover-tip flags.
        ///     包含 placement 与 hover-tip flag 的完整构造函数。
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
        ///     Legacy constructor signature (six CLR parameters) preserved for older mods.
        ///     为旧 mod 保留的 legacy 构造函数签名（六个 CLR 参数）。
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
        ///     Keyword id (normalized on register).
        ///     keyword id（注册时 normalized）。
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        ///     Title localization table.
        ///     title 本地化 table。
        /// </summary>
        public string TitleTable { get; init; } = string.Empty;

        /// <summary>
        ///     Title localization key.
        ///     标题本地化键。
        /// </summary>
        public string TitleKey { get; init; } = string.Empty;

        /// <summary>
        ///     Description localization table.
        ///     description 本地化 table。
        /// </summary>
        public string DescriptionTable { get; init; } = string.Empty;

        /// <summary>
        ///     Description localization key.
        ///     描述本地化键。
        /// </summary>
        public string DescriptionKey { get; init; } = string.Empty;

        /// <summary>
        ///     Optional icon resource path.
        ///     可选图标ResourcePath。
        /// </summary>
        public string? IconPath { get; init; }

        /// <summary>
        ///     Inline card-description injection placement.
        ///     内联卡牌描述注入位置。
        /// </summary>
        public ModKeywordCardDescriptionPlacement CardDescriptionPlacement { get; init; } =
            ModKeywordCardDescriptionPlacement.None;

        /// <summary>
        ///     Whether this id participates in template keyword hover-tip expansion.
        ///     此 id 是否参与 template keyword hover-tip 扩展。
        /// </summary>
        public bool IncludeInCardHoverTip { get; init; }

        /// <summary>
        ///     Registers this entry on <paramref name="registry" />.
        ///     将此条目注册到 <paramref name="registry" />。
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
        ///     <c>card_keywords</c> row: id and loc stem both from <see cref="ModContentRegistry.GetQualifiedKeywordId" />.
        ///     <see cref="ModContentRegistry.GetQualifiedKeywordId" />。
        ///     <c>card_keywords</c> 行：id 与本地化词干都来自 <see cref="ModContentRegistry.GetQualifiedKeywordId" />。
        ///     <see cref="ModContentRegistry.GetQualifiedKeywordId" />。
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
        ///     <c>OwnedCardByLocNamespace</c> overload with legacy hover defaults.
        ///     使用 legacy hover 默认值的 <c>OwnedCardByLocNamespace</c> 重载。
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
        ///     Builds a <c>card_keywords</c> entry (full factory signature).
        ///     构建 <c>card_keywords</c> 条目（完整工厂签名）。
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
        ///     Legacy <c>Card</c> factory signature preserved for older mods.
        ///     为旧 mod 保留的 legacy <c>Card</c> 工厂签名。
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
