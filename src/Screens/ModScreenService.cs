using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;

namespace STS2RitsuLib.Screens
{
    /// <summary>
    ///     Lightweight mod-facing façade around <see cref="NCapstoneContainer" /> for opening and closing
    ///     custom <see cref="ICapstoneScreen" />s without depending on any specific ritsulib subsystem
    ///     (card piles, top-bar buttons, combat commands, …). This is the single public API any mod code
    ///     should use when it wants to mount a screen as a full-page overlay.
    ///     围绕 <see cref="NCapstoneContainer" /> 的轻量 mod 侧外观，用于打开和关闭
    ///     自定义 <see cref="ICapstoneScreen" />，无需依赖任何特定 ritsulib 子系统
    ///     （卡牌牌堆、顶部栏按钮、战斗命令等）。mod 代码想将屏幕挂载为整页覆盖层时，
    ///     应使用这个唯一的公共 API。
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
    ///     <para>
    ///         capstone 容器是由场景拥有的单例；在菜单中或两次跑局之间，它可能
    ///         尚未存在。因此这里的 helper 会在 <see cref="NCapstoneContainer.Instance" />
    ///         为 null 时静默无操作，调用方无需为每次调用都加保护。
    ///     </para>
    ///     <para>
    ///         当已有 capstone 屏幕正在显示时，<see cref="Open" /> 会先关闭它，让新的
    ///         屏幕接管舞台；这符合用户点击用于切换或替换屏幕的“view”式
    ///         顶部栏按钮时的预期行为。
    ///     </para>
    /// </remarks>
    public static class ModScreenService
    {
        /// <summary>
        ///     The screen currently owning the capstone container, or null when the container is idle / not
        ///     yet instantiated.
        ///     当前拥有 capstone 容器的屏幕；当容器空闲或尚未实例化时为 null。
        /// </summary>
        public static ICapstoneScreen? CurrentCapstoneScreen => NCapstoneContainer.Instance?.CurrentCapstoneScreen;

        /// <summary>
        ///     True when a capstone is visible right now.
        ///     当前有 capstone 可见时为 true。
        /// </summary>
        public static bool IsCapstoneOpen => NCapstoneContainer.Instance is { InUse: true };

        /// <summary>
        ///     Mounts <paramref name="screen" /> inside <see cref="NCapstoneContainer" />. If a different
        ///     capstone is already open, it is closed first; if the same instance is already mounted this
        ///     is a no-op.
        ///     将 <paramref name="screen" /> 挂载到 <see cref="NCapstoneContainer" /> 内。如果已有另一个
        ///     capstone 打开，会先将其关闭；如果同一实例已经挂载，则
        ///     不执行任何操作。
        /// </summary>
        /// <param name="screen">
        ///     Screen to mount (must also be a Godot <see cref="Node" />).
        ///     要挂载的屏幕（也必须是 Godot <see cref="Node" />）。
        /// </param>
        /// <returns>
        ///     True when the screen was mounted; false when no container is available.
        ///     屏幕已挂载时为 true；没有可用容器时为 false。
        /// </returns>
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
        ///     关闭当前 capstone（如果有）。实际发生关闭时返回 true。
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
        ///     便捷切换：如果 <paramref name="screen" /> 已是当前 capstone，则关闭它；
        ///     否则打开它。
        /// </summary>
        public static bool Toggle(ICapstoneScreen screen)
        {
            ArgumentNullException.ThrowIfNull(screen);

            return ReferenceEquals(CurrentCapstoneScreen, screen) ? Close() : Open(screen);
        }
    }
}
