namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>Registers a codec and processor on <see cref="RitsuLibSidecarBus" /> in one call.</summary>
    public static class RitsuLibSidecarMessageBinding
    {
        /// <summary>
        ///     Subscribes <paramref name="processor" /> for <see cref="IRitsuLibSidecarMessageCodec{T}.Opcode" />. The
        ///     handler is invoked on the game thread. Send with <see cref="RitsuLibSidecar.CreateEnvelopeWithDelivery" />
        ///     or <see cref="RitsuLibSidecarHighLevelSend" /> to record delivery semantics in the header extension.
        /// </summary>
        /// <param name="codec">Encodes and decodes the payload for this opcode.</param>
        /// <param name="processor">Applies decoded messages on the game thread.</param>
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
    }
}
