using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Ui.Overlay
{
    internal sealed class RitsuOverlayHoverTipLayerPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_overlay_hover_tip_layer";
        public static string Description => "Display hover tips above visible RitsuLib overlay windows";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NHoverTipSet), "CreateAndShow",
                    [typeof(Control), typeof(IEnumerable<IHoverTip>), typeof(HoverTipAlignment)]),
            ];
        }

        public static void Postfix(Control owner, NHoverTipSet? __result)
        {
            RitsuOverlayHostService.TryAttachHoverTips(owner, __result);
        }
    }
}
