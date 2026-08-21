namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarBulkTransferCoordinator
    {
        private static int _inboundStreams;
        private static int _outboundStreams;

        internal static int ActiveInboundStreams => Volatile.Read(ref _inboundStreams);
        internal static int ActiveOutboundStreams => Volatile.Read(ref _outboundStreams);

        internal static Lease? TryAcquireInbound()
        {
            return TryAcquire(
                ref _inboundStreams,
                RitsuLibSidecarEndpointPolicy.MaxBulkConcurrentInboundStreamsGlobal,
                static () => Interlocked.Decrement(ref _inboundStreams));
        }

        internal static Lease? TryAcquireOutbound()
        {
            return TryAcquire(
                ref _outboundStreams,
                RitsuLibSidecarEndpointPolicy.MaxBulkConcurrentOutboundStreamsGlobal,
                static () => Interlocked.Decrement(ref _outboundStreams));
        }

        private static Lease? TryAcquire(ref int counter, int maximum, Action release)
        {
            while (true)
            {
                var current = Volatile.Read(ref counter);
                if (current >= maximum)
                    return null;
                if (Interlocked.CompareExchange(ref counter, current + 1, current) == current)
                    return new(release);
            }
        }

        internal sealed class Lease(Action release) : IDisposable
        {
            private Action? _release = release;

            public void Dispose()
            {
                Interlocked.Exchange(ref _release, null)?.Invoke();
            }
        }
    }
}
