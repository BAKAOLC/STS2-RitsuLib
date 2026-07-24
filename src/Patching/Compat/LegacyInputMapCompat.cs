#if STS2_AT_LEAST_0_107_0 && !STS2_AT_LEAST_0_108_0
using Godot;

namespace STS2RitsuLib.Patching.Compat
{
    internal static class LegacyInputMapCompat
    {
        private const float DefaultDeadzone = 0.5f;

        private static readonly (StringName ModernAction, StringName? LegacyAction)[] ActionAliases =
        [
            ("controller_d_pad_up", "controller_d_pad_north"),
            ("controller_d_pad_down", "controller_d_pad_south"),
            ("controller_d_pad_right", "controller_d_pad_east"),
            ("controller_d_pad_left", "controller_d_pad_west"),
            ("controller_l_stick_press", "controller_joystick_press"),
            ("raw_l_stick_left", "controller_joystick_left"),
            ("raw_l_stick_right", "controller_joystick_right"),
            ("raw_l_stick_up", "controller_joystick_up"),
            ("raw_l_stick_down", "controller_joystick_down"),
            ("controller_r_stick_left", null),
            ("controller_r_stick_right", null),
            ("controller_r_stick_up", null),
            ("controller_r_stick_down", null),
            ("ui_alt_up", null),
            ("ui_alt_down", null),
            ("ui_alt_left", null),
            ("ui_alt_right", null),
            ("raw_r_stick_left", null),
            ("raw_r_stick_right", null),
            ("raw_r_stick_up", null),
            ("raw_r_stick_down", null),
            ("raw_right_trigger", "controller_right_trigger"),
            ("raw_left_trigger", "controller_left_trigger"),
            ("controller_l_stick_up", null),
            ("controller_l_stick_down", null),
            ("controller_l_stick_left", null),
            ("controller_l_stick_right", null),
        ];

        private static bool _initialized;

        public static void EnsureRegistered()
        {
            if (_initialized)
                return;

            _initialized = true;
            var registered = 0;

            foreach (var (modernAction, legacyAction) in ActionAliases)
            {
                if (InputMap.HasAction(modernAction))
                    continue;

                var hasLegacyAction = legacyAction != null && InputMap.HasAction(legacyAction);
                var deadzone = hasLegacyAction
                    ? InputMap.ActionGetDeadzone(legacyAction!)
                    : DefaultDeadzone;
                InputMap.AddAction(modernAction, deadzone);

                if (hasLegacyAction)
                    foreach (var inputEvent in InputMap.ActionGetEvents(legacyAction!))
                        InputMap.ActionAddEvent(modernAction, inputEvent);

                registered++;
            }

            if (registered > 0)
                RitsuLibFramework.Logger.Info(
                    $"[LegacyInputMapCompat] Registered {registered} post-0.107 controller actions to prevent missing InputMap action errors.");
        }
    }
}
#endif
