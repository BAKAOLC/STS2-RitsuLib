using Godot;

namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsFocusExtensions
    {
        internal static void ReleaseFocusIfInsideTree(this Control? control)
        {
            if (control?.IsInsideTree() == true)
                control.ReleaseFocus();
        }
    }
}
