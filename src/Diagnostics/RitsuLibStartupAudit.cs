using System.Diagnostics;
using System.Text;

namespace STS2RitsuLib.Diagnostics
{
    /// <summary>
    ///     Accumulates wall-clock durations of RitsuLib's own startup phases (bootstraps, patch application, and
    ///     framework-internal lifecycle hooks) and emits consolidated audit reports to the log. Only time spent inside
    ///     RitsuLib code is recorded; the gaps where the engine or other mods run are deliberately excluded, so the
    ///     totals reflect RitsuLib's own startup cost.
    ///     累计 RitsuLib 自身启动各阶段（bootstrap、补丁应用、框架内部生命周期钩子）的墙钟耗时，
    ///     并向日志输出合并后的审计报告。仅记录在 RitsuLib 代码内消耗的时间；引擎或其它 mod 运行的
    ///     空档被有意排除，因此汇总值反映的是 RitsuLib 自身的启动开销。
    /// </summary>
    internal static class RitsuLibStartupAudit
    {
        private static readonly Lock Gate = new();
        private static readonly List<PhaseTiming> Phases = [];
        private static int _reportedCount;

        [ThreadStatic] private static MeasureScope? _currentScope;

        /// <summary>
        ///     Times <paramref name="action" /> and records its duration under <paramref name="phase" />.
        ///     对 <paramref name="action" /> 计时，并以 <paramref name="phase" /> 记录其耗时。
        /// </summary>
        internal static void Measure(string phase, Action action)
        {
            var scope = PushScope(phase);
            var sw = Stopwatch.StartNew();
            try
            {
                action();
            }
            finally
            {
                sw.Stop();
                PopScope(scope, sw.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        ///     Times <paramref name="func" /> and records its duration under <paramref name="phase" />, returning the result.
        ///     对 <paramref name="func" /> 计时并以 <paramref name="phase" /> 记录其耗时，返回其结果。
        /// </summary>
        internal static T Measure<T>(string phase, Func<T> func)
        {
            var scope = PushScope(phase);
            var sw = Stopwatch.StartNew();
            try
            {
                return func();
            }
            finally
            {
                sw.Stop();
                PopScope(scope, sw.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        ///     Records a pre-measured phase duration.
        ///     记录一个已测得的阶段耗时。
        /// </summary>
        internal static void Record(string phase, double milliseconds)
        {
            _currentScope?.AddChild(milliseconds);
            lock (Gate)
            {
                Phases.Add(new(phase, milliseconds, milliseconds));
            }
        }

        /// <summary>
        ///     Logs every RitsuLib self-time phase recorded so far as a single consolidated block with one total line.
        ///     将迄今为止记录的所有 RitsuLib 自身耗时阶段作为单个合并块输出，并附带一行总计。
        /// </summary>
        internal static void LogReport(string title)
        {
            lock (Gate)
            {
                if (Phases.Count <= _reportedCount)
                    return;

                var total = Phases.Sum(static entry => entry.ExclusiveMilliseconds);
                var text = new StringBuilder()
                    .AppendLine()
                    .AppendLine($"=== RitsuLib Startup Audit: {title} ===");

                foreach (var timing in Phases)
                {
                    text.Append($"  {timing.Phase}: {timing.ExclusiveMilliseconds:F1} ms");
                    if (Math.Abs(timing.InclusiveMilliseconds - timing.ExclusiveMilliseconds) >= 0.05d)
                        text.Append($" (inclusive {timing.InclusiveMilliseconds:F1} ms)");

                    text.AppendLine();
                }

                text.AppendLine("  ---")
                    .Append($"  RitsuLib exclusive self-time total: {total:F1} ms");

                _reportedCount = Phases.Count;
                RitsuLibFramework.Logger.Info(text.ToString());
            }
        }

        private static MeasureScope PushScope(string phase)
        {
            var scope = new MeasureScope(phase, _currentScope);
            _currentScope = scope;
            return scope;
        }

        private static void PopScope(MeasureScope scope, double inclusiveMilliseconds)
        {
            _currentScope = scope.Parent;
            scope.Parent?.AddChild(inclusiveMilliseconds);

            var exclusiveMilliseconds = Math.Max(0d, inclusiveMilliseconds - scope.ChildMilliseconds);
            lock (Gate)
            {
                Phases.Add(new(scope.Phase, inclusiveMilliseconds, exclusiveMilliseconds));
            }
        }

        private sealed class MeasureScope(string phase, MeasureScope? parent)
        {
            internal string Phase { get; } = phase;
            internal MeasureScope? Parent { get; } = parent;
            internal double ChildMilliseconds { get; private set; }

            internal void AddChild(double milliseconds)
            {
                ChildMilliseconds += milliseconds;
            }
        }

        private readonly record struct PhaseTiming(
            string Phase,
            double InclusiveMilliseconds,
            double ExclusiveMilliseconds);
    }
}
