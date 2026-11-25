using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace STS2RitsuLib.Settings
{
    internal static class NativeSettingsApiCompat
    {
        private static readonly Func<Control, Vector2> SettingTipPosition = ResolveSettingTipPosition();

        internal static Vector2 GetSettingTipPosition(Control owner)
        {
            return SettingTipPosition(owner);
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
