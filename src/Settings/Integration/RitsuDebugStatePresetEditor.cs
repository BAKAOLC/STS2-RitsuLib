using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Ui.Overlay;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    // ReSharper disable once Godot.MissingParameterlessConstructor
    internal sealed partial class RitsuDebugStatePresetEditor : HBoxContainer
    {
        private const float PresetListWidth = 240f;
        private const float DetailDrawerWidth = 580f;
        private readonly Func<RitsuDebugStatePreset, bool> _apply;
        private readonly Func<Player?> _getTarget;
        private readonly Dictionary<string, string> _modelTitles = new(StringComparer.Ordinal);
        private readonly Action<string, bool> _setStatus;
        private RitsuDebugStatePresetCardGrid? _cardGrid;
        private VBoxContainer _contentBody = null!;
        private bool _dirty;
        private RitsuDebugStatePreset? _draft;
        private Control _dragLayer = null!;
        private VBoxContainer _drawerBody = null!;
        private Control _drawerLayer = null!;
        private PanelContainer _drawerPanel = null!;
        private VBoxContainer _drawerPinnedBody = null!;
        private Label _drawerTitle = null!;
        private Tween? _drawerTween;
        private VBoxContainer _mainBody = null!;
        private PresetPage _page = PresetPage.Cards;
        private VBoxContainer _presetList = null!;
        private PileType _selectedPile = PileType.Deck;

        internal RitsuDebugStatePresetEditor(
            Func<RitsuDebugStatePreset, bool> apply,
            Func<Player?> getTarget,
            Action<string, bool> setStatus)
        {
            ArgumentNullException.ThrowIfNull(apply);
            ArgumentNullException.ThrowIfNull(getTarget);
            ArgumentNullException.ThrowIfNull(setStatus);
            _apply = apply;
            _getTarget = getTarget;
            _setStatus = setStatus;
        }

        public override void _Ready()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;
            CustomMinimumSize = new(0f, 540f);
            AddThemeConstantOverride("separation", 10);
            CacheModelTitles(ModelDb.AllCards);
            CacheModelTitles(ModelDb.AllRelics);
            CacheModelTitles(ModelDb.AllPotions);
            CacheModelTitles(ModelDb.AllPowers);
            CacheModelTitles(ModelDb.DebugEnchantments);
            BuildPresetRail();
            BuildWorkspace();
            SelectInitialPreset();
        }

        private void BuildPresetRail()
        {
            var panel = CreatePane(PresetListWidth);
            AddChild(panel);
            var body = CreatePaneBody(panel);
            var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            header.AddThemeConstantOverride("separation", 6);
            var title = SectionTitle(L("ritsulib.debugTools.statePresets.title", "State presets"));
            title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            header.AddChild(title);
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.new", "New"),
                ModSettingsButtonTone.Accent,
                RequestNewPreset,
                58f));
            body.AddChild(header);

            var transfer = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            transfer.AddThemeConstantOverride("separation", 6);
            transfer.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.import", "Import"),
                ModSettingsButtonTone.Normal,
                ImportFromClipboard));
            body.AddChild(transfer);

            var scroll = CreateScroll();
            body.AddChild(scroll);
            _presetList = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            _presetList.AddThemeConstantOverride("separation", 4);
            scroll.AddChild(_presetList);
        }

        private void BuildWorkspace()
        {
            var host = new Control
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                ClipContents = true,
            };
            AddChild(host);

            var panel = CreatePane(0f, true);
            panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            host.AddChild(panel);
            _mainBody = CreatePaneBody(panel);

            _drawerLayer = new()
            {
                Visible = false,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _drawerLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            host.AddChild(_drawerLayer);
            var backdrop = new ColorRect
            {
                Color = new(0.01f, 0.015f, 0.025f, 0.52f),
                MouseFilter = MouseFilterEnum.Stop,
            };
            backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            backdrop.GuiInput += inputEvent =>
            {
                if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                    CloseDrawer();
            };
            _drawerLayer.AddChild(backdrop);

            _drawerPanel = CreatePane(DetailDrawerWidth);
            _drawerPanel.AnchorLeft = 1f;
            _drawerPanel.AnchorRight = 1f;
            _drawerPanel.AnchorTop = 0f;
            _drawerPanel.AnchorBottom = 1f;
            SetDrawerOffsets(0f, DetailDrawerWidth);
            _drawerLayer.AddChild(_drawerPanel);
            var drawerFrame = CreatePaneBody(_drawerPanel);
            var drawerHeader = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            drawerHeader.AddThemeConstantOverride("separation", 8);
            _drawerTitle = SectionTitle(string.Empty);
            _drawerTitle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            drawerHeader.AddChild(_drawerTitle);
            drawerHeader.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.done", "Done"),
                ModSettingsButtonTone.Normal,
                () => CloseDrawer(),
                70f));
            drawerFrame.AddChild(drawerHeader);
            _drawerPinnedBody = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            _drawerPinnedBody.AddThemeConstantOverride("separation", 8);
            drawerFrame.AddChild(_drawerPinnedBody);
            var scroll = CreateScroll();
            drawerFrame.AddChild(scroll);
            _drawerBody = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            _drawerBody.AddThemeConstantOverride("separation", 10);
            scroll.AddChild(_drawerBody);

            _dragLayer = new()
            {
                MouseFilter = MouseFilterEnum.Ignore,
                ZIndex = 80,
            };
            _dragLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            host.AddChild(_dragLayer);
        }

        private void SelectInitialPreset()
        {
            var first = RitsuDebugStatePresetStore.GetSnapshot().FirstOrDefault();
            if (first == null)
                CreateNewPreset();
            else
                SelectPreset(first);
        }

        private void CreateNewPreset()
        {
            var names = RitsuDebugStatePresetStore.GetSnapshot()
                .Select(static preset => preset.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var baseName = L("ritsulib.debugTools.statePresets.newName", "New preset");
            var name = baseName;
            for (var suffix = 2; names.Contains(name); suffix++)
                name = $"{baseName} {suffix}";
            _draft = new() { Name = name };
            _dirty = true;
            _page = PresetPage.Cards;
            _selectedPile = PileType.Deck;
            CloseDrawer(false);
            RebuildAll();
        }

        private void RequestNewPreset()
        {
            RunAfterDiscardConfirmation(CreateNewPreset);
        }

        private void SelectPreset(RitsuDebugStatePreset preset)
        {
            _draft = preset.Clone();
            _dirty = false;
            CloseDrawer(false);
            RebuildAll();
        }

        private void RequestSelectPreset(RitsuDebugStatePreset preset)
        {
            RunAfterDiscardConfirmation(() => SelectPreset(preset));
        }

        private void RunAfterDiscardConfirmation(Action action)
        {
            if (!_dirty)
            {
                action();
                return;
            }

            ModSettingsUiFactory.ShowStyledConfirm(
                this,
                L("ritsulib.debugTools.statePresets.discardTitle", "Discard unsaved changes?"),
                L("ritsulib.debugTools.statePresets.discardBody",
                    "The current preset has changes that have not been saved."),
                L("ritsulib.debugTools.statePresets.cancel", "Cancel"),
                L("ritsulib.debugTools.statePresets.discard", "Discard"),
                true,
                action);
        }

        private void RebuildAll()
        {
            RebuildPresetList();
            RebuildMain();
        }

        private void RebuildPresetList()
        {
            ClearChildren(_presetList);
            var presets = RitsuDebugStatePresetStore.GetSnapshot();
            foreach (var preset in presets)
            {
                var selected = _draft != null && preset.Id.Equals(_draft.Id, StringComparison.OrdinalIgnoreCase);
                var unsaved = selected && _dirty;
                var button = new ModSettingsMiniButton(
                    unsaved
                        ? $"{preset.Name} · {L("ritsulib.debugTools.statePresets.unsaved", "Unsaved")}"
                        : preset.Name,
                    () => RequestSelectPreset(preset))
                {
                    Alignment = HorizontalAlignment.Left,
                    CustomMinimumSize = new(0f, 36f),
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    TooltipText = $"{preset.Name}\n{PresetSummary(preset)}",
                };
                ApplySelectionStyle(button, selected);
                ApplyUnsavedStyle(button, unsaved);
                _presetList.AddChild(button);
            }

            if (_draft == null ||
                presets.Any(preset => preset.Id.Equals(_draft.Id, StringComparison.OrdinalIgnoreCase)))
                return;
            var draft = new ModSettingsMiniButton(
                $"{_draft.Name} · {L("ritsulib.debugTools.statePresets.unsaved", "Unsaved")}",
                static () => { })
            {
                Alignment = HorizontalAlignment.Left,
                CustomMinimumSize = new(0f, 36f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            ApplySelectionStyle(draft, true);
            ApplyUnsavedStyle(draft, true);
            _presetList.AddChild(draft);
        }

        private void RebuildMain()
        {
            ClearChildren(_mainBody);
            _cardGrid = null;
            if (_draft == null)
            {
                _mainBody.AddChild(Hint(L("ritsulib.debugTools.statePresets.noSelection", "Select a preset.")));
                return;
            }

            BuildTopToolbar();
            BuildPageNavigation();
            var frame = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            frame.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListItemCardStyle());
            var margin = CreateMargin(frame, 12, 10);
            _contentBody = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            _contentBody.AddThemeConstantOverride("separation", 10);
            margin.AddChild(_contentBody);
            _mainBody.AddChild(frame);
            BuildCurrentPage();
        }

        private void BuildTopToolbar()
        {
            var toolbar = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            toolbar.AddThemeConstantOverride("h_separation", 6);
            toolbar.AddThemeConstantOverride("v_separation", 6);
            var name = new LineEdit
            {
                Text = _draft!.Name,
                PlaceholderText = L("ritsulib.debugTools.statePresets.name", "Preset name"),
                MaxLength = RitsuDebugStatePresetStore.MaximumNameLength,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new(240f, 34f),
            };
            ModSettingsUiControlTheming.ApplyEntryLineEditValueFieldTheme(
                name,
                RitsuShellTheme.Current.Font.Body,
                RitsuShellTheme.Current.Metric.FontSize.ValueLabel);
            name.TextChanged += value =>
            {
                _draft.Name = value;
                MarkDirty();
            };
            toolbar.AddChild(name);
            toolbar.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.apply", "Apply"),
                ModSettingsButtonTone.Accent,
                ApplyDraft,
                72f));
            toolbar.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.save", "Save"),
                ModSettingsButtonTone.Normal,
                SaveDraft,
                68f));
            toolbar.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.fill", "Fill"),
                ModSettingsButtonTone.Normal,
                ShowCaptureDrawer,
                62f));
            toolbar.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.export", "Export"),
                ModSettingsButtonTone.Normal,
                ExportToClipboard,
                72f));
            toolbar.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.more", "More"),
                ModSettingsButtonTone.Normal,
                ShowManagementDrawer,
                64f));
            _mainBody.AddChild(toolbar);

            var valuePolicy = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            valuePolicy.AddThemeConstantOverride("h_separation", 8);
            valuePolicy.AddThemeConstantOverride("v_separation", 6);
            valuePolicy.AddChild(ValuePolicyToggle(
                L("ritsulib.debugTools.statePresets.recordInternalValues", "Record internal values when filling"),
                L("ritsulib.debugTools.statePresets.recordInternalValuesHint",
                    "When enabled, Fill also copies upgrades, model dynamic values, relic stacks, and current Power amounts."),
                _draft.RecordInternalValues,
                enabled =>
                {
                    _draft.RecordInternalValues = enabled;
                    MarkDirty();
                }));
            valuePolicy.AddChild(ValuePolicyToggle(
                L("ritsulib.debugTools.statePresets.applyInternalValues", "Use saved internal values when applying"),
                L("ritsulib.debugTools.statePresets.applyInternalValuesHint",
                    "When disabled, cards, relics, potions, and Powers use their normal defaults even if the preset stores adjustments."),
                _draft.ApplyInternalValues,
                enabled =>
                {
                    _draft.ApplyInternalValues = enabled;
                    MarkDirty();
                }));
            _mainBody.AddChild(valuePolicy);
        }

        private static Button ValuePolicyToggle(
            string text,
            string tooltip,
            bool selected,
            Action<bool> changed)
        {
            var toggle = ModSettingsUiControlTheming.CreateCompactSettingsToggleButton(text, selected);
            toggle.CustomMinimumSize = new(310f, 36f);
            toggle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            toggle.TooltipText = tooltip;
            toggle.Toggled += enabled => changed(enabled);
            return toggle;
        }

        private void BuildPageNavigation()
        {
            var navigation = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            navigation.AddThemeConstantOverride("h_separation", 6);
            navigation.AddThemeConstantOverride("v_separation", 6);
            AddPage(PresetPage.Cards, "ritsulib.debugTools.category.pileCards", "Card piles",
                RitsuDebugToolsGlyph.PileCards);
            AddPage(PresetPage.Relics, "ritsulib.debugTools.category.relics", "Relics",
                RitsuDebugToolsGlyph.Relics);
            AddPage(PresetPage.Potions, "ritsulib.debugTools.category.potions", "Potions",
                RitsuDebugToolsGlyph.Potions);
            AddPage(PresetPage.Powers, "ritsulib.debugTools.category.powers", "Powers",
                RitsuDebugToolsGlyph.Powers);
            AddPage(PresetPage.Extensions, "ritsulib.debugTools.category.extensions",
                "RitsuLib extensions", RitsuDebugToolsGlyph.Puzzle);
            AddPage(PresetPage.Player, "ritsulib.debugTools.category.players", "Player",
                RitsuDebugToolsGlyph.Players);
            _mainBody.AddChild(navigation);
            return;

            void AddPage(PresetPage page, string key, string fallback, RitsuDebugToolsGlyph glyph)
            {
                var button = new ModSettingsMiniButton(L(key, fallback), () => SelectPage(page))
                {
                    CustomMinimumSize = new(112f, 38f),
                    Icon = RitsuDebugToolsIcons.Get(
                        glyph,
                        18,
                        RitsuShellTheme.Current.Text.LabelPrimary),
                };
                ApplySelectionStyle(button, _page == page);
                navigation.AddChild(button);
            }
        }

        private void SelectPage(PresetPage page)
        {
            if (_page == page)
                return;
            _page = page;
            CloseDrawer(false);
            RebuildMain();
        }

        private void BuildCurrentPage()
        {
            switch (_page)
            {
                case PresetPage.Cards:
                    BuildCardsPage();
                    break;
                case PresetPage.Relics:
                    BuildRelicsPage();
                    break;
                case PresetPage.Potions:
                    BuildPotionsPage();
                    break;
                case PresetPage.Powers:
                    BuildPowersPage();
                    break;
                case PresetPage.Extensions:
                    BuildExtensionsPage();
                    break;
                case PresetPage.Player:
                    BuildPlayerPage();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OpenDrawer()
        {
            _drawerTween?.Kill();
            _drawerLayer.Visible = true;
            _drawerLayer.MouseFilter = MouseFilterEnum.Pass;
            SetDrawerOffsets(0f, DetailDrawerWidth);
            _drawerTween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            _drawerTween.TweenMethod(Callable.From<float>(offset =>
                SetDrawerOffsets(offset, offset + DetailDrawerWidth)), 0f, -DetailDrawerWidth, 0.18f);
        }

        private void CloseDrawer(bool animate = true)
        {
            _drawerTween?.Kill();
            if (!_drawerLayer.Visible)
                return;
            if (!animate)
            {
                _drawerLayer.Visible = false;
                _drawerLayer.MouseFilter = MouseFilterEnum.Ignore;
                SetDrawerOffsets(0f, DetailDrawerWidth);
                return;
            }

            _drawerTween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
            _drawerTween.TweenMethod(Callable.From<float>(offset =>
                SetDrawerOffsets(offset, offset + DetailDrawerWidth)), -DetailDrawerWidth, 0f, 0.14f);
            _drawerTween.TweenCallback(Callable.From(() =>
            {
                _drawerLayer.Visible = false;
                _drawerLayer.MouseFilter = MouseFilterEnum.Ignore;
                RebuildMain();
            }));
        }

        private void SetDrawerOffsets(float left, float right)
        {
            _drawerPanel.OffsetLeft = left;
            _drawerPanel.OffsetRight = right;
        }

        private void MarkDirty(bool rebuild = false)
        {
            _dirty = true;
            RebuildPresetList();
            if (rebuild)
                RebuildMain();
        }

        private void MarkInternalValuesDirty(bool rebuild = false)
        {
            _draft!.ApplyInternalValues = true;
            MarkDirty(rebuild);
        }

        private static void ApplyUnsavedStyle(Button button, bool unsaved)
        {
            if (!unsaved)
                return;
            var color = RitsuShellTheme.Current.Component.TextButton.Accent.Fg;
            button.AddThemeColorOverride("font_color", color);
            button.AddThemeColorOverride("font_hover_color", color);
            button.AddThemeColorOverride("font_pressed_color", color);
            button.AddThemeColorOverride("font_focus_color", color);
        }

        private enum PresetPage
        {
            Cards,
            Relics,
            Potions,
            Powers,
            Extensions,
            Player,
        }
    }
}
