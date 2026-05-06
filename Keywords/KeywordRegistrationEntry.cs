using STS2RitsuLib.Content;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Declarative keyword row for content packs: register with a <see cref="ModKeywordRegistry" /> in one call.
    /// </summary>
    public sealed record KeywordRegistrationEntry
    {
        /// <summary>
        ///     Full constructor including placement and hover-tip flags.
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
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        ///     Title localization table.
        /// </summary>
        public string TitleTable { get; init; } = string.Empty;

        /// <summary>
        ///     Title localization key.
        /// </summary>
        public string TitleKey { get; init; } = string.Empty;

        /// <summary>
        ///     Description localization table.
        /// </summary>
        public string DescriptionTable { get; init; } = string.Empty;

        /// <summary>
        ///     Description localization key.
        /// </summary>
        public string DescriptionKey { get; init; } = string.Empty;

        /// <summary>
        ///     Optional icon resource path.
        /// </summary>
        public string? IconPath { get; init; }

        /// <summary>
        ///     Inline card-description injection placement.
        /// </summary>
        public ModKeywordCardDescriptionPlacement CardDescriptionPlacement { get; init; } =
            ModKeywordCardDescriptionPlacement.None;

        /// <summary>
        ///     Whether this id participates in template keyword hover-tip expansion.
        /// </summary>
        public bool IncludeInCardHoverTip { get; init; }

        /// <summary>
        ///     Registers this entry on <paramref name="registry" />.
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
