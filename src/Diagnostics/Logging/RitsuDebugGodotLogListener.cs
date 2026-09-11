using Godot;
using Godot.Collections;
using System.Text;

namespace STS2RitsuLib.Diagnostics.Logging
{
    internal sealed partial class RitsuDebugGodotLogListener : Logger
    {
        [ThreadStatic] private static bool _inCallback;

        public override void _LogMessage(string message, bool error)
        {
            if (_inCallback)
                return;
            RitsuDebugLogPipeline.EmitGodotLogMessage(message, error);
        }

        public override void _LogError(
            string function,
            string file,
            int line,
            string code,
            string rationale,
            bool editorNotify,
            int errorType,
            Array<ScriptBacktrace> scriptBacktraces)
        {
            if (_inCallback)
                return;
            _inCallback = true;
            try
            {
                RitsuDebugLogPipeline.EmitGodotLogError(function, file, line, code, rationale, errorType,
                    FormatScriptBacktraces(scriptBacktraces));
            }
            catch (Exception exception) when (RitsuLibExceptionPolicy.IsRecoverable(exception))
            {
                RitsuDebugLogPipeline.ReportInternalWarning($"Godot error callback failed: {exception.Message}");
            }
            finally
            {
                _inCallback = false;
            }
        }

        private static string FormatScriptBacktraces(Array<ScriptBacktrace> scriptBacktraces)
        {
            var result = new StringBuilder();
            foreach (var backtrace in scriptBacktraces.Take(8))
            {
                if (backtrace.IsEmpty())
                    continue;
                var text = backtrace.Format();
                result.Append(text.AsSpan(0, Math.Min(text.Length, 16384 - result.Length)));
                if (result.Length >= 16384)
                    break;
            }

            return result.ToString();
        }
    }
}
