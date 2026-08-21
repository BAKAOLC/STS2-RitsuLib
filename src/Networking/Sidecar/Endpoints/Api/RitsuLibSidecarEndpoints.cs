namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">Registers backend-independent, host-routed Sidecar data endpoints.</para>
    ///     <para xml:lang="zh-CN">注册后端无关、由主机路由的 Sidecar 数据端点。</para>
    /// </summary>
    public static class RitsuLibSidecarEndpoints
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers one endpoint for the process lifetime or until the returned handle is disposed. Duplicate
        ///         owner/name pairs are rejected and never replace an existing registration.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个持续到进程结束或返回句柄被释放的端点。重复的所有者/名称组合会被拒绝，绝不会替换已有注册。
        ///     </para>
        /// </summary>
        /// <param name="descriptor">
        ///     <para xml:lang="en">Validated immutable endpoint contract.</para>
        ///     <para xml:lang="zh-CN">经过验证的不可变端点契约。</para>
        /// </param>
        /// <param name="handler">
        ///     <para xml:lang="en">
        ///         Receive callback. Exceptions are logged and isolated from transport dispatch.
        ///     </para>
        ///     <para xml:lang="zh-CN">接收回调。异常会被记录，并与传输分发隔离。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">An owning registration handle.</para>
        ///     <para xml:lang="zh-CN">拥有该注册的句柄。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">The descriptor or handler is null.</para>
        ///     <para xml:lang="zh-CN">描述符或处理器为空。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">The endpoint is already registered or the local registration limit was reached.</para>
        ///     <para xml:lang="zh-CN">端点已经注册，或已达到本地注册数量上限。</para>
        /// </exception>
        public static RitsuLibSidecarEndpointHandle Register(
            RitsuLibSidecarEndpointDescriptor descriptor,
            Action<RitsuLibSidecarEndpointMessage> handler)
        {
            return RitsuLibSidecarEndpointRegistry.Register(descriptor, handler);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers one bounded bulk-stream endpoint. The receive handler is invoked once per validated offer and
        ///         returns a fresh writable target to accept it, or null to reject it. Duplicate owner/name pairs are
        ///         rejected and never replace an existing registration.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个有界批量流端点。每个经过验证的提议都会调用一次接收处理器；处理器返回新的可写目标以
        ///         接受，或返回空值以拒绝。重复的所有者/名称组合会被拒绝，绝不会替换已有注册。
        ///     </para>
        /// </summary>
        /// <param name="descriptor">
        ///     <para xml:lang="en">
        ///         Validated endpoint descriptor whose delivery profile is
        ///         <see cref="RitsuLibSidecarDeliveryProfile.BulkStream" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         投递档位为 <see cref="RitsuLibSidecarDeliveryProfile.BulkStream" /> 的已验证端点描述符。
        ///     </para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">Bounded stream, window, concurrency, retry, and timeout policy.</para>
        ///     <para xml:lang="zh-CN">有界的流、窗口、并发、重试与超时策略。</para>
        /// </param>
        /// <param name="handler">
        ///     <para xml:lang="en">
        ///         Offer handler scheduled according to the descriptor dispatch mode. It must return a target created only
        ///         for this offer, or null. Exceptions reject the offer and are isolated.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按描述符分发模式调度的提议处理器。它必须返回仅为此次提议创建的目标，或返回空值。异常会拒绝
        ///         提议并被隔离。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">An owning bulk endpoint handle.</para>
        ///     <para xml:lang="zh-CN">拥有该批量端点的句柄。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">A required argument is null.</para>
        ///     <para xml:lang="zh-CN">必需参数为空。</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">The descriptor does not use the bulk-stream delivery profile.</para>
        ///     <para xml:lang="zh-CN">描述符没有使用批量流投递档位。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">The endpoint is already registered or the local registration limit was reached.</para>
        ///     <para xml:lang="zh-CN">端点已经注册，或已达到本地注册数量上限。</para>
        /// </exception>
        public static RitsuLibSidecarBulkEndpointHandle RegisterBulk(
            RitsuLibSidecarEndpointDescriptor descriptor,
            RitsuLibSidecarBulkStreamOptions options,
            Func<RitsuLibSidecarBulkStreamOffer, RitsuLibSidecarBulkReceiveTarget?> handler)
        {
            return RitsuLibSidecarEndpointRegistry.RegisterBulk(descriptor, options, handler);
        }
    }
}
