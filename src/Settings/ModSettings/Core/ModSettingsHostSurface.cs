using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Where the RitsuLib mod settings UI is currently hosted. Used to gate visibility and editability of pages and
    ///     sections.
    ///     RitsuLib Mod 设置 UI 当前所在的宿主位置。用于控制页面和 section 的可见性与可编辑性。
    /// </summary>
    [Flags]
    public enum ModSettingsHostSurface
    {
        /// <summary>
        ///     No surface (never use alone for defaults).
        ///     无 surface（不要单独作为默认值使用）。
        /// </summary>
        None = 0,

        /// <summary>
        ///     Settings opened from the main menu (no run in progress).
        ///     从主菜单打开的设置（没有进行中的 run）。
        /// </summary>
        MainMenu = 1 << 0,

        /// <summary>
        ///     Pause/settings while a run exists but combat is not actively in progress.
        ///     存在 run 但当前没有进行中战斗时的暂停/设置。
        /// </summary>
        RunPause = 1 << 1,

        /// <summary>
        ///     Pause/settings opened while a combat encounter is in progress (paused mid-fight).
        ///     战斗遭遇进行中打开的暂停/设置（战斗中暂停）。
        /// </summary>
        CombatPause = 1 << 2,

        /// <summary>
        ///     Convenience mask matching all built-in surfaces.
        ///     匹配所有内置 surface 的便捷掩码。
        /// </summary>
        All = MainMenu | RunPause | CombatPause,
    }

    /// <summary>
    ///     Resolves the active <see cref="ModSettingsHostSurface" /> from run/combat managers.
    ///     从跑局 / 战斗 manager 解析活动的 <see cref="ModSettingsHostSurface" />。
    /// </summary>
    public static class ModSettingsHostSurfaceResolver
    {
        /// <summary>
        ///     Returns exactly one surface bit describing where the player opened settings from.
        ///     返回恰好一个 surface 位，用于描述玩家从哪里打开设置。
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
        ///     当 <paramref name="mask" /> 包含 <see cref="ResolveCurrent" /> 返回的界面时为 true。
        /// </summary>
        public static bool IsVisibleOnCurrentHost(ModSettingsHostSurface mask)
        {
            var current = ResolveCurrent();
            return (mask & current) != 0;
        }

        /// <summary>
        ///     True when the current host is listed in <paramref name="readOnlyMask" /> (inputs should be read-only).
        ///     当当前 host 列在 <paramref name="readOnlyMask" /> 中时为 true（输入应为只读）。
        /// </summary>
        public static bool IsReadOnlyOnCurrentHost(ModSettingsHostSurface readOnlyMask)
        {
            var current = ResolveCurrent();
            return (readOnlyMask & current) != 0;
        }

        /// <summary>
        ///     AND-combines an optional predicate with a host-surface rule (either side may be absent).
        ///     将可选谓词与宿主表面规则做逻辑与组合（任一侧都可以不存在）。
        /// </summary>
        public static Func<bool> CombineVisibility(Func<bool>? existing, Func<bool> hostPredicate)
        {
            ArgumentNullException.ThrowIfNull(hostPredicate);
            return () => (existing?.Invoke() ?? true) && hostPredicate();
        }
    }
}
