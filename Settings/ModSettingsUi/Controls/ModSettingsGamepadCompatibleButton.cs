using Godot;
using MegaCrit.Sts2.Core.ControllerInput;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Godot <see cref="Button" /> defaults lean on <c>ui_accept</c>; STS2 maps controller confirm to
    ///     <see cref="MegaInput.select" /> (<c>ui_select</c>) like
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl" />.
    /// </summary>
    public partial class ModSettingsGamepadCompatibleButton : Button
    {
        /// <summary>
        ///     Creates a button that maps both keyboard and controller confirm actions to press behavior.
        /// </summary>
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

        /// <summary>
        ///     Handles controller confirm input so the button behaves like standard STS2 clickable controls.
        /// </summary>
        /// <param name="event">The input event to process.</param>
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
