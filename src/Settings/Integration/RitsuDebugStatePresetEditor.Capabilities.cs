using Godot;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class RitsuDebugStatePresetEditor
    {
        private void BuildCapabilitiesSection()
        {
            if (_draft!.CapabilityTargets == null)
            {
                BuildDisabledGroup(
                    L("ritsulib.debugTools.category.capabilities", "Capabilities"),
                    () => CaptureScopes(RitsuDebugStatePresetCaptureScope.Capabilities));
                return;
            }

            var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            header.AddThemeConstantOverride("separation", 6);
            var title = SectionTitle(L("ritsulib.debugTools.category.capabilities", "Capabilities"));
            title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            header.AddChild(title);
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.fillPage", "Fill page"),
                ModSettingsButtonTone.Normal,
                () => CaptureScopes(RitsuDebugStatePresetCaptureScope.Capabilities),
                86f));
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.disablePage", "Remove page"),
                ModSettingsButtonTone.Danger,
                () =>
                {
                    _draft.CapabilityTargets = null;
                    MarkDirty(true);
                },
                108f));
            _contentBody.AddChild(header);

            var targets = _draft.CapabilityTargets;
            if (targets.Count == 0)
            {
                _contentBody.AddChild(Hint(L(
                    "ritsulib.debugTools.statePresets.capabilitiesEmpty",
                    "No persisted capability state was found on supported models.")));
                return;
            }

            _contentBody.AddChild(Hint(L(
                "ritsulib.debugTools.statePresets.capabilitiesHint",
                "Capability state is restored after the preset creates or replaces its model instances.")));
            foreach (var target in targets.ToArray())
                _contentBody.AddChild(CreateCapabilityTargetRow(target, targets));
        }

        private Control CreateCapabilityTargetRow(
            RitsuDebugStatePresetCapabilityTarget target,
            ICollection<RitsuDebugStatePresetCapabilityTarget> targets)
        {
            var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            panel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListItemCardStyle());
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 10);
            margin.AddThemeConstantOverride("margin_top", 8);
            margin.AddThemeConstantOverride("margin_right", 10);
            margin.AddThemeConstantOverride("margin_bottom", 8);
            panel.AddChild(margin);
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("separation", 8);
            margin.AddChild(row);
            var identity = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            identity.AddThemeConstantOverride("separation", 2);
            row.AddChild(identity);

            var title = new Label
            {
                Text = $"{CapabilityTargetLabel(target.Target.Kind)} · {target.Target.ModelId}",
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            title.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            title.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            identity.AddChild(title);

            var ids = target.Capabilities.Capabilities
                .Select(static capability => capability.Id)
                .Where(static id => !string.IsNullOrWhiteSpace(id))
                .ToArray();
            var summary = new Label
            {
                Text = ids.Length == 0
                    ? L("ritsulib.debugTools.capabilities.noneAttached", "This model has no attached capabilities.")
                    : string.Join(" · ", ids),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            summary.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            summary.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
            summary.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            identity.AddChild(summary);

            row.AddChild(CompactButton(
                L("ritsulib.debugTools.action.remove", "Remove"),
                ModSettingsButtonTone.Danger,
                () =>
                {
                    targets.Remove(target);
                    MarkDirty(true);
                },
                88f));
            return panel;
        }

        private static string CapabilityTargetLabel(RitsuDebugCapabilityTargetKind kind)
        {
            return kind switch
            {
                RitsuDebugCapabilityTargetKind.Character =>
                    L("ritsulib.debugTools.capabilities.target.character", "Character"),
                RitsuDebugCapabilityTargetKind.Card =>
                    L("ritsulib.debugTools.capabilities.target.card", "Card"),
                RitsuDebugCapabilityTargetKind.Relic =>
                    L("ritsulib.debugTools.capabilities.target.relic", "Relic"),
                RitsuDebugCapabilityTargetKind.Potion =>
                    L("ritsulib.debugTools.capabilities.target.potion", "Potion"),
                RitsuDebugCapabilityTargetKind.Power =>
                    L("ritsulib.debugTools.capabilities.target.power", "Power"),
                RitsuDebugCapabilityTargetKind.Orb =>
                    L("ritsulib.debugTools.capabilities.target.orb", "Orb"),
                RitsuDebugCapabilityTargetKind.Enchantment =>
                    L("ritsulib.debugTools.capabilities.target.enchantment", "Enchantment"),
                RitsuDebugCapabilityTargetKind.Affliction =>
                    L("ritsulib.debugTools.capabilities.target.affliction", "Affliction"),
                RitsuDebugCapabilityTargetKind.Monster =>
                    L("ritsulib.debugTools.capabilities.target.monster", "Monster"),
                _ => kind.ToString(),
            };
        }
    }
}
