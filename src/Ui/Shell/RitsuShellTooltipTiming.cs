using Godot;

namespace STS2RitsuLib.Ui.Shell
{
    internal static class RitsuShellTooltipTiming
    {
        private const double DefaultDelaySeconds = 0.5d;
        private const string TooltipDelaySetting = "gui/timers/tooltip_delay_sec";
        private static readonly Dictionary<long, double> Requests = [];
        private static readonly Lock SyncRoot = new();
        private static double _baselineDelay;
        private static double _managedDelay;
        private static bool _settingManaged;
        private static long _nextRequestId;

        internal static IDisposable Acquire(double delaySeconds)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(delaySeconds, 0d);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(delaySeconds, DefaultDelaySeconds);

            lock (SyncRoot)
            {
                var requestId = ++_nextRequestId;
                Requests.Add(requestId, delaySeconds);
                ApplyCurrentRequest();
                return new Lease(requestId);
            }
        }

        private static void ApplyCurrentRequest()
        {
            var currentDelay = ReadCurrentDelay();
            var currentDelayIsManaged = _settingManaged && ApproximatelyEqual(currentDelay, _managedDelay);
            if (_settingManaged && !currentDelayIsManaged)
            {
                _baselineDelay = currentDelay;
                _settingManaged = false;
            }

            if (Requests.Count == 0)
            {
                if (currentDelayIsManaged)
                    ProjectSettings.SetSetting(TooltipDelaySetting, _baselineDelay);
                _settingManaged = false;
                return;
            }

            var requestedDelay = Requests.Values.Min();
            if (currentDelayIsManaged)
            {
                if (!ApproximatelyEqual(currentDelay, requestedDelay))
                    ProjectSettings.SetSetting(TooltipDelaySetting, requestedDelay);
                _managedDelay = requestedDelay;
                return;
            }

            if (!_settingManaged)
                _baselineDelay = currentDelay;
            if (currentDelay <= requestedDelay)
            {
                _settingManaged = false;
                return;
            }

            ProjectSettings.SetSetting(TooltipDelaySetting, requestedDelay);
            _managedDelay = requestedDelay;
            _settingManaged = true;
        }

        private static double ReadCurrentDelay()
        {
            return ProjectSettings.GetSetting(TooltipDelaySetting, DefaultDelaySeconds).AsDouble();
        }

        private static bool ApproximatelyEqual(double left, double right)
        {
            return Math.Abs(left - right) <= 0.0001d;
        }

        private static void Release(long requestId)
        {
            lock (SyncRoot)
            {
                if (!Requests.Remove(requestId))
                    return;
                ApplyCurrentRequest();
            }
        }

        private sealed class Lease(long requestId) : IDisposable
        {
            private long _requestId = requestId;

            public void Dispose()
            {
                var releasedId = Interlocked.Exchange(ref _requestId, 0L);
                if (releasedId != 0L)
                    Release(releasedId);
            }
        }
    }
}
