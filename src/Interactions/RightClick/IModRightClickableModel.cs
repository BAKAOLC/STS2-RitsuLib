namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     <para xml:lang="en">Implement on a model to receive synchronized right-click actions through RitsuLib.</para>
    ///     <para xml:lang="zh-CN">在模型上实现此接口，以通过 RitsuLib 接收同步的右键操作。</para>
    /// </summary>
    public interface IModRightClickableModel
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Provides an optional local-only fast filter. Check only stable local UI facts here; mutable gameplay state
        ///         should be checked by <see cref="CanExecuteRightClick" /> or <see cref="OnRightClick" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         提供可选且仅在本地运行的快速筛选。此处只应检查稳定的本地界面信息；可变的游戏状态应由
        ///         <see cref="CanExecuteRightClick" /> 或 <see cref="OnRightClick" /> 检查。
        ///     </para>
        /// </summary>
        bool CanHandleRightClickLocal(ModRightClickContext context)
        {
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Determines whether the action may execute after each peer has resolved the synchronized model.
        ///     </para>
        ///     <para xml:lang="zh-CN">各端解析出同步模型后，判断该操作是否可以执行。</para>
        /// </summary>
        bool CanExecuteRightClick(ModRightClickExecutionContext context)
        {
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Runs when the synchronized right-click action reaches the action queue.</para>
        ///     <para xml:lang="zh-CN">同步的右键操作进入行动队列后运行。</para>
        /// </summary>
        Task OnRightClick(ModRightClickExecutionContext context);
    }

    /// <inheritdoc />
    public interface IModRightClickableCard : IModRightClickableModel;

    /// <inheritdoc />
    public interface IModRightClickableRelic : IModRightClickableModel;

    /// <inheritdoc />
    public interface IModRightClickablePower : IModRightClickableModel;

    /// <inheritdoc />
    public interface IModRightClickablePotion : IModRightClickableModel;

    /// <inheritdoc />
    public interface IModRightClickableOrb : IModRightClickableModel;
}
