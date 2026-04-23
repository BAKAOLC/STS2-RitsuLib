using STS2RitsuLib.CardPiles;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     Declaratively registers a mod card pile (see <see cref="ModCardPileRegistry" />). Place on any
    ///     concrete class inside your mod assembly; the type itself acts as the registration carrier and
    ///     can optionally implement <see cref="IModCardPileHandler" /> to customise button-click behaviour.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Field semantics mirror <see cref="ModCardPileSpec" />. Localization follows the vanilla
    ///         pile convention — hover-tip title / description and the empty-pile thought bubble are all
    ///         resolved against <see cref="ModCardPileSpec.HoverTipLocTable" /> using the keys
    ///         <c>"{LocStem}.title"</c>, <c>"{LocStem}.description"</c> and <c>"{LocStem}.empty"</c>. Because
    ///         mods can only extend existing loc tables (not create new ones) the table itself is not
    ///         configurable; author your translations in <c>static_hover_tips.json</c>.
    ///     </para>
    ///     <para>
    ///         Anchor values split across <see cref="AnchorKind" /> plus optional
    ///         <see cref="AnchorOffsetX" /> / <see cref="AnchorOffsetY" /> /
    ///         <see cref="AnchorCustomX" /> / <see cref="AnchorCustomY" />; when <see cref="AnchorKind" />
    ///         is left at <see cref="ModCardPileAnchorKind.StyleDefault" /> the pile auto-stacks per the
    ///         "explicit anchor + auto-stack fallback" rule.
    ///     </para>
    ///     <para>
    ///         If the annotated type implements <see cref="IModCardPileHandler" />, ritsulib creates a
    ///         single instance (parameterless constructor required) and wires its
    ///         <see cref="IModCardPileHandler.OnOpen" /> method into
    ///         <see cref="ModCardPileSpec.OnOpen" />.
    ///     </para>
    /// </remarks>
    /// <param name="localPileStem">Local, mod-scoped pile stem (matches <c>RegisterOwned(localStem, ...)</c>).</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOwnedCardPileAttribute(string localPileStem) : AutoRegistrationAttribute
    {
        /// <summary>Local, mod-scoped pile stem.</summary>
        public string LocalPileStem { get; } = localPileStem;

        /// <summary>Lifetime scope (defaults to <see cref="ModCardPileScope.CombatOnly" />).</summary>
        public ModCardPileScope Scope { get; set; } = ModCardPileScope.CombatOnly;

        /// <summary>UI chrome family (defaults to <see cref="ModCardPileUiStyle.Headless" />).</summary>
        public ModCardPileUiStyle Style { get; set; } = ModCardPileUiStyle.Headless;

        /// <summary>Anchor slot hint (defaults to <see cref="ModCardPileAnchorKind.StyleDefault" />).</summary>
        public ModCardPileAnchorKind AnchorKind { get; set; } = ModCardPileAnchorKind.StyleDefault;

        /// <summary>Extra X pixels added on top of the resolved anchor position.</summary>
        public float AnchorOffsetX { get; set; }

        /// <summary>Extra Y pixels added on top of the resolved anchor position.</summary>
        public float AnchorOffsetY { get; set; }

        /// <summary>Absolute X used only when <see cref="AnchorKind" /> is <see cref="ModCardPileAnchorKind.Custom" />.</summary>
        public float AnchorCustomX { get; set; }

        /// <summary>Absolute Y used only when <see cref="AnchorKind" /> is <see cref="ModCardPileAnchorKind.Custom" />.</summary>
        public float AnchorCustomY { get; set; }

        /// <summary>
        ///     Godot resource path for the pile icon (e.g. <c>res://my_mod/icons/my_pile.png</c>).
        /// </summary>
        public string? IconPath { get; set; }

        /// <summary>
        ///     Optional localization stem (see <see cref="ModCardPileSpec.LocStem" />). When null, the
        ///     normalized pile id is used as the stem — mirroring <c>ModKeywordRegistry</c>'s default.
        /// </summary>
        public string? LocStem { get; set; }

        /// <summary>
        ///     Optional hotkey ids forwarded to <c>NCardPileScreen.ShowScreen</c>. Separate ids with commas
        ///     at the call site (<c>Hotkeys = new[] { "combat_pile_deck" }</c>).
        /// </summary>
        public string[]? Hotkeys { get; set; }

        /// <summary>
        ///     Only meaningful for <see cref="ModCardPileUiStyle.ExtraHand" />: when true, cards added to
        ///     the pile are rendered as <c>NCard</c> nodes inside the pile container.
        /// </summary>
        public bool CardShouldBeVisible { get; set; }
    }
}
