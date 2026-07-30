namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes the status, optional controllable handle, and diagnostic message produced by a
    ///         playback request.
    ///     </para>
    ///     <para xml:lang="zh-CN">描述播放请求产生的状态、可选可控制句柄和诊断消息。</para>
    /// </summary>
    public sealed class AudioPlayResult
    {
        private AudioPlayResult(AudioPlayStatus status, IAudioHandle? handle, string? message)
        {
            Status = status;
            Handle = handle;
            Message = message;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the playback outcome category.</para>
        ///     <para xml:lang="zh-CN">获取播放结果类别。</para>
        /// </summary>
        public AudioPlayStatus Status { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the controllable handle created for the request, or null for handleless playback and
        ///         failures.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取为请求创建的可控制句柄；无句柄播放和失败结果中为 <see langword="null" />。</para>
        /// </summary>
        public IAudioHandle? Handle { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional diagnostic message.</para>
        ///     <para xml:lang="zh-CN">获取可选诊断消息。</para>
        /// </summary>
        public string? Message { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether <see cref="Status" /> is <see cref="AudioPlayStatus.Started" />, independently of
        ///         whether a handle exists.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取 <see cref="Status" /> 是否为 <see cref="AudioPlayStatus.Started" />，与是否存在句柄无关。</para>
        /// </summary>
        public bool Succeeded => Status == AudioPlayStatus.Started;

        /// <summary>
        ///     <para xml:lang="en">Creates a successful result for controllable or handleless playback.</para>
        ///     <para xml:lang="zh-CN">为可控制或无句柄播放创建成功结果。</para>
        /// </summary>
        /// <param name="handle">
        ///     <para xml:lang="en">The optional controllable handle.</para>
        ///     <para xml:lang="zh-CN">可选的可控制句柄。</para>
        /// </param>
        /// <param name="message">
        ///     <para xml:lang="en">The optional diagnostic message.</para>
        ///     <para xml:lang="zh-CN">可选诊断消息。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A result whose status is <see cref="AudioPlayStatus.Started" />.</para>
        ///     <para xml:lang="zh-CN">状态为 <see cref="AudioPlayStatus.Started" /> 的结果。</para>
        /// </returns>
        public static AudioPlayResult Started(IAudioHandle? handle = null, string? message = null)
        {
            return new(AudioPlayStatus.Started, handle, message);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a handleless failed result.</para>
        ///     <para xml:lang="zh-CN">创建不带句柄的失败结果。</para>
        /// </summary>
        /// <param name="status">
        ///     <para xml:lang="en">The non-success outcome category.</para>
        ///     <para xml:lang="zh-CN">非成功结果类别。</para>
        /// </param>
        /// <param name="message">
        ///     <para xml:lang="en">The optional diagnostic message.</para>
        ///     <para xml:lang="zh-CN">可选诊断消息。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A failed result with no handle.</para>
        ///     <para xml:lang="zh-CN">不带句柄的失败结果。</para>
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">Thrown when <paramref name="status" /> is <see cref="AudioPlayStatus.Started" />.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="status" /> 为 <see cref="AudioPlayStatus.Started" /> 时抛出。</para>
        /// </exception>
        public static AudioPlayResult Fail(AudioPlayStatus status, string? message = null)
        {
            // Keep the exceptional guard distinct from result construction.
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (status == AudioPlayStatus.Started)
                throw new ArgumentOutOfRangeException(nameof(status), status, "A failed result cannot use Started.");

            return new(status, null, message);
        }
    }
}
