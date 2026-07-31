using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Identifies the game context from which the RitsuLib mod settings UI is opened. Pages and sections can use
    ///         it to control visibility and editability.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         标识打开 RitsuLib 模组设置界面时所在的游戏上下文。页面与节可据此控制可见性和可编辑性。
    ///     </para>
    /// </summary>
    [Flags]
    public enum ModSettingsHostSurface
    {
        /// <summary>
        ///     <para xml:lang="en">No host context. A mask containing only this value never matches a resolved host.</para>
        ///     <para xml:lang="zh-CN">无宿主上下文。仅包含此值的掩码不会匹配任何已解析的宿主。</para>
        /// </summary>
        None = 0,

        /// <summary>
        ///     <para xml:lang="en">Settings opened from the main menu while no run is in progress.</para>
        ///     <para xml:lang="zh-CN">未进行对局时从主菜单打开的设置。</para>
        /// </summary>
        MainMenu = 1 << 0,

        /// <summary>
        ///     <para xml:lang="en">Settings opened during a run while no combat is in progress.</para>
        ///     <para xml:lang="zh-CN">对局进行中但当前不在战斗内时打开的设置。</para>
        /// </summary>
        RunPause = 1 << 1,

        /// <summary>
        ///     <para xml:lang="en">Settings opened while combat is in progress.</para>
        ///     <para xml:lang="zh-CN">战斗进行中打开的设置。</para>
        /// </summary>
        CombatPause = 1 << 2,

        /// <summary>
        ///     <para xml:lang="en">A mask containing every built-in host context.</para>
        ///     <para xml:lang="zh-CN">包含所有内置宿主上下文的掩码。</para>
        /// </summary>
        All = MainMenu | RunPause | CombatPause,
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Resolves the active <see cref="ModSettingsHostSurface" /> from the run and combat managers.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         根据对局与战斗管理器解析当前的 <see cref="ModSettingsHostSurface" />。
    ///     </para>
    /// </summary>
    public static class ModSettingsHostSurfaceResolver
    {
        /// <summary>
        ///     <para xml:lang="en">Returns exactly one flag describing the current settings host context.</para>
        ///     <para xml:lang="zh-CN">返回且仅返回一个用于描述当前设置宿主上下文的标志。</para>
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
        ///     <para xml:lang="en">
        ///         Determines whether a host mask includes the context returned by <see cref="ResolveCurrent" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         确定宿主掩码是否包含 <see cref="ResolveCurrent" /> 返回的上下文。
        ///     </para>
        /// </summary>
        /// <param name="mask">
        ///     <para xml:lang="en">The allowed host contexts.</para>
        ///     <para xml:lang="zh-CN">允许的宿主上下文。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Whether the current host is included.</para>
        ///     <para xml:lang="zh-CN">当前宿主是否包含在内。</para>
        /// </returns>
        public static bool IsVisibleOnCurrentHost(ModSettingsHostSurface mask)
        {
            var current = ResolveCurrent();
            return (mask & current) != 0;
        }

        /// <summary>
        ///     <para xml:lang="en">Determines whether a read-only mask includes the current host context.</para>
        ///     <para xml:lang="zh-CN">确定只读掩码是否包含当前宿主上下文。</para>
        /// </summary>
        /// <param name="readOnlyMask">
        ///     <para xml:lang="en">The host contexts in which inputs should be read-only.</para>
        ///     <para xml:lang="zh-CN">输入应为只读的宿主上下文。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Whether the current host is read-only.</para>
        ///     <para xml:lang="zh-CN">当前宿主是否为只读。</para>
        /// </returns>
        public static bool IsReadOnlyOnCurrentHost(ModSettingsHostSurface readOnlyMask)
        {
            var current = ResolveCurrent();
            return (readOnlyMask & current) != 0;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Combines an optional existing predicate and a host-context predicate with logical AND. A missing
        ///         existing predicate is treated as <see langword="true" />; any evaluated predicate that throws is
        ///         logged and treated as <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         以逻辑与组合可选的现有谓词与宿主上下文谓词。缺少现有谓词时按
        ///         <see langword="true" /> 处理；任何已求值谓词抛出的异常都会被记录并按
        ///         <see langword="false" /> 处理。
        ///     </para>
        /// </summary>
        /// <param name="existing">
        ///     <para xml:lang="en">The optional existing visibility predicate.</para>
        ///     <para xml:lang="zh-CN">可选的现有可见性谓词。</para>
        /// </param>
        /// <param name="hostPredicate">
        ///     <para xml:lang="en">The required host-context predicate.</para>
        ///     <para xml:lang="zh-CN">必需的宿主上下文谓词。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A predicate that requires both conditions to pass.</para>
        ///     <para xml:lang="zh-CN">要求两个条件均通过的谓词。</para>
        /// </returns>
        public static Func<bool> CombineVisibility(Func<bool>? existing, Func<bool> hostPredicate)
        {
            ArgumentNullException.ThrowIfNull(hostPredicate);
            return () => ModSettingsPredicate.Evaluate(existing) && ModSettingsPredicate.Evaluate(hostPredicate);
        }
    }
}
