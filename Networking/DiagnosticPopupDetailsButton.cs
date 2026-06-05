using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace STS2RitsuLib.Networking
{
    internal static class DiagnosticPopupDetailsButton
    {
        private const string AttachedMeta = "ritsu_diagnostics_details_button_attached";

        public static void Add(NErrorPopup popup, string text, Action open)
        {
            var verticalPopup = popup.GetNodeOrNull<NVerticalPopup>("VerticalPopup");
            if (verticalPopup == null || popup.HasMeta(AttachedMeta) || verticalPopup.NoButton.Visible)
                return;

            popup.SetMeta(AttachedMeta, true);

            Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(verticalPopup))
                    return;

                var details = verticalPopup.NoButton;
                details.Visible = true;
                details.IsYes = false;
                details.SetText(text);
                details.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(_ => open()));
            }).CallDeferred();
        }
    }
}
