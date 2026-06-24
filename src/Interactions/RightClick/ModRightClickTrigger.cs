namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     Input metadata carried by a model right-click request.
    ///     模型右键请求携带的输入元数据。
    /// </summary>
    /// <param name="IsController">
    ///     True when the request came from controller cancel on a focused control.
    ///     当请求来自聚焦控件上的手柄 cancel 输入时为 true。
    /// </param>
    /// <param name="Metadata">
    ///     Optional mod-defined metadata for custom handlers.
    ///     可选的 mod 自定义元数据，供自定义 handler 使用。
    /// </param>
    public readonly record struct ModRightClickTrigger(bool IsController = false, string? Metadata = null);
}
