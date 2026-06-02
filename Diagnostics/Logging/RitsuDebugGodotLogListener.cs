using Godot;
using Godot.Collections;

namespace STS2RitsuLib.Diagnostics.Logging
{
    internal sealed partial class RitsuDebugGodotLogListener : Logger
    {
        public override void _LogMessage(string message, bool error)
        {
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
            RitsuDebugLogPipeline.EmitGodotLogError(
                function,
                file,
                line,
                code,
                rationale,
                errorType,
                FormatScriptBacktraces(scriptBacktraces));
        }

        private static string FormatScriptBacktraces(Array<ScriptBacktrace> scriptBacktraces)
        {
            var lines =
                (from backtrace in scriptBacktraces where !backtrace.IsEmpty() select backtrace.Format()).ToList();

            return string.Join("", lines);
        }
    }
}
