using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace STS2RitsuLib.Networking.JoinDiagnostics
{
    internal static class JoinFailureDiagnosticsPopup
    {
        public static string DetailsButtonText =>
            JoinFailureDiagnosticsLocalization.Get("button.details", "Details");

        public static void ShowDeferred(JoinFailureDiagnosticReport report)
        {
            Callable.From(() =>
            {
                NModalContainer.Instance?.Clear();
                Show(report);
            }).CallDeferred();
        }

        public static void WireDetailsButton(NErrorPopup popup, JoinFailureDiagnosticReport report)
        {
            DiagnosticPopupDetailsButton.Add(popup, DetailsButtonText, () => ShowDeferred(report));
        }

        private static void Show(JoinFailureDiagnosticReport report)
        {
            if (NModalContainer.Instance == null)
                return;

            NModalContainer.Instance.Add(new JoinFailureDiagnosticsPanel(report));
        }
    }
}
