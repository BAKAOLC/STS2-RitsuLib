namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">Registers a codec and processor with <see cref="RitsuLibSidecarBus" /> in one call.</para>
    ///     <para xml:lang="zh-CN">一次调用即可向 <see cref="RitsuLibSidecarBus" /> 注册编解码器和处理器。</para>
    /// </summary>
    public static class RitsuLibSidecarMessageBinding
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Subscribes <paramref name="processor" /> for <see cref="IRitsuLibSidecarMessageCodec{T}.Opcode" />. The
        ///         processor runs on the same thread as
        ///         <see cref="RitsuLibSidecarReceivePipeline.ShouldSuppressVanillaDeserialize" />
        ///         in the vanilla multiplayer receive path, which is not guaranteed to be the Godot main thread. Send with
        ///         <see cref="RitsuLibSidecar.CreateEnvelopeWithDelivery" /> or <see cref="RitsuLibSidecarHighLevelSend" /> to
        ///         record delivery semantics in the header extension.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <see cref="IRitsuLibSidecarMessageCodec{T}.Opcode" /> 注册 <paramref name="processor" />。
        ///         处理器与游戏原版多人接收路径中的
        ///         <see cref="RitsuLibSidecarReceivePipeline.ShouldSuppressVanillaDeserialize" /> 在同一线程运行，
        ///         该线程不保证是 Godot 主线程。发送时可使用 <see cref="RitsuLibSidecar.CreateEnvelopeWithDelivery" />
        ///         或 <see cref="RitsuLibSidecarHighLevelSend" /> 在标头扩展中记录投递语义。
        ///     </para>
        /// </summary>
        /// <param name="codec">
        ///     <para xml:lang="en">Encodes and decodes the payload for this opcode.</para>
        ///     <para xml:lang="zh-CN">为此操作码编码和解码载荷。</para>
        /// </param>
        /// <param name="processor">
        ///     <para xml:lang="en">Processes decoded messages.</para>
        ///     <para xml:lang="zh-CN">处理已解码消息。</para>
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
        ///     <para xml:lang="en">
        ///         Like <see cref="Register{T}" />, but copies envelope bytes then decodes and calls
        ///         <paramref name="processor" /> on the Godot main loop when
        ///         <see cref="RitsuLibSidecarGodotMainLoopScheduling.TryPostToMainLoop" />
        ///         succeeds; otherwise falls back to the receive thread (same as <see cref="Register{T}" />).
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         与 <see cref="Register{T}" /> 类似，但会先复制信封字节。当
        ///         <see cref="RitsuLibSidecarGodotMainLoopScheduling.TryPostToMainLoop" /> 成功时，
        ///         在 Godot 主循环解码并调用 <paramref name="processor" />；否则回退到接收线程，
        ///         与 <see cref="Register{T}" /> 相同。
        ///     </para>
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
