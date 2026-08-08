using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Ui.Overlay;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class RitsuDebugToolsPanel
    {
        private const int MaximumCapabilityStatePreviewLength = 180;

        private void AddCapabilitySection(
            RitsuDebugLiveDetailContainer detail,
            RitsuDebugCapabilityTarget target,
            AbstractModel model,
            string? title = null)
        {
            if (!CanManageCapabilities(model))
                return;

            var section = CreateCapabilitySection(target, model, title, out var content);
            detail.AddChild(section);
            var binding = new CapabilityEditorBinding(content, target, model);
            detail.RegisterRefresh(() => RefreshCapabilityEditor(binding));
        }

        private void AddCardCapabilitySections(
            RitsuDebugLiveDetailContainer detail,
            RitsuDebugCapabilityTarget cardTarget,
            CardModel card)
        {
            var host = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            host.AddThemeConstantOverride("separation", 8);
            detail.AddChild(host);
            var bindings = new List<CapabilityEditorBinding>();
            var enchantment = card.Enchantment;
            var affliction = card.Affliction;
            RebuildSections();
            detail.RegisterRefresh(RefreshSections);
            return;

            void RefreshSections()
            {
                if (!ReferenceEquals(enchantment, card.Enchantment) ||
                    !ReferenceEquals(affliction, card.Affliction))
                {
                    enchantment = card.Enchantment;
                    affliction = card.Affliction;
                    RebuildSections();
                    return;
                }

                foreach (var binding in bindings)
                    RefreshCapabilityEditor(binding);
            }

            void RebuildSections()
            {
                foreach (var child in host.GetChildren())
                {
                    host.RemoveChild(child);
                    child.QueueFree();
                }

                bindings.Clear();
                AddSection(
                    cardTarget,
                    card,
                    L("ritsulib.debugTools.capabilities.section", "Capabilities"));
                if (enchantment != null)
                    AddSection(
                        new(
                            RitsuDebugCapabilityTargetKind.Enchantment,
                            enchantment.Id.ToString(),
                            cardTarget.Index,
                            cardTarget.Pile,
                            ContainerModelId: card.Id.ToString()),
                        enchantment,
                        L("ritsulib.debugTools.capabilities.enchantmentSection",
                            "Enchantment capabilities"));
                if (affliction != null)
                    AddSection(
                        new(
                            RitsuDebugCapabilityTargetKind.Affliction,
                            affliction.Id.ToString(),
                            cardTarget.Index,
                            cardTarget.Pile,
                            ContainerModelId: card.Id.ToString()),
                        affliction,
                        L("ritsulib.debugTools.capabilities.afflictionSection",
                            "Affliction capabilities"));
                host.Visible = bindings.Count > 0;
            }

            void AddSection(RitsuDebugCapabilityTarget target, AbstractModel model, string title)
            {
                if (!CanManageCapabilities(model))
                    return;
                var section = CreateCapabilitySection(target, model, title, out var content);
                host.AddChild(section);
                bindings.Add(new(content, target, model));
            }
        }

        private void RefreshCapabilityEditor(CapabilityEditorBinding binding)
        {
            if (!IsInstanceValid(binding.Content))
                return;
            var stateHash = GetCapabilityStateHash(binding.Model);
            if (binding.StateHash == stateHash)
                return;
            binding.StateHash = stateHash;
            RebuildCapabilityEditor(binding.Content, binding.Target, binding.Model);
        }

        private Control CreateCapabilitySection(
            RitsuDebugCapabilityTarget target,
            AbstractModel model,
            string? title,
            out VBoxContainer content)
        {
            content = CreateAdjustmentContent();
            RebuildCapabilityEditor(content, target, model);
            return AdjustmentSection(
                title ?? L("ritsulib.debugTools.capabilities.section", "Capabilities"),
                content,
                RitsuDebugToolsGlyph.Puzzle);
        }

        private static bool CanManageCapabilities(AbstractModel model)
        {
            return SafeGetCapabilities(model).Count > 0 ||
                   ModelCapabilityRegistry.GetRegistrationsSnapshot()
                       .Any(registration => ModelCapabilityRegistry.IsCompatibleWith(
                           registration.CapabilityType,
                           model));
        }

        private void RebuildCapabilityEditor(
            VBoxContainer content,
            RitsuDebugCapabilityTarget target,
            AbstractModel model)
        {
            foreach (var child in content.GetChildren())
            {
                content.RemoveChild(child);
                child.QueueFree();
            }

            var capabilities = SafeGetCapabilities(model);
            if (capabilities.Count == 0)
                AddHint(
                    content,
                    L("ritsulib.debugTools.capabilities.noneAttached",
                        "This model has no attached capabilities."));
            else
                foreach (var row in CreateCapabilityDisplayRows(capabilities))
                    content.AddChild(CreateCapabilityRow(
                        target,
                        row.Capability,
                        row.Index,
                        row.Count,
                        row.StateText));

            var compatible = ModelCapabilityRegistry.GetRegistrationsSnapshot()
                .Where(registration => ModelCapabilityRegistry.IsCompatibleWith(registration.CapabilityType, model))
                .ToArray();
            if (compatible.Length == 0)
                return;

            var selected = compatible[0];
            var dropdown = new ModSettingsDropdownChoiceControl<ModelCapabilityRegistration>(
                [
                    .. compatible.Select(static registration =>
                        (registration, $"{registration.CapabilityType.Name} · {registration.Id}")),
                ],
                selected,
                value => selected = value)
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new(0f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
            };
            content.AddChild(ActionField(
                L("ritsulib.debugTools.capabilities.add", "Add capability"),
                dropdown,
                ActionButton(
                    L("ritsulib.debugTools.action.add", "Add"),
                    ModSettingsButtonTone.Accent,
                    () => SubmitCapabilityAction(target, RitsuDebugCapabilityOperation.Add, selected.Id))));
        }

        private Control CreateCapabilityRow(
            RitsuDebugCapabilityTarget target,
            IModelCapability capability,
            int index,
            int count,
            string? stateText)
        {
            var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            panel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListItemCardStyle());
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 8);
            margin.AddThemeConstantOverride("margin_top", 5);
            margin.AddThemeConstantOverride("margin_right", 6);
            margin.AddThemeConstantOverride("margin_bottom", 5);
            panel.AddChild(margin);
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("separation", 6);
            margin.AddChild(row);
            var identity = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            identity.AddThemeConstantOverride("separation", 1);
            row.AddChild(identity);

            var title = new Label
            {
                Text = count > 1
                    ? $"{CapabilityDisplayName(capability)} ×{count}"
                    : CapabilityDisplayName(capability),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            title.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            title.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            identity.AddChild(title);

            var identifier = new Label
            {
                Text = capability.CapabilityId,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            identifier.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            identifier.AddThemeFontSizeOverride(
                "font_size",
                RitsuShellTheme.Current.Metric.FontSize.Secondary);
            identifier.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            identity.AddChild(identifier);

            if (stateText != null)
            {
                var state = new Label
                {
                    Text = stateText,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    ClipText = true,
                    TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                    TooltipText = stateText,
                };
                state.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
                state.AddThemeFontSizeOverride(
                    "font_size",
                    RitsuShellTheme.Current.Metric.FontSize.Secondary);
                state.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.Hint);
                identity.AddChild(state);
            }

            var remove = new RitsuDebugToolsIconButton(38f, 36f);
            remove.Configure(
                RitsuDebugToolsIcons.Get(
                    RitsuDebugToolsGlyph.Trash,
                    17,
                    RitsuShellTheme.Current.Component.TextButton.Danger.Fg),
                L("ritsulib.debugTools.capabilities.remove", "Remove capability"),
                ModSettingsButtonTone.Danger);
            remove.Pressed += () => SubmitCapabilityAction(
                target,
                RitsuDebugCapabilityOperation.Remove,
                capability.CapabilityId,
                index);
            row.AddChild(remove);
            panel.TooltipText = $"{capability.GetType().FullName}\n{capability.CapabilityId}" +
                                (count > 1 ? $"\n×{count}" : string.Empty) +
                                (stateText == null ? string.Empty : $"\n{stateText}");
            return panel;
        }

        private static IReadOnlyList<CapabilityDisplayRow> CreateCapabilityDisplayRows(
            IReadOnlyList<IModelCapability> capabilities)
        {
            var rows = new List<CapabilityDisplayRow>();
            for (var index = 0; index < capabilities.Count; index++)
            {
                var capability = capabilities[index];
                var stateText = SafeCapabilityState(capability);
                if (stateText == null)
                {
                    var existing = rows.FirstOrDefault(row =>
                        row.StateText == null &&
                        row.Capability.GetType() == capability.GetType() &&
                        string.Equals(
                            row.Capability.CapabilityId,
                            capability.CapabilityId,
                            StringComparison.Ordinal));
                    if (existing != null)
                    {
                        existing.Count++;
                        continue;
                    }
                }

                rows.Add(new(capability, index, stateText));
            }

            return rows;
        }

        private static string CapabilityDisplayName(IModelCapability capability)
        {
            const string suffix = "Capability";
            var name = capability.GetType().Name;
            if (name.EndsWith(suffix, StringComparison.Ordinal) && name.Length > suffix.Length)
                name = name[..^suffix.Length];
            var display = new StringBuilder(name.Length + 8);
            for (var index = 0; index < name.Length; index++)
            {
                var current = name[index];
                if (index > 0 &&
                    char.IsUpper(current) &&
                    (!char.IsUpper(name[index - 1]) ||
                     index + 1 < name.Length && char.IsLower(name[index + 1])))
                    display.Append(' ');
                display.Append(current);
            }

            return display.ToString();
        }

        private void SubmitCapabilityAction(
            RitsuDebugCapabilityTarget target,
            RitsuDebugCapabilityOperation operation,
            string? capabilityId = null,
            int capabilityIndex = -1)
        {
            if (!TryGetActionContext(out var requester, out var targetPlayer))
                return;
            RunAction(() => RitsuDebugCapabilityActions.Submit(
                requester,
                targetPlayer,
                target,
                operation,
                capabilityId,
                capabilityIndex));
        }

        private static IReadOnlyList<IModelCapability> SafeGetCapabilities(AbstractModel model)
        {
            try
            {
                if (ModelCapabilities.TryGet(model, out var capabilities))
                    return capabilities.All;
                return ModelCapabilityDefaults.HasDefaultCapabilitySource(model)
                    ? ModelCapabilities.Get(model).All
                    : [];
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugToolsUi] Could not inspect capabilities for '{model.Id}': {ex.Message}");
                return [];
            }
        }

        private static string? SafeCapabilityState(IModelCapability capability)
        {
            if (capability is not IModelCapabilityJsonState state)
                return null;
            try
            {
                var node = state.SaveState();
                if (node == null)
                    return null;
                var json = node.ToJsonString(new() { WriteIndented = false });
                if (json.Length > MaximumCapabilityStatePreviewLength)
                    json = $"{json[..MaximumCapabilityStatePreviewLength]}…";
                return string.Format(
                    L("ritsulib.debugTools.capabilities.stateInline", "Schema {0} · {1}"),
                    state.SchemaVersion,
                    json);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugToolsUi] Could not serialize capability '{capability.CapabilityId}': {ex.Message}");
                return L("ritsulib.debugTools.capabilities.stateUnavailable", "State preview unavailable");
            }
        }

        private static int GetCapabilityStateHash(AbstractModel? model)
        {
            if (model == null)
                return 0;
            var hash = new HashCode();
            foreach (var capability in SafeGetCapabilities(model))
            {
                hash.Add(capability.CapabilityId, StringComparer.Ordinal);
                hash.Add(capability.GetType());
                if (capability is not IModelCapabilityJsonState state)
                    continue;
                try
                {
                    hash.Add(state.SchemaVersion);
                    hash.Add(state.SaveState()?.ToJsonString(), StringComparer.Ordinal);
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    hash.Add(ex.GetType());
                }
            }

            return hash.ToHashCode();
        }

        private sealed class CapabilityEditorBinding(
            VBoxContainer content,
            RitsuDebugCapabilityTarget target,
            AbstractModel model)
        {
            internal VBoxContainer Content { get; } = content;

            internal AbstractModel Model { get; } = model;

            internal int StateHash { get; set; } = GetCapabilityStateHash(model);

            internal RitsuDebugCapabilityTarget Target { get; } = target;
        }

        private sealed class CapabilityDisplayRow(
            IModelCapability capability,
            int index,
            string? stateText)
        {
            internal IModelCapability Capability { get; } = capability;

            internal int Count { get; set; } = 1;

            internal int Index { get; } = index;

            internal string? StateText { get; } = stateText;
        }
    }
}
