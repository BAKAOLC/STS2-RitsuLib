using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Immutable registry entry for a mod card pile. Produced by <see cref="ModCardPileRegistry" /> and keyed
    ///     by both the normalized id and the deterministically minted <see cref="PileType" /> value.
    /// </summary>
    /// <remarks>
    ///     Localization follows the vanilla pile convention: the hover-tip title / description and
    ///     empty-pile message are always resolved against <see cref="ModCardPileSpec.HoverTipLocTable" />
    ///     (<c>static_hover_tips</c>) using the keys <c>"{LocStem}.title"</c>,
    ///     <c>"{LocStem}.description"</c> and <c>"{LocStem}.empty"</c>. See
    ///     <see cref="ModCardPileSpec.LocStem" /> for the authoring contract.
    /// </remarks>
    public sealed record ModCardPileDefinition
    {
        /// <summary>
        ///     Primary constructor used by the registry; all fields are immutable once registered.
        /// </summary>
        /// <param name="modId">Owning mod id (<c>com.example.my-mod</c>).</param>
        /// <param name="id">Normalized global id (<c>NormalizeId</c> output from <see cref="ModCardPileRegistry" />).</param>
        /// <param name="pileType">Minted <see cref="PileType" /> value that represents this pile at runtime.</param>
        /// <param name="scope">Lifetime scope.</param>
        /// <param name="style">UI chrome style.</param>
        /// <param name="anchor">UI slot hint.</param>
        /// <param name="iconPath">Optional Godot resource path for the pile icon.</param>
        /// <param name="locStem">
        ///     Localization stem used to build title / description / empty-pile keys in
        ///     <see cref="ModCardPileSpec.HoverTipLocTable" />. Null means "use the normalized id as stem".
        /// </param>
        /// <param name="hotkeys">Optional hotkey ids for the pile button.</param>
        /// <param name="cardShouldBeVisible">Whether cards render as <c>NCard</c> nodes inside the pile container.</param>
        /// <param name="onOpen">
        ///     Optional callback invoked when the pile's UI button is released (see <see cref="OnOpen" />).
        /// </param>
        public ModCardPileDefinition(
            string modId,
            string id,
            PileType pileType,
            ModCardPileScope scope,
            ModCardPileUiStyle style,
            ModCardPileAnchor anchor,
            string? iconPath,
            string? locStem,
            string[]? hotkeys,
            bool cardShouldBeVisible,
            Action<ModCardPileOpenContext>? onOpen = null)
        {
            ModId = modId;
            Id = id;
            PileType = pileType;
            Scope = scope;
            Style = style;
            Anchor = anchor;
            IconPath = iconPath;
            LocStem = string.IsNullOrWhiteSpace(locStem) ? id : locStem;
            Hotkeys = hotkeys;
            CardShouldBeVisible = cardShouldBeVisible;
            OnOpen = onOpen;
        }

        /// <summary>
        ///     Owning mod id.
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     Normalized global id (trimmed).
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Deterministically minted <see cref="PileType" /> value.
        /// </summary>
        public PileType PileType { get; }

        /// <summary>
        ///     Lifetime scope declared at registration.
        /// </summary>
        public ModCardPileScope Scope { get; }

        /// <summary>
        ///     UI chrome style.
        /// </summary>
        public ModCardPileUiStyle Style { get; }

        /// <summary>
        ///     UI slot hint.
        /// </summary>
        public ModCardPileAnchor Anchor { get; }

        /// <summary>
        ///     Icon resource path (<c>res://...</c>); null falls back to a placeholder icon.
        /// </summary>
        public string? IconPath { get; }

        /// <summary>
        ///     Localization stem (never null; defaults to <see cref="Id" /> when the spec left it unset).
        ///     Combined with <see cref="ModCardPileSpec.HoverTipLocTable" /> to produce title / description
        ///     / empty-pile keys.
        /// </summary>
        public string LocStem { get; }

        /// <summary>
        ///     Hover-tip title resolved against <see cref="ModCardPileSpec.HoverTipLocTable" /> with key
        ///     <c>"{LocStem}.title"</c>.
        /// </summary>
        public LocString Title => new(ModCardPileSpec.HoverTipLocTable, $"{LocStem}.title");

        /// <summary>
        ///     Hover-tip description resolved against <see cref="ModCardPileSpec.HoverTipLocTable" /> with
        ///     key <c>"{LocStem}.description"</c>.
        /// </summary>
        public LocString Description => new(ModCardPileSpec.HoverTipLocTable, $"{LocStem}.description");

        /// <summary>
        ///     Message displayed when the pile is opened while empty; resolved against
        ///     <see cref="ModCardPileSpec.HoverTipLocTable" /> with key <c>"{LocStem}.empty"</c>.
        /// </summary>
        public LocString EmptyPileMessage => new(ModCardPileSpec.HoverTipLocTable, $"{LocStem}.empty");

        /// <summary>
        ///     Hotkey ids (see <c>MegaInput</c>) forwarded to <c>NCardPileScreen.ShowScreen</c>.
        /// </summary>
        public string[]? Hotkeys { get; }

        /// <summary>
        ///     When true, the pile renders cards as <c>NCard</c> nodes (only meaningful for
        ///     <see cref="ModCardPileUiStyle.ExtraHand" />).
        /// </summary>
        public bool CardShouldBeVisible { get; }

        /// <summary>
        ///     Handler invoked when the pile's UI button is released. Null means "use the default
        ///     <c>NCardPileScreen</c>". See <see cref="ModCardPileSpec.OnOpen" /> for the full contract.
        /// </summary>
        public Action<ModCardPileOpenContext>? OnOpen { get; }
    }
}
