namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     Implement on a model to receive synced right-click actions through RitsuLib.
    ///     在模型上实现此接口，即可通过 RitsuLib 接收同步右键动作。
    /// </summary>
    public interface IModRightClickableModel
    {
        /// <summary>
        ///     Optional local-only fast filter. Use only stable, local UI facts here; mutable gameplay state should be
        ///     checked in <see cref="CanExecuteRightClick" /> or <see cref="OnRightClick" />.
        ///     可选的仅本地快速过滤。这里只应使用稳定的本地 UI 信息；可变游戏状态应在
        ///     <see cref="CanExecuteRightClick" /> 或 <see cref="OnRightClick" /> 中检查。
        /// </summary>
        bool CanHandleRightClickLocal(ModRightClickContext context)
        {
            return true;
        }

        /// <summary>
        ///     Execution-time guard. It runs after the synced action resolves the model on each peer.
        ///     执行期判定：同步动作在各端解析模型后调用。
        /// </summary>
        bool CanExecuteRightClick(ModRightClickExecutionContext context)
        {
            return true;
        }

        /// <summary>
        ///     Runs when the synced right-click action reaches the queue.
        ///     当同步右键动作到达队列时运行。
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
}
