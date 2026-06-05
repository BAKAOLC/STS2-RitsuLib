using MegaCrit.Sts2.Core.Debug;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Telemetry.Diagnostics;

namespace STS2RitsuLib.Diagnostics.Patches
{
    /// <summary>
    ///     Mirrors vanilla Sentry exception captures into authorized RitsuLib diagnostics telemetry applicants.
    ///     将原版 Sentry 异常捕获镜像到已授权的 RitsuLib diagnostics 遥测申请方。
    /// </summary>
    public class SentryDiagnosticsTelemetryPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "sentry_diagnostics_telemetry";

        /// <inheritdoc />
        public static string Description => "Mirror vanilla Sentry exception captures into authorized telemetry";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
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

        /// <summary>
        ///     Harmony postfix: when vanilla reports an exception, forward the same exception through RitsuLib telemetry.
        ///     Harmony postfix：当原版上报异常时，通过 RitsuLib 遥测转发同一个异常。
        /// </summary>
        public static void Postfix(Exception ex)
        {
            DiagnosticsTelemetryCollector.CaptureExceptionForAuthorizedApplicants(
                ex,
                "vanilla_sentry");
        }
    }
}
