using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Immutable registration data for a mod keyword (localization tables, keys, optional icon).
    /// </summary>
    public sealed record ModKeywordDefinition
    {
        /// <summary>
        ///     Original binary-compatible constructor (seven CLR parameters); prior RitsuLib keyword definitions.
        /// </summary>
        public ModKeywordDefinition(
            string ModId,
            string Id,
            string TitleTable,
            string TitleKey,
            string DescriptionTable,
            string DescriptionKey,
            string? IconPath = null)
        {
            this.ModId = ModId;
            this.Id = Id;
            this.TitleTable = TitleTable;
            this.TitleKey = TitleKey;
            this.DescriptionTable = DescriptionTable;
            this.DescriptionKey = DescriptionKey;
            this.IconPath = IconPath;
            CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None;
            IncludeInCardHoverTip = true;
        }

        /// <summary>
        ///     Extended constructor: same as the legacy seven-parameter ABI plus placement and hover-tip inclusion.
        /// </summary>
        public ModKeywordDefinition(
            string ModId,
            string Id,
            string TitleTable,
            string TitleKey,
            string DescriptionTable,
            string DescriptionKey,
            string? IconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            this.ModId = ModId;
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
        ///     Owning mod manifest id.
        /// </summary>
        public string ModId { get; init; } = string.Empty;

        /// <summary>
        ///     Normalized keyword id (lowercase).
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        ///     Localization table for the title.
        /// </summary>
        public string TitleTable { get; init; } = string.Empty;

        /// <summary>
        ///     Key for the title string.
        /// </summary>
        public string TitleKey { get; init; } = string.Empty;

        /// <summary>
        ///     Localization table for the body text.
        /// </summary>
        public string DescriptionTable { get; init; } = string.Empty;

        /// <summary>
        ///     Key for the description string.
        /// </summary>
        public string DescriptionKey { get; init; } = string.Empty;

        /// <summary>
        ///     Optional Godot resource path for hover icon.
        /// </summary>
        public string? IconPath { get; init; }

        /// <summary>
        ///     Whether and where to inject keyword BBCode into card descriptions.
        /// </summary>
        public ModKeywordCardDescriptionPlacement CardDescriptionPlacement { get; init; } =
            ModKeywordCardDescriptionPlacement.None;

        /// <summary>
        ///     When true, this keyword’s hover tip is included from <c>RegisteredKeywordIds</c> / runtime mod-keyword sets
        ///     on cards and other mod templates.
        /// </summary>
        public bool IncludeInCardHoverTip { get; init; }

        /// <summary>
        ///     Deterministic <see cref="CardKeyword" /> value minted for this keyword (hash of <see cref="Id" />,
        ///     forced above the vanilla enum range). Stored directly inside <c>CardModel.Keywords</c> so the mod
        ///     keyword rides vanilla workflows (lookups, cloning, canonical seeding, per-run saves) without any
        ///     parallel side-loaded state. Populated by <see cref="ModKeywordRegistry" /> at registration time;
        ///     remains <see cref="CardKeyword.None" /> for definitions constructed outside the registry.
        /// </summary>
        public CardKeyword CardKeywordValue { get; init; } = CardKeyword.None;
    }
}
