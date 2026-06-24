namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Registers a codec and processor on <see cref="RitsuLibSidecarBus" /> in one call.
    ///     一次调用即可在 <see cref="RitsuLibSidecarBus" /> 上注册 codec 和 processor。
    /// </summary>
    public static class RitsuLibSidecarMessageBinding
    {
        /// <summary>
        ///     Subscribes <paramref name="processor" /> for <see cref="IRitsuLibSidecarMessageCodec{T}.Opcode" />. The
        ///     handler runs on the same thread as <see cref="RitsuLibSidecarReceivePipeline.ShouldSuppressVanillaDeserialize" />
        ///     (vanilla multiplayer receive path), which is not guaranteed to be the Godot main thread. Send with
        ///     <see cref="RitsuLibSidecar.CreateEnvelopeWithDelivery" /> or <see cref="RitsuLibSidecarHighLevelSend" /> to
        ///     record delivery semantics in the header extension.
        ///     为 <paramref name="processor" /> 订阅 <see cref="IRitsuLibSidecarMessageCodec{T}.Opcode" />。该
        ///     处理器运行在线程与 <see cref="RitsuLibSidecarReceivePipeline.ShouldSuppressVanillaDeserialize" /> 相同的线程上
        ///     （原版多人接收路径），不保证是 Godot 主线程。发送时使用
        ///     <see cref="RitsuLibSidecar.CreateEnvelopeWithDelivery" /> 或 <see cref="RitsuLibSidecarHighLevelSend" /> 来
        ///     在 header 扩展中记录投递语义。
        /// </summary>
        /// <param name="codec">
        ///     Encodes and decodes the payload for this opcode.
        ///     为此 opcode 编码和解码载荷。
        /// </param>
        /// <param name="processor">
        ///     Applies decoded messages.
        ///     应用已解码消息。
        /// </param>
        public static void Register<T>(
            IRitsuLibSidecarMessageCodec<T> codec,
            IRitsuLibSidecarSyncProcessor<T> processor)
            where T : notnull
        {
            ArgumentNullException.ThrowIfNull(codec);
            ArgumentNullException.ThrowIfNull(processor);
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            RitsuLibSidecarBus.RegisterHandler(
                codec.Opcode,
                ctx =>
                {
                    if (!codec.TryDecode(ctx.Payload.Span, out var m) || m is null)
                        return;

                    processor.Apply(m, in ctx);
                });
        }

        /// <summary>
        ///     Like <see cref="Register{T}" />, but copies envelope bytes then decodes and calls
        ///     <paramref name="processor" /> on the Godot main loop when
        ///     <see cref="RitsuLibSidecarGodotMainLoopScheduling.TryPostToMainLoop" />
        ///     succeeds; otherwise falls back to the receive thread (same as <see cref="Register{T}" />).
        ///     类似 <see cref="Register{T}" />，但会复制 envelope 字节，然后解码并在
        ///     <paramref name="processor" /> 于 Godot 主循环上调用，当
        ///     <see cref="RitsuLibSidecarGodotMainLoopScheduling.TryPostToMainLoop" />
        ///     成功时；否则回退到接收线程（与 <see cref="Register{T}" /> 相同）。
        /// </summary>
        public static void RegisterForGodotMainLoop<T>(
            IRitsuLibSidecarMessageCodec<T> codec,
            IRitsuLibSidecarSyncProcessor<T> processor)
            where T : notnull
        {
            ArgumentNullException.ThrowIfNull(codec);
            ArgumentNullException.ThrowIfNull(processor);
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            RitsuLibSidecarBus.RegisterHandler(
                codec.Opcode,
                ctx =>
                {
                    var owned = ctx.WithOwnedEnvelopeMemory();

                    if (!RitsuLibSidecarGodotMainLoopScheduling.TryPostToMainLoop(ApplyOnLoop))
                        ApplyOnLoop();
                    return;

                    void ApplyOnLoop()
                    {
                        if (!codec.TryDecode(owned.Payload.Span, out var m) || m is null)
                            return;

                        processor.Apply(m, in owned);
                    }
                });
        }
    }
}
