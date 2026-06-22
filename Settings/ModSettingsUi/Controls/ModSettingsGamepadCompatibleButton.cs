using Godot;
using MegaCrit.Sts2.Core.ControllerInput;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Godot <see cref="Button" /> defaults lean on <c>ui_accept</c>; STS2 maps controller confirm to
    ///     <see cref="MegaInput.select" /> (<c>ui_select</c>) like
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl" />.
    ///     Godot <see cref="Button" /> 默认偏向 <c>ui_accept</c>；STS2 会像
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl" /> 一样，将控制器确认映射到
    ///     <see cref="MegaInput.select" />（<c>ui_select</c>）。
    /// </summary>
    public partial class ModSettingsGamepadCompatibleButton : Button
    {
        /// <summary>
        ///     Creates a button that maps both keyboard and controller confirm actions to press behavior.
        ///     创建一个按钮，将键盘和控制器确认动作都映射为按下行为。
        /// </summary>
        public ModSettingsGamepadCompatibleButton()
        {
            ClipContents = false;
        }

        /// <summary>
        ///     Handles controller confirm input so the button behaves like standard STS2 clickable controls.
        ///     处理控制器确认输入，使按钮表现得像标准 STS2 可点击控件。
        /// </summary>
        /// <param name="event">
        ///     The input event to process.
        ///     要处理的输入事件。
        /// </param>
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
