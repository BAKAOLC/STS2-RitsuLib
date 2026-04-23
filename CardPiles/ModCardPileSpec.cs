namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Describes a mod card pile at registration time. Everything but the mod id and local stem is optional;
    ///     sensible defaults match the vanilla Draw / Discard / Exhaust button behaviour.
    /// </summary>
    /// <remarks>
    ///     Localization follows the vanilla pile convention — the hover-tip title / description and the
    ///     "open empty pile" thought bubble are always resolved against the built-in
    ///     <c>static_hover_tips</c> loc table, using the keys <c>"{LocStem}.title"</c>,
    ///     <c>"{LocStem}.description"</c> and <c>"{LocStem}.empty"</c>. Mods cannot create additional loc
    ///     tables, so all entries are expected to live in <c>static_hover_tips.json</c> merged through
    ///     the normal mod-localization pipeline.
    /// </remarks>
    public sealed record ModCardPileSpec
    {
        /// <summary>
        ///     Vanilla loc table used for every mod-card-pile hover tip. Mods can only *extend* this table
        ///     (not create new tables), so the pile subsystem always resolves into it.
        /// </summary>
        public const string HoverTipLocTable = "static_hover_tips";

        /// <summary>
        ///     Builds a spec with defaults suitable for a combat-only, bottom-left auto-stacking pile.
        /// </summary>
        public ModCardPileSpec()
        {
        }

        /// <summary>
        ///     Lifetime scope of the pile. Defaults to <see cref="ModCardPileScope.CombatOnly" />.
        /// </summary>
        public ModCardPileScope Scope { get; init; } = ModCardPileScope.CombatOnly;

        /// <summary>
        ///     Visual style family; drives which UI chrome is attached in combat. Defaults to
        ///     <see cref="ModCardPileUiStyle.Headless" /> (no UI button).
        /// </summary>
        public ModCardPileUiStyle Style { get; init; } = ModCardPileUiStyle.Headless;

        /// <summary>
        ///     Slot hint paired with <see cref="Style" />. When left at <see cref="ModCardPileAnchor.Default" />
        ///     the pile auto-stacks after other same-style piles in registration order.
        /// </summary>
        public ModCardPileAnchor Anchor { get; init; } = ModCardPileAnchor.Default;

        /// <summary>
        ///     Godot resource path for the pile's button icon (for example <c>res://art/my_pile.png</c>). When
        ///     null or missing the placeholder texture is used.
        /// </summary>
        public string? IconPath { get; init; }

        /// <summary>
        ///     Localization stem used to build the hover-tip title / description and the empty-pile message
        ///     (all resolved against <see cref="HoverTipLocTable" />). When null, the pile's normalized id
        ///     is used as the stem, mirroring how <c>ModKeywordRegistry</c> derives default loc keys.
        /// </summary>
        /// <remarks>
        ///     Expected JSON keys are <c>"{stem}.title"</c>, <c>"{stem}.description"</c> and
        ///     <c>"{stem}.empty"</c>. Follow the vanilla convention (see <c>DRAW_PILE.title</c> /
        ///     <c>DRAW_PILE.description</c> in <c>static_hover_tips.json</c>) when authoring translations.
        /// </remarks>
        public string? LocStem { get; init; }

        /// <summary>
        ///     Optional controller / keyboard hotkey ids that open the pile's view screen.
        /// </summary>
        public string[]? Hotkeys { get; init; }

        /// <summary>
        ///     When true, cards added to the pile are displayed as <c>NCard</c> nodes inside the pile's UI
        ///     container (only meaningful for <see cref="ModCardPileUiStyle.ExtraHand" />).
        /// </summary>
        public bool CardShouldBeVisible { get; init; }

        /// <summary>
        ///     Optional callback invoked when the pile's UI button is released. When null (the default) the
        ///     button falls back to <c>NCardPileScreen.ShowScreen</c> — the same behaviour as vanilla Draw /
        ///     Discard / Exhaust buttons. Supply a delegate to plug in a custom
        ///     <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Capstones.ICapstoneScreen" />, inspect the pile, or
        ///     do nothing at all.
        /// </summary>
        /// <remarks>
        ///     The context exposes helpers — <see cref="ModCardPileOpenContext.ShowDefaultPileScreen" /> runs
        ///     the default behaviour, while <see cref="ModCardPileOpenContext.OpenCapstoneScreen" /> mounts a
        ///     custom screen through <c>NCapstoneContainer</c>. The callback is *not* invoked when the pile is
        ///     empty; in that case the empty-pile thought bubble is shown and re-clicking an already-open
        ///     default pile screen continues to toggle it closed before the callback runs.
        /// </remarks>
        public Action<ModCardPileOpenContext>? OnOpen { get; init; }
    }
}
