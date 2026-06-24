namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     Handles a local right-click request before the built-in model-interface handler.
    ///     在内置模型接口 handler 之前处理本地右键请求。
    /// </summary>
    public interface IModRightClickHandler
    {
        /// <summary>
        ///     Higher priority handlers run first.
        ///     优先级越高越先运行。
        /// </summary>
        int Priority => 0;

        /// <summary>
        ///     Returns true when the request was accepted and should consume input.
        ///     返回 true 表示请求已被接受，应消耗输入。
        /// </summary>
        bool TryHandle(ModRightClickContext context);
    }
}
