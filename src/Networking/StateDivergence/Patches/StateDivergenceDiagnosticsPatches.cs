using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Checksums;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.StateDivergence.Patches
{
    internal static class StateDivergenceDiagnosticsReports
    {
        public static readonly ConditionalWeakTable<NErrorPopup, StateDivergenceDiagnosticReport> PopupReports = new();
        private static StateDivergenceDiagnosticReport? _latestReport;
        private static StateDivergenceDiagnosticReport? _latestLogReport;
        private static string? _latestBundlePath;
        private static string? _latestBundleError;
        private static bool _latestReportLogged;

        public static void Store(
            StateDivergenceDiagnosticReport report,
            StateDivergenceDiagnosticReport logReport,
            string? bundlePath,
            string? bundleError)
        {
            _latestReport = report;
            _latestLogReport = logReport;
            _latestBundlePath = bundlePath;
            _latestBundleError = bundleError;
            _latestReportLogged = false;
        }

        public static bool TryGetLatest(out StateDivergenceDiagnosticReport report)
        {
            report = _latestReport!;
            return report != null;
        }

        public static void TryLogLatestToGameLog(string trigger)
        {
            if (_latestLogReport == null || _latestReportLogged)
                return;

            _latestReportLogged = true;
            try
            {
                if (!string.IsNullOrWhiteSpace(_latestBundlePath))
                {
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[State divergence diagnostics: {trigger}] Diagnostic bundle written: {_latestBundlePath}");
                    return;
                }

                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[State divergence diagnostics: {trigger}] Failed to write diagnostic bundle: {_latestBundleError ?? "unknown error"}");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[State divergence diagnostics] failed to write report to log: {ex.Message}");
            }
        }
    }

    internal sealed class StateDivergenceSupplementSerializePatch : IPatchMethod
    {
        public static string PatchId => "state_divergence_supplement_serialize";
        public static bool IsCritical => false;

        public static string Description =>
            "Append compressed RitsuLib divergence diagnostics to state divergence messages.";

        public static ModPatchTarget[] GetTargets()
        {
            StateDivergenceSupplementPayloadCodec.EnsureRegistered();
            return
            [
                new(typeof(StateDivergenceMessage), nameof(StateDivergenceMessage.Serialize), [typeof(PacketWriter)]),
            ];
        }

        public static void Postfix(StateDivergenceMessage __instance, PacketWriter writer)
        {
            StateDivergenceSupplementPayloadCodec.Write(writer, __instance);
        }
    }

    internal sealed class StateDivergenceSupplementDeserializePatch : IPatchMethod
    {
        public static string PatchId => "state_divergence_supplement_deserialize";
        public static bool IsCritical => false;

        public static string Description =>
            "Read compressed RitsuLib divergence diagnostics from state divergence messages.";

        public static ModPatchTarget[] GetTargets()
        {
            StateDivergenceSupplementPayloadCodec.EnsureRegistered();
            return
            [
                new(typeof(StateDivergenceMessage), nameof(StateDivergenceMessage.Deserialize), [typeof(PacketReader)]),
            ];
        }

        public static void Postfix(PacketReader reader)
        {
            StateDivergenceSupplementPayloadCodec.Read(reader);
        }
    }

    internal sealed class StateDivergenceDiagnosticsLogPatch : IPatchMethod
    {
        private const BindingFlags InstanceFieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static string PatchId => "state_divergence_diagnostics_panel";
        public static bool IsCritical => false;

        public static string Description =>
            "Show a structured RitsuLib diagnostics panel for multiplayer state divergence.";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ChecksumTracker), "LogStateDivergence")];
        }

        public static void Prefix(
            ChecksumTracker __instance,
            object localChecksum,
            StateDivergenceMessage message,
            ulong remoteId)
        {
            try
            {
                if (!TryReadTrackedState(localChecksum, out var local))
                    return;

                var role = TryReadRole(__instance);
                var localSupplement = StateDivergenceSupplementPayloadCodec.CreateLocalSnapshot(local.Checksum);
                var hasRemoteSupplement =
                    StateDivergenceSupplementStore.TryTake(message.senderChecksum, out var remoteSupplement);
                var activeRemoteSupplement = hasRemoteSupplement ? remoteSupplement : null;
                if (string.Equals(role, "Client", StringComparison.Ordinal))
                    StateDivergenceSupplementPayloadCodec.PrepareOutgoingSnapshot(localSupplement);

                var report = StateDivergenceDiagnosticReportBuilder.Build(local, message, remoteId, role,
                    localSupplement, activeRemoteSupplement);
                using var english = StateDivergenceDiagnosticsLocalization.UseEnglish();
                var logReport = StateDivergenceDiagnosticReportBuilder.Build(local, message, remoteId, role,
                    localSupplement, activeRemoteSupplement);
                StateDivergenceLogBundleWriter.TryWrite(
                    logReport,
                    localSupplement.RecentLogs,
                    activeRemoteSupplement?.RecentLogs,
                    "LogStateDivergence",
                    out var bundlePath,
                    out var bundleFileName,
                    out var bundleError);
                report = report with
                {
                    BundleFileName = bundleFileName,
                    BundlePath = bundlePath,
                    BundleError = bundleError,
                };
                logReport = logReport with
                {
                    BundleFileName = bundleFileName,
                    BundlePath = bundlePath,
                    BundleError = bundleError,
                };
                StateDivergenceDiagnosticsReports.Store(report, logReport, bundlePath, bundleError);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[State divergence diagnostics] failed: {ex.Message}");
            }
        }

        private static bool TryReadTrackedState(object source, out StateDivergenceTrackedState tracked)
        {
            tracked = default;
            var type = source.GetType();
            var dataField = type.GetField("data", InstanceFieldFlags);
            var contextField = type.GetField("context", InstanceFieldFlags);
            var fullStateField = type.GetField("fullState", InstanceFieldFlags);
            if (dataField?.GetValue(source) is not NetChecksumData data ||
                fullStateField?.GetValue(source) is not NetFullCombatState fullState)
                return false;

            var context = contextField?.GetValue(source) as string ?? "";
            tracked = new(data, context, fullState);
            return true;
        }

        private static string TryReadRole(ChecksumTracker tracker)
        {
            return typeof(ChecksumTracker).GetField("_netService", InstanceFieldFlags)?.GetValue(tracker)
                is not INetGameService service
                ? "Unknown"
                : service.Type.ToString();
        }
    }

    internal sealed class StateDivergenceDiagnosticsPopupCreatePatch : IPatchMethod
    {
        public static string PatchId => "state_divergence_diagnostics_popup_create";
        public static bool IsCritical => false;

        public static string Description =>
            "Attach RitsuLib state divergence diagnostics to state divergence error popups.";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NErrorPopup), nameof(NErrorPopup.Create), [typeof(NetErrorInfo)])];
        }

        public static void Postfix(NetErrorInfo info, NErrorPopup? __result)
        {
            if (__result == null || info.GetReason() != NetError.StateDivergence)
                return;
            if (!StateDivergenceDiagnosticsReports.TryGetLatest(out var report))
                return;

            StateDivergenceDiagnosticsReports.PopupReports.Remove(__result);
            StateDivergenceDiagnosticsReports.PopupReports.Add(__result, report);
            StateDivergenceDiagnosticsReports.TryLogLatestToGameLog("error popup created");
        }
    }

    internal sealed class StateDivergenceDiagnosticsPopupReadyPatch : IPatchMethod
    {
        public static string PatchId => "state_divergence_diagnostics_popup_ready";
        public static bool IsCritical => false;
        public static string Description => "Add a details button to state divergence error popups.";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NErrorPopup), "_Ready", [])];
        }

        public static void Postfix(NErrorPopup __instance)
        {
            if (StateDivergenceDiagnosticsReports.PopupReports.TryGetValue(__instance, out var report))
                StateDivergenceDiagnosticsPopup.WireDetailsButton(__instance, report);
        }
    }
}
