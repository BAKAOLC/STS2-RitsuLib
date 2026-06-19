using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsNativeHoverTip
    {
        private static readonly StringName AttachedMetaKey = new("_ritsu_native_hover_tip");

        private static readonly PropertyInfo? HoverTipTitleProperty = typeof(HoverTip).GetProperty(
            nameof(HoverTip.Title),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static void Attach(Control owner, Func<string> titleProvider, Func<string> descriptionProvider)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(titleProvider);
            ArgumentNullException.ThrowIfNull(descriptionProvider);

            if (owner.HasMeta(AttachedMetaKey))
                return;

            owner.SetMeta(AttachedMetaKey, true);
            if (owner.MouseFilter == Control.MouseFilterEnum.Ignore)
                owner.MouseFilter = Control.MouseFilterEnum.Pass;

            owner.MouseEntered += Show;
            owner.FocusEntered += Show;
            owner.MouseExited += Hide;
            owner.FocusExited += Hide;
            owner.TreeExiting += Hide;
            return;

            void Show()
            {
                var description = Resolve(descriptionProvider);
                if (string.IsNullOrWhiteSpace(description))
                    return;

                var title = Resolve(titleProvider);
                var hoverTip = new HoverTip(new("settings_ui", "FASTMODE"), description);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    object boxedHoverTip = hoverTip;
                    HoverTipTitleProperty?.SetValue(boxedHoverTip, title);
                    hoverTip = (HoverTip)boxedHoverTip;
                }

                hoverTip.Id = $"ritsu-settings:{owner.GetInstanceId()}";

                NHoverTipSet.Remove(owner);
                NHoverTipSet.CreateAndShow(owner, hoverTip)
                    ?.SetGlobalPosition(owner.GlobalPosition + NSettingsScreen.settingTipsOffset);
            }

            void Hide()
            {
                NHoverTipSet.Remove(owner);
            }
        }

        private static string Resolve(Func<string> provider)
        {
            try
            {
                return provider().Trim();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
