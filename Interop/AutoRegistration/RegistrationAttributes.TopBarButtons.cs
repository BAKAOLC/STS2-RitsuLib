using STS2RitsuLib.TopBar;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     Declaratively registers a mod-owned top-bar button (see
    ///     <see cref="ModTopBarButtonRegistry" />). Mirrors the ergonomics of
    ///     <see cref="RegisterOwnedCardPileAttribute" /> but targets the generic, pile-independent
    ///     top-bar button system.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Place on any concrete class inside your mod assembly. The annotated class must implement
    ///         <see cref="IModTopBarButtonHandler" /> — ritsulib instantiates it once (parameterless
    ///         constructor required) and wires
    ///         <see cref="IModTopBarButtonHandler.OnClick" /> into <see cref="ModTopBarButtonSpec.OnClick" />
    ///         and <see cref="IModTopBarButtonHandler.IsVisible" /> into
    ///         <see cref="ModTopBarButtonSpec.VisibleWhen" />.
    ///     </para>
    ///     <para>
    ///         Localization follows the vanilla loc-table convention: title / description are resolved
    ///         against <c>static_hover_tips</c> using the keys <c>"{LocStem}.title"</c> and
    ///         <c>"{LocStem}.description"</c>. When <see cref="LocStem" /> is null the registered id
    ///         (<c>MYMOD_TOPBARBUTTON_STEM</c>) is used as the stem.
    ///     </para>
    /// </remarks>
    /// <param name="localButtonStem">
    ///     Local, mod-scoped stem (matches <c>RegisterOwned(localStem, ...)</c>).
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOwnedTopBarButtonAttribute(string localButtonStem) : AutoRegistrationAttribute
    {
        /// <summary>Local, mod-scoped button stem.</summary>
        public string LocalButtonStem { get; } = localButtonStem;

        /// <summary>Godot resource path for the icon (e.g. <c>res://my_mod/icons/recipes.png</c>).</summary>
        public string? IconPath { get; set; }

        /// <summary>
        ///     Optional localization stem (see <see cref="ModTopBarButtonSpec.LocStem" />). Defaults to
        ///     the registered id.
        /// </summary>
        public string? LocStem { get; set; }

        /// <summary>Sort order within this mod's top-bar buttons.</summary>
        public int ButtonOrder { get; set; }

        /// <summary>Extra pixel offset X on top of the auto-stacked slot.</summary>
        public float OffsetX { get; set; }

        /// <summary>Extra pixel offset Y on top of the auto-stacked slot.</summary>
        public float OffsetY { get; set; }
    }
}
