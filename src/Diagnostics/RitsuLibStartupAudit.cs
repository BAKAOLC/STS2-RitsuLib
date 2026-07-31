using System.Diagnostics;
using System.Text;

namespace STS2RitsuLib.Diagnostics
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Accumulates wall-clock durations for RitsuLib's own startup phases, including bootstrap work, patch
    ///         application, and internal lifecycle hooks, and writes consolidated audit reports to the log. Time spent
    ///         between these phases in the engine or other mods is excluded.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         累计 RitsuLib 自身各启动阶段的墙钟耗时，包括引导初始化、补丁应用和内部生命周期钩子，并向日志写入
    ///         汇总审计报告。各阶段之间由引擎或其他模组消耗的时间不计入其中。
    ///     </para>
    /// </summary>
    internal static class RitsuLibStartupAudit
    {
        private static readonly Lock Gate = new();
        private static readonly List<PhaseTiming> Phases = [];
        private static int _reportedCount;

        [ThreadStatic] private static MeasureScope? _currentScope;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Measures <paramref name="action" /> and records its duration under <paramref name="phase" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         测量 <paramref name="action" /> 的耗时，并以 <paramref name="phase" /> 作为阶段名称记录。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Measures <paramref name="func" />, records its duration under <paramref name="phase" />, and returns
        ///         the function result.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         测量 <paramref name="func" /> 的耗时并以 <paramref name="phase" /> 作为阶段名称记录，然后返回函数
        ///         结果。
        ///     </para>
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
        ///     <para xml:lang="en">Records a previously measured phase duration.</para>
        ///     <para xml:lang="zh-CN">记录一个已测量的阶段耗时。</para>
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
        ///     <para xml:lang="en">
        ///         Logs all RitsuLib self-time phases recorded so far as one consolidated block with an exclusive-time
        ///         total.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将截至当前记录的所有 RitsuLib 自身耗时阶段作为一个汇总块写入日志，并附带独占耗时总计。
        ///     </para>
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
