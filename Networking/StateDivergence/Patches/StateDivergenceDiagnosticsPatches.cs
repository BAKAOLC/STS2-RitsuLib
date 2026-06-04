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

        public static void Store(StateDivergenceDiagnosticReport report)
        {
            _latestReport = report;
        }

        public static bool TryGetLatest(out StateDivergenceDiagnosticReport report)
        {
            report = _latestReport!;
            return report != null;
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

        // ReSharper disable once InconsistentNaming
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

        // ReSharper disable once InconsistentNaming
        public static void Postfix(
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
                var report = StateDivergenceDiagnosticReportBuilder.Build(local, message, remoteId, role);
                StateDivergenceDiagnosticsReports.Store(report);
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

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NetErrorInfo info, NErrorPopup? __result)
        {
            if (__result == null || info.GetReason() != NetError.StateDivergence)
                return;
            if (!StateDivergenceDiagnosticsReports.TryGetLatest(out var report))
                return;

            StateDivergenceDiagnosticsReports.PopupReports.Remove(__result);
            StateDivergenceDiagnosticsReports.PopupReports.Add(__result, report);
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

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NErrorPopup __instance)
        {
            if (StateDivergenceDiagnosticsReports.PopupReports.TryGetValue(__instance, out var report))
                StateDivergenceDiagnosticsPopup.WireDetailsButton(__instance, report);
        }
    }
}
