using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    // ReSharper disable once Godot.MissingParameterlessConstructor
    internal sealed partial class RitsuDebugMonsterIntentPicker : VBoxContainer
    {
        private readonly RitsuDebugMonsterIntentCanvas _summary;

        internal RitsuDebugMonsterIntentPicker(Creature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddThemeConstantOverride("separation", 8);

            var title = new Label
            {
                Text = L("ritsulib.debugTools.field.currentIntent", "Current intent"),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            title.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            title.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
            title.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            AddChild(title);

            _summary = new(false);
            AddChild(_summary);
            AddChild(new ModSettingsTextButton(
                L("ritsulib.debugTools.action.openMonsterIntentWindow", "Open intent map"),
                ModSettingsButtonTone.Accent,
                () => OpenRequested?.Invoke())
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });
            Refresh(creature);
        }

        internal event Action? OpenRequested;

        internal void Refresh(Creature creature)
        {
            _summary.Refresh(creature);
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }
    }
}
