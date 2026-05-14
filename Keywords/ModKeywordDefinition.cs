using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Immutable registration data for a mod keyword (localization tables, keys, optional icon).
    ///     mod 关键词的不可变注册数据（本地化表、键、可选图标）。
    /// </summary>
    public sealed record ModKeywordDefinition
    {
        /// <summary>
        ///     Original binary-compatible constructor (seven CLR parameters); prior RitsuLib keyword definitions.
        ///     原始二进制兼容构造函数（七个 CLR 参数）；用于旧版 RitsuLib keyword definition。
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
        ///     扩展构造函数：与旧七参数 ABI 相同，并额外包含 placement 与 hover-tip inclusion。
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
        ///     所属 mod manifest id。
        /// </summary>
        public string ModId { get; init; } = string.Empty;

        /// <summary>
        ///     Normalized keyword id (lowercase).
        ///     normalized keyword id（小写）。
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        ///     Localization table for the title.
        ///     title 使用的本地化 table。
        /// </summary>
        public string TitleTable { get; init; } = string.Empty;

        /// <summary>
        ///     Key for the title string.
        ///     标题字符串使用的键。
        /// </summary>
        public string TitleKey { get; init; } = string.Empty;

        /// <summary>
        ///     Localization table for the body text.
        ///     body text 使用的本地化 table。
        /// </summary>
        public string DescriptionTable { get; init; } = string.Empty;

        /// <summary>
        ///     Key for the description string.
        ///     描述字符串使用的键。
        /// </summary>
        public string DescriptionKey { get; init; } = string.Empty;

        /// <summary>
        ///     Optional Godot resource path for hover icon.
        ///     hover icon 使用的可选 Godot ResourcePath。
        /// </summary>
        public string? IconPath { get; init; }

        /// <summary>
        ///     Whether and where to inject keyword BBCode into card descriptions.
        ///     是否以及在哪里将关键词 BBCode 注入卡牌描述。
        /// </summary>
        public ModKeywordCardDescriptionPlacement CardDescriptionPlacement { get; init; } =
            ModKeywordCardDescriptionPlacement.None;

        /// <summary>
        ///     When true, this keyword’s hover tip is included from <c>RegisteredKeywordIds</c> / runtime mod-keyword sets
        ///     on cards and other mod templates.
        ///     为 true 时，此关键词的悬停提示会从 <c>RegisteredKeywordIds</c>、
        ///     运行时 mod 关键词集合
        ///     包含到卡牌和其它 mod 模板上。
        /// </summary>
        public bool IncludeInCardHoverTip { get; init; }

        /// <summary>
        ///     Deterministic <see cref="CardKeyword" /> value minted for this keyword (hash of <see cref="Id" />,
        ///     forced above the vanilla enum range). Stored directly inside <c>CardModel.Keywords</c> so the mod
        ///     keyword rides vanilla workflows (lookups, cloning, canonical seeding, per-run saves) without any
        ///     parallel side-loaded state. Populated by <see cref="ModKeywordRegistry" /> at registration time;
        ///     remains <see cref="CardKeyword.None" /> for definitions constructed outside the registry.
        ///     <see cref="CardKeyword.None" />。
        ///     为此关键词铸造的确定性 <see cref="CardKeyword" /> 值（<see cref="Id" /> 的 hash，
        ///     强制高于原版 enum 范围）。它会直接存入 <c>CardModel.Keywords</c>，使 mod
        ///     关键词沿用原版流程（查找、克隆、规范种入、逐跑局保存），无需任何
        ///     并行 side-loaded 状态。由 <see cref="ModKeywordRegistry" /> 在注册时填充；
        ///     在注册表外构造的定义保持 <see cref="CardKeyword.None" />。
        ///     <see cref="CardKeyword.None" />。
        /// </summary>
        public CardKeyword CardKeywordValue { get; init; } = CardKeyword.None;
    }
}
