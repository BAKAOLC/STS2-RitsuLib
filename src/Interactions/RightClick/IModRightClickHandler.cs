namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Handles a local right-click request before the built-in model-interface handler.
    ///     </para>
    ///     <para xml:lang="zh-CN">在内置的模型接口处理器之前处理本地右键请求。</para>
    /// </summary>
    public interface IModRightClickHandler
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the handler priority; higher-priority handlers run first.</para>
        ///     <para xml:lang="zh-CN">获取处理器优先级；优先级越高，运行越早。</para>
        /// </summary>
        int Priority => 0;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns <see langword="true" /> when the request was accepted and its input should be consumed.
        ///     </para>
        ///     <para xml:lang="zh-CN">请求已被接受且应消耗此次输入时返回 <see langword="true" />。</para>
        /// </summary>
        bool TryHandle(ModRightClickContext context);
    }
}
