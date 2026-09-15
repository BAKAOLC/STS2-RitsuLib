namespace STS2RitsuLib.Telemetry.Diagnostics
{
    internal sealed class TelemetryDiagnosticsScope : IDisposable
    {
        private static readonly AsyncLocal<int> Depth = new();
        private readonly int _previous = Depth.Value;
        private bool _disposed;

        internal static bool IsActive => Depth.Value > 0;

        internal TelemetryDiagnosticsScope()
        {
            Depth.Value = _previous + 1;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Depth.Value = _previous;
        }
    }
}
