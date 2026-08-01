using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Connection;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
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

    internal static class JoinFailureDiagnosticsPopupReports
    {
        public static readonly ConditionalWeakTable<NErrorPopup, JoinFailureDiagnosticReport> Reports = [];
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
