using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace STS2RitsuLib.Settings
{
    internal static class NativeSettingsApiCompat
    {
        private static readonly Action<NHotkeyManager, string, Action, long> PushPressed = ResolvePushPressed();
        private static readonly Func<NHotkeyManager, string, Action, long> RemovePressed = ResolveRemovePressed();
        private static readonly Func<Control, Vector2> SettingTipPosition = ResolveSettingTipPosition();

        internal static void PushHotkeyPressedBinding(NHotkeyManager manager, string hotkey, Action action,
            long sequence)
        {
            PushPressed(manager, hotkey, action, sequence);
        }

        internal static long RemoveHotkeyPressedBinding(NHotkeyManager manager, string hotkey, Action action)
        {
            return RemovePressed(manager, hotkey, action);
        }

        internal static Vector2 GetSettingTipPosition(Control owner)
        {
            return SettingTipPosition(owner);
        }

        private static Action<NHotkeyManager, string, Action, long> ResolvePushPressed()
        {
            var type = typeof(NHotkeyManager);
            var current = type.GetMethod(nameof(NHotkeyManager.PushHotkeyPressedBinding),
                [typeof(string), typeof(Action), typeof(long)]);
            if (current != null)
                return current.CreateDelegate<Action<NHotkeyManager, string, Action, long>>();

            var legacy = type.GetMethod(nameof(NHotkeyManager.PushHotkeyPressedBinding),
                             [typeof(string), typeof(Action)])
                         ?? throw new MissingMethodException(type.FullName,
                             nameof(NHotkeyManager.PushHotkeyPressedBinding));
            var push = legacy.CreateDelegate<Action<NHotkeyManager, string, Action>>();
            return (manager, hotkey, action, _) => push(manager, hotkey, action);
        }

        private static Func<NHotkeyManager, string, Action, long> ResolveRemovePressed()
        {
            var type = typeof(NHotkeyManager);
            var method = type.GetMethod(nameof(NHotkeyManager.RemoveHotkeyPressedBinding),
                             [typeof(string), typeof(Action)])
                         ?? throw new MissingMethodException(type.FullName,
                             nameof(NHotkeyManager.RemoveHotkeyPressedBinding));
            if (method.ReturnType != typeof(void))
                return method.CreateDelegate<Func<NHotkeyManager, string, Action, long>>();

            var remove = method.CreateDelegate<Action<NHotkeyManager, string, Action>>();
            return (manager, hotkey, action) =>
            {
                remove(manager, hotkey, action);
                return 0L;
            };
        }

        private static Func<Control, Vector2> ResolveSettingTipPosition()
        {
            var type = typeof(NSettingsScreen);
            var method = type.GetMethod("GetSettingTipPosition", [typeof(Control)]);
            if (method != null)
                return method.CreateDelegate<Func<Control, Vector2>>();

            var field = type.GetField("settingTipsOffset", BindingFlags.Public | BindingFlags.Static)
                        ?? throw new MissingFieldException(type.FullName, "settingTipsOffset");
            var offset = (Vector2)field.GetValue(null)!;
            return owner => owner.GlobalPosition + offset;
        }
    }
}
