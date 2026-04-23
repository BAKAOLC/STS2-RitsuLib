using System.Numerics;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     Declarative spec for a mod-owned top-bar button. Mirrors <c>ModCardPileSpec</c>'s structure but is
    ///     fully decoupled from card piles — the only contract is "show a button next to the vanilla deck
    ///     button, hover-tip via <c>static_hover_tips</c>, click runs a callback".
    /// </summary>
    /// <remarks>
    ///     Localization follows the same <c>static_hover_tips.{LocStem}.title</c> / <c>.description</c>
    ///     convention used by <c>ModCardPileSpec</c>; <see cref="LocStem" /> defaults to the registered id
    ///     (<c>MODID_TOPBARBUTTON_STEM</c>) when left null.
    /// </remarks>
    public sealed record ModTopBarButtonSpec
    {
        /// <summary>Vanilla loc table that the button's hover-tip resolves against.</summary>
        public const string HoverTipLocTable = ModTopBarButtonLocConstants.HoverTipLocTable;

        /// <summary>Godot resource path for the button icon (for example <c>res://my_mod/icon.png</c>).</summary>
        public string? IconPath { get; init; }

        /// <summary>
        ///     Stem used to build <c>{LocStem}.title</c> / <c>{LocStem}.description</c> under
        ///     <c>static_hover_tips</c>. Defaults to the registered id (<c>MODID_TOPBARBUTTON_STEM</c>)
        ///     when left null.
        /// </summary>
        public string? LocStem { get; init; }

        /// <summary>
        ///     Sort order within a single mod's top-bar buttons; lower values render closer to the vanilla
        ///     deck button.
        /// </summary>
        public int Order { get; init; }

        /// <summary>
        ///     Extra pixel offset applied on top of the auto-stacked slot position. Use this for
        ///     fine-tuning when the default horizontal stacking layout doesn't match your icon.
        /// </summary>
        public Vector2 Offset { get; init; }

        /// <summary>
        ///     Required click handler. Receives a <see cref="ModTopBarButtonContext" /> that exposes
        ///     <see cref="ModTopBarButtonContext.OpenCapstoneScreen" /> / related helpers.
        /// </summary>
        public Action<ModTopBarButtonContext>? OnClick { get; init; }

        /// <summary>
        ///     Optional predicate that decides whether the button is visible for the current player. When
        ///     null the button is always visible (once the top bar exists). Evaluated on
        ///     <see cref="Godot.Node._Process" /> to keep the visibility state in sync with combat/run state
        ///     changes.
        /// </summary>
        public Func<ModTopBarButtonContext, bool>? VisibleWhen { get; init; }

        /// <summary>
        ///     Optional predicate used by the button to decide when it should render in its "screen open"
        ///     rocking state (mirroring vanilla <c>NTopBarDeckButton</c> / <c>NTopBarMapButton</c>). Typical
        ///     usage is
        ///     <c>ctx =&gt; ModScreenService.CurrentCapstoneScreen is MyScreen</c>. When null the button never
        ///     enters the open state — pick this for one-shot "fire and forget" click handlers.
        /// </summary>
        public Func<ModTopBarButtonContext, bool>? IsOpenWhen { get; init; }

        /// <summary>
        ///     Optional provider used to populate the count badge under the button's icon. Called on
        ///     <see cref="Godot.Node._Process" /> — keep it cheap. When null, the count label is hidden
        ///     entirely, which is the right choice for fire-and-forget action buttons (menus, toggles).
        ///     When set, the button reuses the vanilla card-pile "number jumped up" bump animation so the
        ///     feedback feels identical to the player's deck / draw / discard counts.
        /// </summary>
        public Func<ModTopBarButtonContext, int>? CountProvider { get; init; }
    }

    /// <summary>Shared constants for the top-bar-button localization convention.</summary>
    internal static class ModTopBarButtonLocConstants
    {
        public const string HoverTipLocTable = "static_hover_tips";
    }
}
