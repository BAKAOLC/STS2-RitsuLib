namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Specifies how a named audio channel handles a claim while another handle owns it.</para>
    ///     <para xml:lang="zh-CN">指定已有句柄占用命名音频通道时如何处理新的占用请求。</para>
    /// </summary>
    public enum AudioChannelMode
    {
        /// <summary>
        ///     <para xml:lang="en">Keeps the current owner and rejects the new claim.</para>
        ///     <para xml:lang="zh-CN">保留当前占用者并拒绝新的占用请求。</para>
        /// </summary>
        KeepExisting = 0,

        /// <summary>
        ///     <para xml:lang="en">Stops and releases the current owner, replacing it only when release succeeds.</para>
        ///     <para xml:lang="zh-CN">停止并释放当前占用者，并且仅在释放成功时用新句柄替换。</para>
        /// </summary>
        ReplaceExisting = 1,
    }
}
