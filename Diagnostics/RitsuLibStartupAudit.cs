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
        private static readonly List<(string Phase, double Milliseconds)> Phases = [];
        private static int _reportedCount;

        /// <summary>
        ///     Times <paramref name="action" /> and records its duration under <paramref name="phase" />.
        ///     对 <paramref name="action" /> 计时，并以 <paramref name="phase" /> 记录其耗时。
        /// </summary>
        internal static void Measure(string phase, Action action)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                action();
            }
            finally
            {
                sw.Stop();
                Record(phase, sw.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        ///     Times <paramref name="func" /> and records its duration under <paramref name="phase" />, returning the result.
        ///     对 <paramref name="func" /> 计时并以 <paramref name="phase" /> 记录其耗时，返回其结果。
        /// </summary>
        internal static T Measure<T>(string phase, Func<T> func)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                return func();
            }
            finally
            {
                sw.Stop();
                Record(phase, sw.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        ///     Records a pre-measured phase duration.
        ///     记录一个已测得的阶段耗时。
        /// </summary>
        internal static void Record(string phase, double milliseconds)
        {
            lock (Gate)
            {
                Phases.Add((phase, milliseconds));
            }
        }

        /// <summary>
        ///     Logs the phases recorded since the previous report as one consolidated block, including a running total
        ///     of all RitsuLib self-time recorded so far.
        ///     将自上次报告以来记录的阶段作为一个合并块输出，并附带迄今为止记录的 RitsuLib 自身耗时累计值。
        /// </summary>
        internal static void LogReport(string title)
        {
            lock (Gate)
            {
                if (Phases.Count <= _reportedCount)
                    return;

                var segment = Phases.GetRange(_reportedCount, Phases.Count - _reportedCount);
                var segmentTotal = segment.Sum(static entry => entry.Milliseconds);
                var grandTotal = Phases.Sum(static entry => entry.Milliseconds);

                var text = new StringBuilder()
                    .AppendLine()
                    .AppendLine($"=== RitsuLib Startup Audit: {title} ===");

                foreach (var (phase, milliseconds) in segment)
                    text.AppendLine($"  {phase}: {milliseconds:F1} ms");

                text.AppendLine("  ---")
                    .AppendLine($"  segment total: {segmentTotal:F1} ms")
                    .Append($"  RitsuLib self-time total so far: {grandTotal:F1} ms");

                _reportedCount = Phases.Count;
                RitsuLibFramework.Logger.Info(text.ToString());
            }
        }
    }
}
