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
            Callable.From(() => Show(report)).CallDeferred();
        }

        public static void WireDetailsButton(NErrorPopup popup, JoinFailureDiagnosticReport report)
        {
            var verticalPopup = popup.GetNodeOrNull<NVerticalPopup>("VerticalPopup");
            if (verticalPopup == null)
                return;

            verticalPopup.InitNoButton(
                new("main_menu_ui", "GENERIC_POPUP.ok"),
                _ => ShowDeferred(report));
            verticalPopup.NoButton.SetText(DetailsButtonText);
            verticalPopup.NoButton.DisconnectHotkeys();
            verticalPopup.YesButton.FocusMode = Control.FocusModeEnum.All;
            verticalPopup.NoButton.FocusMode = Control.FocusModeEnum.All;

            var yesPath = verticalPopup.YesButton.GetPath();
            var noPath = verticalPopup.NoButton.GetPath();
            verticalPopup.YesButton.FocusNeighborLeft = noPath;
            verticalPopup.YesButton.FocusNeighborRight = noPath;
            verticalPopup.NoButton.FocusNeighborLeft = yesPath;
            verticalPopup.NoButton.FocusNeighborRight = yesPath;
            Callable.From(() => verticalPopup.YesButton.GrabFocus()).CallDeferred();
        }

        private static void Show(JoinFailureDiagnosticReport report)
        {
            if (NModalContainer.Instance == null)
                return;

            NModalContainer.Instance.Add(new JoinFailureDiagnosticsPanel(report));
        }
    }
}
