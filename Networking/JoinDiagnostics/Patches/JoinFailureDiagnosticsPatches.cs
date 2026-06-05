using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Connection;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.JoinDiagnostics.Patches
{
    internal sealed class JoinFailureDiagnosticsBeginAttemptPatch : IPatchMethod
    {
        public static string PatchId => "join_failure_diagnostics_begin_attempt";
        public static bool IsCritical => false;
        public static string Description => "Track multiplayer join attempts for enhanced failure diagnostics";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NJoinFriendScreen), nameof(NJoinFriendScreen.JoinGameAsync),
                    [typeof(IClientConnectionInitializer)]),
            ];
        }

        public static void Prefix()
        {
            JoinFailureDiagnosticsService.BeginJoinAttempt();
        }
    }

    internal sealed class JoinFailureDiagnosticsInitialInfoPatch : IPatchMethod
    {
        public static string PatchId => "join_failure_diagnostics_initial_game_info";
        public static bool IsCritical => false;
        public static string Description => "Capture host game-info handshake data for join failure diagnostics";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(JoinFlow), "HandleInitialGameInfoMessage", [typeof(InitialGameInfoMessage), typeof(ulong)]),
            ];
        }

        public static void Prefix(InitialGameInfoMessage message)
        {
            JoinFailureDiagnosticsService.ObserveInitialGameInfo(message);
        }
    }

    internal sealed class JoinFailureDiagnosticsInitialInfoSerializePatch : IPatchMethod
    {
        public static string PatchId => "join_failure_diagnostics_initial_game_info_serialize";
        public static bool IsCritical => false;

        public static string Description =>
            "Append registered RitsuLib join diagnostics data to the initial game-info message";

        public static ModPatchTarget[] GetTargets()
        {
            JoinDiagnosticsPayloadCodec.EnsureRegistered();
            return
            [
                new(typeof(InitialGameInfoMessage), nameof(InitialGameInfoMessage.Serialize), [typeof(PacketWriter)]),
            ];
        }

        public static void Postfix(InitialGameInfoMessage __instance, PacketWriter writer)
        {
            JoinDiagnosticsPayloadCodec.Write(writer, __instance);
        }
    }

    internal sealed class JoinFailureDiagnosticsInitialInfoDeserializePatch : IPatchMethod
    {
        public static string PatchId => "join_failure_diagnostics_initial_game_info_deserialize";
        public static bool IsCritical => false;

        public static string Description =>
            "Read registered RitsuLib join diagnostics data from the initial game-info message";

        public static ModPatchTarget[] GetTargets()
        {
            JoinDiagnosticsPayloadCodec.EnsureRegistered();
            return
            [
                new(typeof(InitialGameInfoMessage), nameof(InitialGameInfoMessage.Deserialize), [typeof(PacketReader)]),
            ];
        }

        public static void Postfix(PacketReader reader)
        {
            JoinDiagnosticsPayloadCodec.Read(reader);
        }
    }

    internal static class JoinFailureDiagnosticsPopupReports
    {
        public static readonly ConditionalWeakTable<NErrorPopup, JoinFailureDiagnosticReport> Reports = new();
    }

    internal sealed class JoinFailureDiagnosticsPopupCreatePatch : IPatchMethod
    {
        public static string PatchId => "join_failure_diagnostics_popup_create";
        public static bool IsCritical => false;
        public static string Description => "Attach RitsuLib join diagnostics to network error popups";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NErrorPopup), nameof(NErrorPopup.Create), [typeof(NetErrorInfo)])];
        }

        public static void Postfix(NetErrorInfo info, NErrorPopup? __result)
        {
            if (__result == null) return;
            if (!JoinFailureDiagnosticsService.TryCreateReport(info, out var report)) return;

            JoinFailureDiagnosticsPopupReports.Reports.Remove(__result);
            JoinFailureDiagnosticsPopupReports.Reports.Add(__result, report);
        }
    }

    internal sealed class JoinFailureDiagnosticsPopupReadyPatch : IPatchMethod
    {
        public static string PatchId => "join_failure_diagnostics_popup_ready";
        public static bool IsCritical => false;
        public static string Description => "Add a localized details button to join failure popups";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NErrorPopup), "_Ready", [])];
        }

        public static void Postfix(NErrorPopup __instance)
        {
            if (JoinFailureDiagnosticsPopupReports.Reports.TryGetValue(__instance, out var report))
                JoinFailureDiagnosticsPopup.WireDetailsButton(__instance, report);
        }
    }
}
