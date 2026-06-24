namespace STS2RitsuLib.Diagnostics.Logging
{
    internal sealed class RitsuDebugLogRingBuffer
    {
        private readonly RitsuDebugLogRecord[] _items;
        private readonly Lock _lock = new();
        private int _count;
        private int _next;

        public RitsuDebugLogRingBuffer(int capacity)
        {
            _items = new RitsuDebugLogRecord[Math.Max(128, capacity)];
        }

        public int Capacity => _items.Length;

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _count;
                }
            }
        }

        public void Add(RitsuDebugLogRecord record)
        {
            lock (_lock)
            {
                _items[_next] = record;
                _next = (_next + 1) % _items.Length;
                if (_count < _items.Length)
                    _count++;
            }
        }

        public RitsuDebugLogRecord[] Snapshot(int limit)
        {
            lock (_lock)
            {
                var count = limit <= 0 ? _count : Math.Min(_count, limit);
                var result = new RitsuDebugLogRecord[count];
                var start = (_next - _count + _items.Length) % _items.Length;
                start = (start + Math.Max(0, _count - count)) % _items.Length;

                for (var i = 0; i < count; i++)
                    result[i] = _items[(start + i) % _items.Length];

                return result;
            }
        }
    }
}
