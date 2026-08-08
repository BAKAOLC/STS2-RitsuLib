using Godot;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Diagnostics.DebugTools;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class RitsuDebugStatePresetEditor
    {
        private void BuildExtensionsPage()
        {
            BuildSecondaryResourcesSection();
            BuildCapabilitiesSection();
        }

        private void BuildSecondaryResourcesSection()
        {
            var definitions = ModSecondaryResourceRegistry.GetDefinitionsSnapshot();
            if (_draft!.SecondaryResources == null)
            {
                if (definitions.Length == 0)
                {
                    _contentBody.AddChild(Hint(L(
                        "ritsulib.debugTools.empty.secondaryResources",
                        "No mods have registered secondary resources.")));
                    return;
                }

                BuildDisabledGroup(
                    L("ritsulib.debugTools.category.secondaryResources", "Secondary resources"),
                    () =>
                    {
                        _draft.SecondaryResources = definitions
                            .Take(RitsuDebugStatePresetStore.MaximumSecondaryResources)
                            .ToDictionary(
                                static definition => definition.Id,
                                static definition => definition.DefaultAmount,
                                StringComparer.Ordinal);
                        MarkDirty(true);
                    });
                return;
            }

            var resources = _draft.SecondaryResources;
            var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            header.AddThemeConstantOverride("separation", 6);
            var title = SectionTitle(L(
                "ritsulib.debugTools.category.secondaryResources",
                "Secondary resources"));
            title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            header.AddChild(title);
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.fillPage", "Fill page"),
                ModSettingsButtonTone.Normal,
                () => CaptureScopes(RitsuDebugStatePresetCaptureScope.SecondaryResources),
                86f));
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.disablePage", "Remove page"),
                ModSettingsButtonTone.Danger,
                () =>
                {
                    _draft.SecondaryResources = null;
                    MarkDirty(true);
                },
                108f));
            _contentBody.AddChild(header);

            if (definitions.Length == 0)
            {
                _contentBody.AddChild(Hint(L(
                    "ritsulib.debugTools.statePresets.secondaryResourcesUnavailable",
                    "The resources saved on this page are not currently registered.")));
                return;
            }

            var grid = new GridContainer
            {
                Columns = 2,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            grid.AddThemeConstantOverride("h_separation", 10);
            grid.AddThemeConstantOverride("v_separation", 8);
            foreach (var definition in definitions.Take(RitsuDebugStatePresetStore.MaximumSecondaryResources))
            {
                var captured = definition;
                grid.AddChild(OptionalIntegerField(
                    SecondaryResourcePresetTitle(definition),
                    resources.TryGetValue(definition.Id, out var savedAmount) ? savedAmount : null,
                    definition.MinAmount,
                    definition.HardMaxAmount,
                    value =>
                    {
                        if (value.HasValue)
                            resources[captured.Id] = value.Value;
                        else
                            resources.Remove(captured.Id);
                        MarkDirty();
                    }));
            }

            _contentBody.AddChild(grid);
        }

        private static string SecondaryResourcePresetTitle(SecondaryResourceDefinition definition)
        {
            try
            {
                return SecondaryResourceText.GetTitleText(definition);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                return definition.Id;
            }
        }
    }
}
