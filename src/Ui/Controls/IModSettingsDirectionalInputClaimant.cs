namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Identifies controls that temporarily claim directional input, such as open dropdowns or active
    ///         key-capture editors.
    ///     </para>
    ///     <para xml:lang="zh-CN">标识会临时占用方向输入的控件，例如已展开的下拉列表或正在捕获按键的编辑器。</para>
    /// </summary>
    internal interface IModSettingsDirectionalInputClaimant
    {
        bool ClaimsDirectionalInput { get; }
    }
}
