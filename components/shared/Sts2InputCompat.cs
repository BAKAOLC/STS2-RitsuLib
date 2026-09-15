using Godot;

namespace STS2RitsuLib.Compat
{
    internal static class Sts2InputCompat
    {
        internal static StringName ConfirmAction => RitsuLibFramework.Host.ConfirmAction;
        internal static StringName CancelCardPlayAction => RitsuLibFramework.Host.CancelCardPlayAction;
        internal static bool IsUsingDirectionalNavigation => RitsuLibFramework.Host.IsUsingDirectionalNavigation;
        internal static bool IsUsingController => RitsuLibFramework.Host.IsUsingController;
    }
}
