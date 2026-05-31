namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     Implement on a model to receive synced right-click actions through RitsuLib.
    ///     在模型上实现此接口，即可通过 RitsuLib 接收同步右键动作。
    /// </summary>
    public interface IModRightClickableModel
    {
        /// <summary>
        ///     Local preflight check. Returning false leaves the input unhandled.
        ///     本地预检；返回 false 时不会消耗输入。
        /// </summary>
        bool CanHandleRightClickLocal(ModRightClickContext context)
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
