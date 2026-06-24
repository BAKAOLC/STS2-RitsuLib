using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace STS2RitsuLib.Networking.StateDivergence
{
    internal static class StateDivergenceDiagnosticsPopup
    {
        public static string DetailsButtonText =>
            StateDivergenceDiagnosticsLocalization.Get("button.details", "Details");

        public static void ShowDeferred(StateDivergenceDiagnosticReport report)
        {
            Callable.From(() =>
            {
                NModalContainer.Instance?.Clear();
                Show(report);
            }).CallDeferred();
        }

        public static void WireDetailsButton(NErrorPopup popup, StateDivergenceDiagnosticReport report)
        {
            DiagnosticPopupDetailsButton.Add(popup, DetailsButtonText, () => ShowDeferred(report));
        }

        private static void Show(StateDivergenceDiagnosticReport report)
        {
            NModalContainer.Instance?.Add(new StateDivergenceDiagnosticsPanel(report));
        }
    }
}
