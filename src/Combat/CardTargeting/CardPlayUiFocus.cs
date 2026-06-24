#if !STS2_AT_LEAST_0_104_0
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
#else
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
#endif

namespace STS2RitsuLib.Combat.CardTargeting
{
    internal static class CardPlayUiFocus
    {
        internal static void AfterCardPlayFinished()
        {
#if !STS2_AT_LEAST_0_104_0
            NCombatRoom.Instance?.Ui.Hand.DefaultFocusedControl.TryGrabFocus();
#else
            ActiveScreenContext.Instance.FocusOnDefaultControl();
#endif
        }
    }
}
