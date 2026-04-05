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
            string? IconPath)
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
    }
}
