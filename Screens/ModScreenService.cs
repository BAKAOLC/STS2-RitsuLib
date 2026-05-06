using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;

namespace STS2RitsuLib.Screens
{
    /// <summary>
    ///     Lightweight mod-facing façade around <see cref="NCapstoneContainer" /> for opening and closing
    ///     custom <see cref="ICapstoneScreen" />s without depending on any specific ritsulib subsystem
    ///     (card piles, top-bar buttons, combat commands, …). This is the single public API any mod code
    ///     should use when it wants to mount a screen as a full-page overlay.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The capstone container is a scene-owned singleton; during menus / between runs it may not
    ///         yet exist. The helpers here therefore silently no-op when <see cref="NCapstoneContainer.Instance" />
    ///         is null so callers do not need to guard every invocation.
    ///     </para>
    ///     <para>
    ///         When a capstone screen is already showing, <see cref="Open" /> closes it first so the new
    ///         screen can take the stage — matching the behaviour users expect when clicking "view"-style
    ///         top-bar buttons that toggle or swap screens.
    ///     </para>
    /// </remarks>
    public static class ModScreenService
    {
        /// <summary>
        ///     The screen currently owning the capstone container, or null when the container is idle / not
        ///     yet instantiated.
        /// </summary>
        public static ICapstoneScreen? CurrentCapstoneScreen => NCapstoneContainer.Instance?.CurrentCapstoneScreen;

        /// <summary>
        ///     True when a capstone is visible right now.
        /// </summary>
        public static bool IsCapstoneOpen => NCapstoneContainer.Instance is { InUse: true };

        /// <summary>
        ///     Mounts <paramref name="screen" /> inside <see cref="NCapstoneContainer" />. If a different
        ///     capstone is already open, it is closed first; if the same instance is already mounted this
        ///     is a no-op.
        /// </summary>
        /// <param name="screen">Screen to mount (must also be a Godot <see cref="Node" />).</param>
        /// <returns>True when the screen was mounted; false when no container is available.</returns>
        public static bool Open(ICapstoneScreen screen)
        {
            ArgumentNullException.ThrowIfNull(screen);

            var container = NCapstoneContainer.Instance;
            if (container == null)
                return false;

            if (ReferenceEquals(container.CurrentCapstoneScreen, screen))
                return true;

            if (container.InUse)
                container.Close();

            container.Open(screen);
            return true;
        }

        /// <summary>
        ///     Closes the current capstone, if any. Returns true when a close actually happened.
        /// </summary>
        public static bool Close()
        {
            var container = NCapstoneContainer.Instance;
            if (container is not { InUse: true })
                return false;

            container.Close();
            return true;
        }

        /// <summary>
        ///     Convenience toggle: if <paramref name="screen" /> is already the current capstone, close it;
        ///     otherwise open it.
        /// </summary>
        public static bool Toggle(ICapstoneScreen screen)
        {
            ArgumentNullException.ThrowIfNull(screen);

            return ReferenceEquals(CurrentCapstoneScreen, screen) ? Close() : Open(screen);
        }
    }
}
