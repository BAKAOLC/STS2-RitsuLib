using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;

namespace STS2RitsuLib.Screens
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Opens, closes, and queries custom <see cref="ICapstoneScreen" /> instances through
    ///         <see cref="NCapstoneContainer" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过 <see cref="NCapstoneContainer" /> 打开、关闭和查询自定义
    ///         <see cref="ICapstoneScreen" /> 实例。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Operations return <see langword="false" /> when the scene-owned container is unavailable. Opening a
    ///         screen replaces a different current screen and leaves an already-current instance unchanged.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         场景持有的容器不可用时，操作返回 <see langword="false" />。打开屏幕会替换其他当前屏幕；
    ///         已是当前屏幕的实例保持不变。
    ///     </para>
    /// </remarks>
    public static class ModScreenService
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the current Capstone screen, or <see langword="null" /> when the container is idle or
        ///         unavailable.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取当前 Capstone 屏幕；容器空闲或不可用时为 <see langword="null" />。</para>
        /// </summary>
        public static ICapstoneScreen? CurrentCapstoneScreen => NCapstoneContainer.Instance?.CurrentCapstoneScreen;

        /// <summary>
        ///     <para xml:lang="en">Gets whether a Capstone screen is currently open.</para>
        ///     <para xml:lang="zh-CN">获取当前是否打开了 Capstone 屏幕。</para>
        /// </summary>
        public static bool IsCapstoneOpen => NCapstoneContainer.Instance is { InUse: true };

        /// <summary>
        ///     <para xml:lang="en">
        ///         Mounts <paramref name="screen" /> in <see cref="NCapstoneContainer" />. Opening the screen
        ///         replaces a different current screen; opening the already current instance is a no-op.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <paramref name="screen" /> 挂载到 <see cref="NCapstoneContainer" /> 中。打开该屏幕会替换不同的
        ///         当前屏幕；打开已是当前实例的屏幕时不执行任何操作。
        ///     </para>
        /// </summary>
        /// <param name="screen">
        ///     <para xml:lang="en">The screen to mount, which must also be a Godot <see cref="Node" />.</para>
        ///     <para xml:lang="zh-CN">要挂载的屏幕，且其也必须是 Godot <see cref="Node" />。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if <paramref name="screen" /> is current after the call; otherwise,
        ///         <see langword="false" /> when the container is unavailable.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         调用后 <paramref name="screen" /> 为当前屏幕时返回 <see langword="true" />；容器不可用时返回
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool Open(ICapstoneScreen screen)
        {
            ArgumentNullException.ThrowIfNull(screen);

            var container = NCapstoneContainer.Instance;
            if (container == null)
                return false;

            if (ReferenceEquals(container.CurrentCapstoneScreen, screen))
                return true;

            container.Open(screen);
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Closes the current Capstone screen, if any.</para>
        ///     <para xml:lang="zh-CN">关闭当前 Capstone 屏幕（如果存在）。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if an open screen was closed; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">关闭了已打开的屏幕时返回 <see langword="true" />；否则返回 <see langword="false" />。</para>
        /// </returns>
        public static bool Close()
        {
            var container = NCapstoneContainer.Instance;
            if (container is not { InUse: true })
                return false;

            container.Close();
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Closes <paramref name="screen" /> when it is current; otherwise opens it.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="screen" /> 为当前屏幕时将其关闭；否则打开它。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if the requested open or close operation succeeded; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         请求的打开或关闭操作成功时返回 <see langword="true" />；否则返回 <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool Toggle(ICapstoneScreen screen)
        {
            ArgumentNullException.ThrowIfNull(screen);

            return ReferenceEquals(CurrentCapstoneScreen, screen) ? Close() : Open(screen);
        }
    }
}
