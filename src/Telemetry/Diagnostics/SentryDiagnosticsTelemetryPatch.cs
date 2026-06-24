using MegaCrit.Sts2.Core.Debug;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Telemetry.Diagnostics;

namespace STS2RitsuLib.Diagnostics.Patches
{
    internal class SentryDiagnosticsTelemetryPatch : IPatchMethod
    {
        public static string PatchId => "sentry_diagnostics_telemetry";

        public static string Description => "Mirror vanilla Sentry exception captures into authorized telemetry";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            var exceptionOnly = new ModPatchTarget(
                typeof(SentryService),
                nameof(SentryService.CaptureException),
                [typeof(Exception)]);

            var scopeType = Type.GetType("Sentry.Scope, Sentry", false);
            if (scopeType == null)
                return [exceptionOnly];

            var configureScopeType = typeof(Action<>).MakeGenericType(scopeType);
            return
            [
                exceptionOnly,
                new(
                    typeof(SentryService),
                    nameof(SentryService.CaptureException),
                    [typeof(Exception), configureScopeType],
                    true),
            ];
        }

        public static void Postfix(Exception ex)
        {
            DiagnosticsTelemetryCollector.CaptureExceptionForAuthorizedApplicants(
                ex,
                "vanilla_sentry");
        }
    }
}
