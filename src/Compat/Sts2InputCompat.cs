using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace STS2RitsuLib.Compat
{
    internal static class Sts2InputCompat
    {
        public static StringName ConfirmAction
        {
            get
            {
#if STS2_AT_LEAST_0_110_0
                return MegaInput.confirm;
#else
                return MegaInput.accept;
#endif
            }
        }

        public static StringName CancelCardPlayAction
        {
            get
            {
#if STS2_AT_LEAST_0_110_0
                return MegaInput.cancel;
#else
                return MegaInput.releaseCard;
#endif
            }
        }

        public static bool IsUsingDirectionalNavigation
        {
            get
            {
#if STS2_AT_LEAST_0_110_0
                return NControllerManager.Instance?.IsUsingDirectionalNavigation == true;
#else
                return NControllerManager.Instance?.IsUsingController == true;
#endif
            }
        }

        public static bool IsUsingController
        {
            get
            {
#if STS2_AT_LEAST_0_110_0
                return NControllerManager.Instance?.InputType == InputType.Controller;
#else
                return NControllerManager.Instance?.IsUsingController == true;
#endif
            }
        }
    }
}
