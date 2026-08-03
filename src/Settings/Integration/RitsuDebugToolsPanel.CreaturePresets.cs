using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Diagnostics.DebugTools;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class RitsuDebugToolsPanel
    {
        private void AddCreaturePresetManager(RitsuDebugLiveDetailContainer root, Creature creature)
        {
            if (creature.IsPlayer || creature.Monster == null || !creature.CombatId.HasValue)
                return;

            AddSectionTitle(root, L("ritsulib.debugTools.creaturePresets.title", "Creature presets"));
            AddHint(root, L(
                "ritsulib.debugTools.creaturePresets.description",
                "Save this enemy's health, block, and Powers. Apply a preset to an enemy or add its saved monster to combat."));

            var newName = TextField(
                root,
                L("ritsulib.debugTools.creaturePresets.name", "New preset name"),
                CreateCreaturePresetName(creature),
                L("ritsulib.debugTools.creaturePresets.nameHint", "Enter a preset name"));
            root.AddChild(ActionButton(
                L("ritsulib.debugTools.creaturePresets.saveNew", "Save current as new"),
                ModSettingsButtonTone.Accent,
                () => SaveNewCreaturePreset(creature, newName.Text)));

            var presets = RitsuDebugCreaturePresetStore.GetSnapshot();
            if (presets.Count == 0)
            {
                AddHint(root, L("ritsulib.debugTools.creaturePresets.empty", "No saved creature presets."));
                return;
            }

            var selected = presets.FirstOrDefault(preset => preset.Id == _selectedCreaturePresetId) ??
                           presets.FirstOrDefault(preset => preset.MonsterId == creature.Monster.Id.ToString()) ??
                           presets[0];
            _selectedCreaturePresetId = selected.Id;
            var summary = CreatePowerListHint(CreaturePresetSummary(selected));
            root.AddChild(DropdownField(
                L("ritsulib.debugTools.creaturePresets.saved", "Saved preset"),
                [.. presets.Select(static preset => (preset.Id, preset.Name))],
                selected.Id,
                value =>
                {
                    _selectedCreaturePresetId = value;
                    selected = presets.First(preset => preset.Id == value);
                    summary.Text = CreaturePresetSummary(selected);
                }));
            root.AddChild(summary);

            var primaryActions = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            primaryActions.AddThemeConstantOverride("separation", 8);
            primaryActions.AddChild(ActionButton(
                L("ritsulib.debugTools.creaturePresets.apply", "Apply to this enemy"),
                ModSettingsButtonTone.Accent,
                () => SubmitCreaturePreset(selected, creature.CombatId.Value)));
            primaryActions.AddChild(ActionButton(
                L("ritsulib.debugTools.creaturePresets.add", "Add saved monster"),
                ModSettingsButtonTone.Normal,
                () => SubmitCreaturePresetMonster(selected)));
            root.AddChild(primaryActions);

            var managementActions = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            managementActions.AddThemeConstantOverride("separation", 8);
            managementActions.AddChild(ActionButton(
                L("ritsulib.debugTools.creaturePresets.update", "Update from current"),
                ModSettingsButtonTone.Normal,
                () => UpdateCreaturePreset(selected, creature)));
            managementActions.AddChild(ActionButton(
                L("ritsulib.debugTools.creaturePresets.delete", "Delete preset"),
                ModSettingsButtonTone.Danger,
                () => DeleteCreaturePreset(selected)));
            root.AddChild(managementActions);
        }

        private void SaveNewCreaturePreset(Creature creature, string requestedName)
        {
            var name = requestedName.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                SetStatus(L("ritsulib.debugTools.creaturePresets.nameRequired",
                    "Enter a name for the creature preset."), true);
                return;
            }

            if (!RitsuDebugCreaturePresetStore.TryCapture(creature, name, out var preset, out var feedback) ||
                !RitsuDebugCreaturePresetStore.TrySave(preset, out feedback))
            {
                ShowActionWarning(feedback.GetLocalizedText());
                return;
            }

            _selectedCreaturePresetId = preset.Id;
            SetStatus(L("ritsulib.debugTools.creaturePresets.savedStatus", "Creature preset saved."), false);
            ScheduleRefresh();
        }

        private void UpdateCreaturePreset(RitsuDebugCreaturePreset current, Creature creature)
        {
            if (!RitsuDebugCreaturePresetStore.TryCapture(
                    creature,
                    current.Name,
                    out var captured,
                    out var feedback))
            {
                ShowActionWarning(feedback.GetLocalizedText());
                return;
            }

            captured.Id = current.Id;
            if (!RitsuDebugCreaturePresetStore.TrySave(captured, out feedback))
            {
                ShowActionWarning(feedback.GetLocalizedText());
                return;
            }

            _selectedCreaturePresetId = captured.Id;
            SetStatus(L("ritsulib.debugTools.creaturePresets.updatedStatus", "Creature preset updated."), false);
            ScheduleRefresh();
        }

        private void DeleteCreaturePreset(RitsuDebugCreaturePreset preset)
        {
            ModSettingsUiFactory.ShowStyledConfirm(
                this,
                L("ritsulib.debugTools.creaturePresets.deleteTitle", "Delete creature preset?"),
                string.Format(
                    L("ritsulib.debugTools.creaturePresets.deleteBody", "Delete '{0}' permanently?"),
                    preset.Name),
                L("ritsulib.debugTools.statePresets.cancel", "Cancel"),
                L("ritsulib.debugTools.creaturePresets.delete", "Delete preset"),
                true,
                () =>
                {
                    if (!RitsuDebugCreaturePresetStore.TryDelete(preset.Id))
                        return;
                    _selectedCreaturePresetId = null;
                    SetStatus(L("ritsulib.debugTools.creaturePresets.deletedStatus",
                        "Creature preset deleted."), false);
                    ScheduleRefresh();
                });
        }

        private void SubmitCreaturePreset(RitsuDebugCreaturePreset preset, uint combatId)
        {
            if (!TryGetActionContext(out var requester, out var target))
                return;
            RunAction(() => RitsuDebugCreaturePresetActions.SubmitApplyPreset(
                requester,
                target,
                combatId,
                preset));
        }

        private void SubmitCreaturePresetMonster(RitsuDebugCreaturePreset preset)
        {
            if (!TryGetActionContext(out var requester, out var target))
                return;
            RunAction(() => RitsuDebugCreaturePresetActions.SubmitAddPresetMonster(requester, target, preset));
        }

        private static string CreateCreaturePresetName(Creature creature)
        {
            var baseName = creature.Name?.Trim();
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = creature.Monster?.Id.ToString() ?? "Creature";
            return string.Format(
                L("ritsulib.debugTools.creaturePresets.defaultName", "{0} preset"),
                baseName);
        }

        private static string CreaturePresetSummary(RitsuDebugCreaturePreset preset)
        {
            return string.Format(
                L("ritsulib.debugTools.creaturePresets.summary",
                    "{0} · HP {1}/{2} · Block {3} · {4} Powers"),
                preset.MonsterId,
                preset.CurrentHp,
                preset.MaxHp,
                preset.Block,
                preset.Powers.Count);
        }
    }
}
