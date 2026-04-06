using Godot;

namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsInteractionUi
    {
        internal static bool IsBindingReadOnly(IModSettingsBinding? binding)
        {
            return binding is IModSettingsSessionEditGate { IsReadOnlyInCurrentSession: true };
        }

        internal static void ApplySessionReadOnlyGates(Control root, IModSettingsBinding? scopeBinding)
        {
            if (!IsBindingReadOnly(scopeBinding))
                return;

            ApplyReadOnlyRecursive(root, true);
        }

        private static void ApplyReadOnlyRecursive(Control node, bool readOnly)
        {
            switch (node)
            {
                case OptionButton ob:
                    ob.Disabled = readOnly;
                    break;
                case MenuButton mb:
                    mb.Disabled = readOnly;
                    break;
                case ColorPickerButton cp:
                    cp.Disabled = readOnly;
                    break;
                case Button b:
                    b.Disabled = readOnly;
                    break;
                case LineEdit le:
                    le.Editable = !readOnly;
                    break;
                case TextEdit te:
                    te.Editable = !readOnly;
                    break;
                case Slider s:
                    s.Editable = !readOnly;
                    break;
                case SpinBox sb:
                    sb.Editable = !readOnly;
                    break;
            }

            foreach (var child in node.GetChildren())
                if (child is Control c)
                    ApplyReadOnlyRecursive(c, readOnly);
        }
    }
}
