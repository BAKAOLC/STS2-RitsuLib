using MegaCrit.Sts2.Core.Helpers;
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
        ///     <c>card_keywords</c> row with an id from <see cref="ModContentRegistry.GetQualifiedKeywordId" />.
        /// </summary>
        public static KeywordRegistrationEntry OwnedCardByLocNamespace(
            string modId,
            string localKeywordStem,
            string? locNamespace,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            var ns = string.IsNullOrWhiteSpace(locNamespace)
                ? StringHelper.Slugify(modId)
                : locNamespace;

            var stem = StringHelper.Slugify(ns) + "_" + StringHelper.Slugify(localKeywordStem);
            var id = ModContentRegistry.GetQualifiedKeywordId(modId, localKeywordStem);

            return new(
                id,
                "card_keywords",
                $"{stem}.title",
                "card_keywords",
                $"{stem}.description",
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
            string? locNamespace = null,
            string? iconPath = null)
        {
            return OwnedCardByLocNamespace(
                modId,
                localKeywordStem,
                locNamespace,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     <c>card_keywords</c> row with an id from <see cref="ModContentRegistry.GetQualifiedKeywordId" />.
        /// </summary>
        [Obsolete(
            "Pitfall: locKeyPrefix is NOT a prefix that affects only the modid/namespace portion. It is the full card_keywords entry stem used to form '{stem}.title' and '{stem}.description'. Prefer OwnedCardByLocNamespace (default stem: '<modid>_<keyword>').")]
        public static KeywordRegistrationEntry OwnedCard(
            string modId,
            string localKeywordStem,
            string locKeyPrefix,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            var id = ModContentRegistry.GetQualifiedKeywordId(modId, localKeywordStem);
            return new(
                id,
                "card_keywords",
                $"{locKeyPrefix}.title",
                "card_keywords",
                $"{locKeyPrefix}.description",
                iconPath,
                cardDescriptionPlacement,
                includeInCardHoverTip);
        }

        /// <summary>
        ///     <c>OwnedCard</c> overload with legacy hover defaults.
        /// </summary>
        [Obsolete(
            "Pitfall: locKeyPrefix is NOT a prefix that affects only the modid/namespace portion. It is the full card_keywords entry stem used to form '{stem}.title' and '{stem}.description'. Prefer OwnedCardByLocNamespace (default stem: '<modid>_<keyword>').")]
        public static KeywordRegistrationEntry OwnedCard(
            string modId,
            string localKeywordStem,
            string locKeyPrefix,
            string? iconPath = null)
        {
            return OwnedCard(
                modId,
                localKeywordStem,
                locKeyPrefix,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     Builds a <c>card_keywords</c> entry (full factory signature).
        /// </summary>
        [Obsolete(
            "Prefer OwnedCard(modId, localKeywordStem, ...) so the keyword id is mod-qualified like fixed model entries; flat ids collide globally.")]
        public static KeywordRegistrationEntry Card(
            string id,
            string locKeyPrefix,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            return new(
                id,
                "card_keywords",
                $"{locKeyPrefix}.title",
                "card_keywords",
                $"{locKeyPrefix}.description",
                iconPath,
                cardDescriptionPlacement,
                includeInCardHoverTip);
        }

        /// <summary>
        ///     Legacy <c>Card</c> factory signature preserved for older mods.
        /// </summary>
        [Obsolete(
            "Prefer OwnedCard(modId, localKeywordStem, ...) so the keyword id is mod-qualified like fixed model entries; flat ids collide globally.")]
        public static KeywordRegistrationEntry Card(string id, string locKeyPrefix, string? iconPath = null)
        {
            return Card(
                id,
                locKeyPrefix,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }
    }
}
