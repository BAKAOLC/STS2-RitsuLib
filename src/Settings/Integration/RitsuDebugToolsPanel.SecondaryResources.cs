using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Content;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Ui.Catalog;
using STS2RitsuLib.Ui.Overlay;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class RitsuDebugToolsPanel
    {
        private Control CreateSecondaryResourceCatalog()
        {
            if (!TryGetTargetPlayer(out var target) || !HasActiveCombatState(target))
                return EmptyBrowser(L(
                    "ritsulib.debugTools.empty.secondaryResourcesCombat",
                    "Start a combat to inspect and change secondary resources."));

            var definitions = ModSecondaryResourceRegistry.GetDefinitionsSnapshot();
            if (definitions.Length == 0)
                return EmptyBrowser(L(
                    "ritsulib.debugTools.empty.secondaryResources",
                    "No mods have registered secondary resources."));

            var byId = definitions.ToDictionary(static definition => definition.Id, StringComparer.Ordinal);
            var sourceByItemId = definitions.ToDictionary(
                static definition => definition.Id,
                static definition => new ContentSourceDescriptor(definition.ModId, definition.ModId),
                StringComparer.Ordinal);
            var browser = Browser(
                L(
                    "ritsulib.debugTools.search.secondaryResources",
                    "Search secondary resources by name, owner, or ID"),
                item => CreateSecondaryResourceDetail(byId[item.Id]),
                [
                    CreateContentSourceFilter(
                        sourceByItemId.Values,
                        item => sourceByItemId.TryGetValue(item.Id, out var source) ? source : null),
                ],
                presentation: RitsuCatalogPresentation.List,
                detailWidth: 520f);
            browser.SetItems(CreateSecondaryResourceItems(target, definitions));
            return browser;
        }

        private RitsuCatalogItem[] CreateSecondaryResourceItems(
            Player target,
            IEnumerable<SecondaryResourceDefinition> definitions)
        {
            return
            [
                .. definitions
                    .OrderBy(SecondaryResourceTitle, StringComparer.CurrentCultureIgnoreCase)
                    .Select(definition => CreateSecondaryResourceItem(target, definition)),
            ];
        }

        private RitsuCatalogItem CreateSecondaryResourceItem(
            Player target,
            SecondaryResourceDefinition definition)
        {
            var amount = SecondaryResourceCmd.Get(target, definition.Id);
            var maximum = SecondaryResourceCmd.GetMax(target, definition.Id);
            return new(
                definition.Id,
                SecondaryResourceTitle(definition),
                string.Format(
                    L("ritsulib.debugTools.secondaryResource.owner", "Owner: {0}"),
                    definition.ModId),
                $"{definition.Id} {definition.ModId} {definition.LocalId}",
                badge: SecondaryResourceAmountText(amount, maximum),
                iconFactory: () => SecondaryResourceIcon(definition));
        }

        private Control CreateSecondaryResourceDetail(SecondaryResourceDefinition definition)
        {
            if (!TryGetTargetPlayer(out var target) || !HasActiveCombatState(target))
                return EmptyBrowser(L(
                    "ritsulib.debugTools.empty.secondaryResourcesCombat",
                    "Start a combat to inspect and change secondary resources."));

            var root = DetailShell(
                definition.Id,
                () => SecondaryResourceIcon(definition),
                SecondaryResourceMetadata(target, definition),
                SecondaryResourceDescription(target, definition),
                () => TryGetTargetPlayer(out var refreshedTarget)
                    ? SecondaryResourceMetadata(refreshedTarget, definition)
                    : SecondaryResourceTitle(definition),
                () => TryGetTargetPlayer(out var refreshedTarget)
                    ? SecondaryResourceDescription(refreshedTarget, definition)
                    : string.Empty);
            var settings = CreateAdjustmentContent();
            AddIntegerActionRow(
                settings,
                L("ritsulib.debugTools.secondaryResource.gainAmount", "Gain"),
                "1",
                1,
                int.MaxValue,
                value => SubmitSecondaryResource(definition, RitsuDebugSecondaryResourceOperation.Gain, value),
                L("ritsulib.debugTools.action.gain", "Gain"));
            AddIntegerActionRow(
                settings,
                L("ritsulib.debugTools.secondaryResource.loseAmount", "Lose"),
                "1",
                1,
                int.MaxValue,
                value => SubmitSecondaryResource(definition, RitsuDebugSecondaryResourceOperation.Lose, value),
                L("ritsulib.debugTools.action.lose", "Lose"));
            var setAmount = CreateIntegerEdit(SecondaryResourceCmd.Get(target, definition.Id).ToString());
            var setAmountChanged = false;
            setAmount.TextChanged += _ => setAmountChanged = true;
            settings.AddChild(ActionField(
                L("ritsulib.debugTools.secondaryResource.setAmount", "Set amount"),
                setAmount,
                ActionButton(
                    L("ritsulib.debugTools.action.set", "Set"),
                    ModSettingsButtonTone.Accent,
                    () =>
                    {
                        if (TryReadInt(setAmount, definition.MinAmount, definition.HardMaxAmount, out var value))
                            SubmitSecondaryResource(definition, RitsuDebugSecondaryResourceOperation.Set, value);
                    })));
            root.RegisterRefresh(() =>
            {
                if (!setAmountChanged && !setAmount.HasFocus() && TryGetTargetPlayer(out var refreshedTarget))
                {
                    setAmount.Text = SecondaryResourceCmd.Get(refreshedTarget, definition.Id).ToString();
                    setAmountChanged = false;
                }
            });

            var resetActions = new List<(string Text, ModSettingsButtonTone Tone, Action Action)>
            {
                (L("ritsulib.debugTools.action.resetDefault", "Reset to default"),
                    ModSettingsButtonTone.Normal,
                    () => SubmitSecondaryResource(definition, RitsuDebugSecondaryResourceOperation.Reset)),
            };
            if (SecondaryResourceCmd.GetMax(target, definition.Id).HasValue)
                resetActions.Add((
                    L("ritsulib.debugTools.action.resetMaximum", "Reset to maximum"),
                    ModSettingsButtonTone.Normal,
                    () => SubmitSecondaryResource(definition, RitsuDebugSecondaryResourceOperation.ResetToMax)));
            settings.AddChild(ActionGrid(resetActions));
            root.AddChild(AdjustmentSection(
                L("ritsulib.debugTools.secondaryResource.change", "Change amount"),
                settings));
            return root;
        }

        private void SubmitSecondaryResource(
            SecondaryResourceDefinition definition,
            RitsuDebugSecondaryResourceOperation operation,
            int value = 0)
        {
            if (!TryGetActionContext(out var requester, out var target))
                return;
            RunAction(() => RitsuDebugSecondaryResourceActions.Submit(
                requester,
                target,
                definition.Id,
                operation,
                value));
        }

        private static string SecondaryResourceMetadata(
            Player target,
            SecondaryResourceDefinition definition)
        {
            var amount = SecondaryResourceCmd.Get(target, definition.Id);
            var maximum = SecondaryResourceCmd.GetMax(target, definition.Id);
            var bounds = maximum.HasValue
                ? string.Format(
                    L("ritsulib.debugTools.secondaryResource.boundsWithMaximum", "Range {0}–{1} · Current max {2}"),
                    definition.MinAmount,
                    definition.HardMaxAmount,
                    maximum.Value)
                : string.Format(
                    L("ritsulib.debugTools.secondaryResource.bounds", "Range {0}–{1} · No current max"),
                    definition.MinAmount,
                    definition.HardMaxAmount);
            return $"{SecondaryResourceTitle(definition)} · {SecondaryResourceAmountText(amount, maximum)}\n{bounds}";
        }

        private static string SecondaryResourceDescription(
            Player target,
            SecondaryResourceDefinition definition)
        {
            try
            {
                var amount = SecondaryResourceCmd.Get(target, definition.Id);
                var maximum = SecondaryResourceCmd.GetMax(target, definition.Id);
                return SecondaryResourceText.GetDescription(definition, amount, maximum)?.GetFormattedText() ??
                       definition.EffectiveDescriptionKey;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                return definition.EffectiveDescriptionKey;
            }
        }

        private static string SecondaryResourceTitle(SecondaryResourceDefinition definition)
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

        private static string SecondaryResourceAmountText(int amount, int? maximum)
        {
            return maximum.HasValue
                ? string.Format(L("ritsulib.debugTools.secondaryResource.amountWithMaximum", "{0}/{1}"), amount,
                    maximum.Value)
                : amount.ToString();
        }

        private static Texture2D? SecondaryResourceIcon(SecondaryResourceDefinition definition)
        {
            var iconPath = definition.LargeIconPath ?? definition.SmallIconPath;
            if (!string.IsNullOrWhiteSpace(iconPath))
                try
                {
                    var texture = ResourceLoader.Load<Texture2D>(iconPath.Trim());
                    if (texture != null)
                        return texture;
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[DebugToolsUi] Could not load secondary-resource icon '{iconPath}' for '{definition.Id}': {ex.Message}");
                }

            return RitsuDebugToolsIcons.Get(
                RitsuDebugToolsGlyph.Sliders,
                32,
                RitsuShellTheme.Current.Text.LabelPrimary);
        }
    }
}
