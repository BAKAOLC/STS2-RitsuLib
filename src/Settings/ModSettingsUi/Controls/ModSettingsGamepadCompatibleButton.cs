using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">A Godot button that accepts Slay the Spire 2's select and version-appropriate confirm actions.</para>
    ///     <para xml:lang="zh-CN">同时接受《杀戮尖塔 2》的选择操作和当前游戏版本确认操作的 Godot 按钮。</para>
    /// </summary>
    public partial class ModSettingsGamepadCompatibleButton : Button
    {
        /// <summary>
        ///     <para xml:lang="en">Initializes a button without clipping content outside its bounds.</para>
        ///     <para xml:lang="zh-CN">初始化一个不会裁剪边界外内容的按钮。</para>
        /// </summary>
        public ModSettingsGamepadCompatibleButton()
        {
            ClipContents = false;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Emits <see cref="BaseButton.SignalName.Pressed" /> for non-repeated select or confirm input
        ///         while the button is enabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">按钮启用时，对非重复的选择或确认输入发出 <see cref="BaseButton.SignalName.Pressed" /> 信号。</para>
        /// </summary>
        /// <param name="event">
        ///     <para xml:lang="en">The GUI input event to process.</para>
        ///     <para xml:lang="zh-CN">要处理的 GUI 输入事件。</para>
        /// </param>
        public override void _GuiInput(InputEvent @event)
        {
            if (!Disabled && !@event.IsEcho() &&
                (@event.IsActionPressed(MegaInput.select) ||
                 @event.IsActionPressed(Sts2InputCompat.ConfirmAction)))
            {
                EmitSignal(BaseButton.SignalName.Pressed);
                AcceptEvent();
                return;
            }

            base._GuiInput(@event);
        }
    }
}
