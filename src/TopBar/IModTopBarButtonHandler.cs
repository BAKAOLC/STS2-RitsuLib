namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines the behavior of a type registered with
    ///         <see cref="Interop.AutoRegistration.RegisterOwnedTopBarButtonAttribute" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义带有 <see cref="Interop.AutoRegistration.RegisterOwnedTopBarButtonAttribute" />
    ///         特性的类型在自动注册顶部栏按钮时使用的行为。
    ///     </para>
    /// </summary>
    public interface IModTopBarButtonHandler
    {
        /// <summary>
        ///     <para xml:lang="en">Invoked when the button is released, after its click animation starts.</para>
        ///     <para xml:lang="zh-CN">按钮释放且点击动画开始后调用。</para>
        /// </summary>
        void OnClick(ModTopBarButtonContext context);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether the button should be visible. This method runs during
        ///         <c>_Process</c> and should remain inexpensive. The default is <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回按钮是否应当可见。此方法会在 <c>_Process</c> 中调用，应避免耗时操作。
        ///         默认返回 <see langword="true" />。
        ///     </para>
        /// </summary>
        bool IsVisible(ModTopBarButtonContext context)
        {
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether the associated screen or mode is open. An open button uses the selected-state
        ///         tilt of vanilla top-bar buttons. This method runs during <c>_Process</c>; the default is
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回关联界面或模式是否打开。打开的按钮会使用原版顶部栏按钮的选中状态倾斜效果。
        ///         此方法会在 <c>_Process</c> 中调用；默认返回 <see langword="false" />。
        ///     </para>
        /// </summary>
        bool IsOpen(ModTopBarButtonContext context)
        {
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the count displayed below the icon. Negative values hide the label. This method runs
        ///         during <c>_Process</c>; the default is <c>-1</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回显示在图标下方的数量。负数会隐藏标签。此方法会在 <c>_Process</c> 中调用；
        ///         默认值为 <c>-1</c>。
        ///     </para>
        /// </summary>
        int GetCount(ModTopBarButtonContext context)
        {
            return -1;
        }
    }
}
