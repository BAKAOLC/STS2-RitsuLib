using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Where the RitsuLib mod settings UI is currently hosted. Used to gate visibility and editability of pages and
    ///     sections.
    /// </summary>
    [Flags]
    public enum ModSettingsHostSurface
    {
        /// <summary>
        ///     No surface (never use alone for defaults).
        /// </summary>
        None = 0,

        /// <summary>
        ///     Settings opened from the main menu (no run in progress).
        /// </summary>
        MainMenu = 1 << 0,

        /// <summary>
        ///     Pause/settings while a run exists but combat is not actively in progress.
        /// </summary>
        RunPause = 1 << 1,

        /// <summary>
        ///     Pause/settings opened while a combat encounter is in progress (paused mid-fight).
        /// </summary>
        CombatPause = 1 << 2,

        /// <summary>
        ///     Convenience mask matching all built-in surfaces.
        /// </summary>
        All = MainMenu | RunPause | CombatPause,
    }

    /// <summary>
    ///     Resolves the active <see cref="ModSettingsHostSurface" /> from run/combat managers.
    /// </summary>
    public static class ModSettingsHostSurfaceResolver
    {
        /// <summary>
        ///     Returns exactly one surface bit describing where the player opened settings from.
        /// </summary>
        public static ModSettingsHostSurface ResolveCurrent()
        {
            if (RunManager.Instance?.IsInProgress != true)
                return ModSettingsHostSurface.MainMenu;

            return CombatManager.Instance?.IsInProgress == true
                ? ModSettingsHostSurface.CombatPause
                : ModSettingsHostSurface.RunPause;
        }

        /// <summary>
        ///     True when <paramref name="mask" /> includes the surface returned by <see cref="ResolveCurrent" />.
        /// </summary>
        public static bool IsVisibleOnCurrentHost(ModSettingsHostSurface mask)
        {
            var current = ResolveCurrent();
            return (mask & current) != 0;
        }

        /// <summary>
        ///     True when the current host is listed in <paramref name="readOnlyMask" /> (inputs should be read-only).
        /// </summary>
        public static bool IsReadOnlyOnCurrentHost(ModSettingsHostSurface readOnlyMask)
        {
            var current = ResolveCurrent();
            return (readOnlyMask & current) != 0;
        }

        /// <summary>
        ///     AND-combines an optional predicate with a host-surface rule (either side may be absent).
        /// </summary>
        public static Func<bool> CombineVisibility(Func<bool>? existing, Func<bool> hostPredicate)
        {
            ArgumentNullException.ThrowIfNull(hostPredicate);
            return () => (existing?.Invoke() ?? true) && hostPredicate();
        }
    }
}
