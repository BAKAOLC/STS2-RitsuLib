using System.Collections.Concurrent;

namespace STS2RitsuLib.Diagnostics.Logging
{
    internal sealed class RitsuDebugLogSseClient
    {
        private const int MaxPendingEvents = 512;

        private readonly ConcurrentQueue<string> _pending = new();
        private readonly SemaphoreSlim _signal = new(0);
        private int _count;

        public void Enqueue(string json)
        {
            if (Interlocked.Increment(ref _count) > MaxPendingEvents)
            {
                Interlocked.Decrement(ref _count);
                return;
            }

            _pending.Enqueue(json);
            _signal.Release();
        }

        public async Task<string?> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            try
            {
                if (!await _signal.WaitAsync(timeout, cancellationToken).ConfigureAwait(false))
                    return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            if (!_pending.TryDequeue(out var json))
                return null;

            Interlocked.Decrement(ref _count);
            return json;
        }
    }
}
