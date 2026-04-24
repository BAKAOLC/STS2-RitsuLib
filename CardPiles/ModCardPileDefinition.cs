using Godot;
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
    ///     (<c>static_hover_tips</c>) using the keys <c>"{Id}.title"</c>, <c>"{Id}.description"</c> and
    ///     <c>"{Id}.empty"</c> (same as the registered pile id).
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
        /// <param name="hotkeys">Optional hotkey ids for the pile button.</param>
        /// <param name="cardShouldBeVisible">Whether cards render as <c>NCard</c> nodes inside the pile container.</param>
        /// <param name="onOpen">
        ///     Optional callback invoked when the pile's UI button is released (see <see cref="OnOpen" />).
        /// </param>
        /// <param name="hoverTipScreenOffset">
        ///     Added to the hover tip position after automatic placement (see <see cref="HoverTipScreenOffset" />).
        /// </param>
        /// <param name="hoverTipPlacement">
        ///     How the hover tip is anchored to the pile button (see <see cref="HoverTipPlacement" />).
        /// </param>
        /// <param name="visibleWhen">
        ///     Optional visibility predicate (see <see cref="VisibleWhen" />). Null means always visible on the
        ///     button node (subject to parent visibility).
        /// </param>
        public ModCardPileDefinition(
            string modId,
            string id,
            PileType pileType,
            ModCardPileScope scope,
            ModCardPileUiStyle style,
            ModCardPileAnchor anchor,
            string? iconPath,
            string[]? hotkeys,
            bool cardShouldBeVisible,
            Action<ModCardPileOpenContext>? onOpen,
            Vector2 hoverTipScreenOffset,
            ModCardPileHoverTipPlacement hoverTipPlacement,
            Func<ModCardPileVisibilityContext, bool>? visibleWhen)
        {
            ModId = modId;
            Id = id;
            PileType = pileType;
            Scope = scope;
            Style = style;
            Anchor = anchor;
            IconPath = iconPath;
            Hotkeys = hotkeys;
            CardShouldBeVisible = cardShouldBeVisible;
            OnOpen = onOpen;
            HoverTipScreenOffset = hoverTipScreenOffset;
            HoverTipPlacement = hoverTipPlacement;
            VisibleWhen = visibleWhen;
        }

        /// <summary>
        ///     Compatibility overload that omitted <see cref="VisibleWhen" />; forwards with null.
        /// </summary>
        public ModCardPileDefinition(
            string modId,
            string id,
            PileType pileType,
            ModCardPileScope scope,
            ModCardPileUiStyle style,
            ModCardPileAnchor anchor,
            string? iconPath,
            string[]? hotkeys,
            bool cardShouldBeVisible,
            Action<ModCardPileOpenContext>? onOpen,
            Vector2 hoverTipScreenOffset,
            ModCardPileHoverTipPlacement hoverTipPlacement)
            : this(modId, id, pileType, scope, style, anchor, iconPath, hotkeys, cardShouldBeVisible, onOpen,
                hoverTipScreenOffset, hoverTipPlacement, null)
        {
        }

        /// <summary>
        ///     Compatibility overload that omitted <see cref="HoverTipPlacement" />; forwards with
        ///     <see cref="ModCardPileHoverTipPlacement.Auto" />.
        /// </summary>
        public ModCardPileDefinition(
            string modId,
            string id,
            PileType pileType,
            ModCardPileScope scope,
            ModCardPileUiStyle style,
            ModCardPileAnchor anchor,
            string? iconPath,
            string[]? hotkeys,
            bool cardShouldBeVisible,
            Action<ModCardPileOpenContext>? onOpen,
            Vector2 hoverTipScreenOffset)
            : this(modId, id, pileType, scope, style, anchor, iconPath, hotkeys, cardShouldBeVisible, onOpen,
                hoverTipScreenOffset, ModCardPileHoverTipPlacement.Auto)
        {
        }

        /// <summary>
        ///     Compatibility overload matching the historical call shape that omitted
        ///     <see cref="OnOpen" />; forwards with a null <see cref="OnOpen" />,
        ///     <see cref="Vector2.Zero" /> for <see cref="HoverTipScreenOffset" />, and
        ///     <see cref="ModCardPileHoverTipPlacement.Auto" /> for <see cref="HoverTipPlacement" />.
        /// </summary>
        public ModCardPileDefinition(
            string modId,
            string id,
            PileType pileType,
            ModCardPileScope scope,
            ModCardPileUiStyle style,
            ModCardPileAnchor anchor,
            string? iconPath,
            string[]? hotkeys,
            bool cardShouldBeVisible)
            : this(modId, id, pileType, scope, style, anchor, iconPath, hotkeys, cardShouldBeVisible, null,
                default, ModCardPileHoverTipPlacement.Auto)
        {
        }

        /// <summary>
        ///     Compatibility overload for the historical shape before
        ///     <see cref="HoverTipScreenOffset" /> and <see cref="HoverTipPlacement" />; forwards with
        ///     <see cref="Vector2.Zero" /> and <see cref="ModCardPileHoverTipPlacement.Auto" />.
        /// </summary>
        public ModCardPileDefinition(
            string modId,
            string id,
            PileType pileType,
            ModCardPileScope scope,
            ModCardPileUiStyle style,
            ModCardPileAnchor anchor,
            string? iconPath,
            string[]? hotkeys,
            bool cardShouldBeVisible,
            Action<ModCardPileOpenContext>? onOpen)
            : this(modId, id, pileType, scope, style, anchor, iconPath, hotkeys, cardShouldBeVisible, onOpen,
                default, ModCardPileHoverTipPlacement.Auto)
        {
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
        ///     Hover-tip title resolved against <see cref="ModCardPileSpec.HoverTipLocTable" /> with key
        ///     <c>"{Id}.title"</c>.
        /// </summary>
        public LocString Title => new(ModCardPileSpec.HoverTipLocTable, $"{Id}.title");

        /// <summary>
        ///     Hover-tip description resolved against <see cref="ModCardPileSpec.HoverTipLocTable" /> with
        ///     key <c>"{Id}.description"</c>.
        /// </summary>
        public LocString Description => new(ModCardPileSpec.HoverTipLocTable, $"{Id}.description");

        /// <summary>
        ///     Message displayed when the pile is opened while empty; resolved against
        ///     <see cref="ModCardPileSpec.HoverTipLocTable" /> with key <c>"{Id}.empty"</c>.
        /// </summary>
        public LocString EmptyPileMessage => new(ModCardPileSpec.HoverTipLocTable, $"{Id}.empty");

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

        /// <summary>
        ///     Extra pixels added to the hover tip position (see <see cref="ModCardPileSpec.HoverTipScreenOffset" />).
        /// </summary>
        public Vector2 HoverTipScreenOffset { get; }

        /// <summary>
        ///     How the hover tip is anchored to the pile button (see <see cref="ModCardPileSpec.HoverTipPlacement" />).
        /// </summary>
        public ModCardPileHoverTipPlacement HoverTipPlacement { get; }

        /// <summary>
        ///     When non-null, the pile button evaluates this on <c>_Process</c> and hides itself when the
        ///     predicate returns false (see <see cref="ModCardPileSpec.VisibleWhen" />).
        /// </summary>
        public Func<ModCardPileVisibilityContext, bool>? VisibleWhen { get; }
    }
}
