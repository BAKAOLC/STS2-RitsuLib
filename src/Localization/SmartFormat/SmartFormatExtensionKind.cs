namespace STS2RitsuLib.Localization.SmartFormat
{
    /// <summary>
    ///     <para xml:lang="en">Identifies the category of a SmartFormat extension registered through RitsuLib.</para>
    ///     <para xml:lang="zh-CN">标识通过 RitsuLib 注册的 SmartFormat 扩展类别。</para>
    /// </summary>
    public enum SmartFormatExtensionKind
    {
        /// <summary>
        ///     <para xml:lang="en">A SmartFormat selector source.</para>
        ///     <para xml:lang="zh-CN">SmartFormat 选择器数据源。</para>
        /// </summary>
        Source,

        /// <summary>
        ///     <para xml:lang="en">A SmartFormat formatter.</para>
        ///     <para xml:lang="zh-CN">SmartFormat 格式化器。</para>
        /// </summary>
        Formatter,
    }
}
