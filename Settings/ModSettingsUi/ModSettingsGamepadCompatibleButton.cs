using Godot;
using MegaCrit.Sts2.Core.ControllerInput;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Godot <see cref="Button" /> defaults lean on <c>ui_accept</c>; STS2 maps controller confirm to
    ///     <see cref="MegaInput.select" /> (<c>ui_select</c>) like
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl" />.
    /// </summary>
    internal partial class ModSettingsGamepadCompatibleButton : Button
    {
        public ModSettingsGamepadCompatibleButton()
        {
            ClipContents = false;
            TreeEntered += AttachFocusReticleOnce;
        }

        private void AttachFocusReticleOnce()
        {
            TreeEntered -= AttachFocusReticleOnce;
            ModSettingsFocusChrome.AttachControllerSelectionReticle(this);
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (!Disabled && !@event.IsEcho() &&
                (@event.IsActionPressed(MegaInput.select) || @event.IsActionPressed(MegaInput.accept)))
            {
                EmitSignal(BaseButton.SignalName.Pressed);
                AcceptEvent();
                return;
            }

            base._GuiInput(@event);
        }
    }
}
