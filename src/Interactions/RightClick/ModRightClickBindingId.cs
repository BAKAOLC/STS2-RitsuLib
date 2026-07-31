namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     <para xml:lang="en">Identifies a registered right-click binding.</para>
    ///     <para xml:lang="zh-CN">标识一个已注册的右键绑定。</para>
    /// </summary>
    public readonly record struct ModRightClickBindingId(string Id)
    {
        /// <inheritdoc />
        public override string ToString()
        {
            return Id;
        }
    }
}
