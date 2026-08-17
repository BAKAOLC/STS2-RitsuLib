namespace STS2RitsuLib.Networking.Sidecar
{
    internal sealed class RitsuLibSidecarTokenBucket
    {
        private readonly Lock _gate = new();
        private readonly double _packetCapacity;
        private readonly double _byteCapacity;
        private readonly double _packetsPerMillisecond;
        private readonly double _bytesPerMillisecond;
        private readonly Func<long> _tickCount64;

        private double _packetTokens;
        private double _byteTokens;
        private long _lastRefillTickCount64;

        internal RitsuLibSidecarTokenBucket(int packetsPerSecond, int bytesPerSecond)
            : this(packetsPerSecond, bytesPerSecond, static () => Environment.TickCount64)
        {
        }

        internal RitsuLibSidecarTokenBucket(
            int packetsPerSecond,
            int bytesPerSecond,
            Func<long> tickCount64)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(packetsPerSecond, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(bytesPerSecond, 1);
            ArgumentNullException.ThrowIfNull(tickCount64);
            _packetCapacity = packetsPerSecond;
            _byteCapacity = bytesPerSecond;
            _packetsPerMillisecond = packetsPerSecond / 1000d;
            _bytesPerMillisecond = bytesPerSecond / 1000d;
            _tickCount64 = tickCount64;
            _packetTokens = _packetCapacity;
            _byteTokens = _byteCapacity;
            _lastRefillTickCount64 = _tickCount64();
        }

        internal bool TryConsume(int bytes)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(bytes);
            lock (_gate)
            {
                Refill(_tickCount64());
                if (_packetTokens < 1d || _byteTokens < bytes)
                    return false;
                _packetTokens -= 1d;
                _byteTokens -= bytes;
                return true;
            }
        }

        private void Refill(long now)
        {
            var elapsed = now - _lastRefillTickCount64;
            if (elapsed <= 0)
                return;

            _packetTokens = Math.Min(_packetCapacity, _packetTokens + elapsed * _packetsPerMillisecond);
            _byteTokens = Math.Min(_byteCapacity, _byteTokens + elapsed * _bytesPerMillisecond);
            _lastRefillTickCount64 = now;
        }
    }
}
