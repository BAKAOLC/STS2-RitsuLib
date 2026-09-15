using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Godot;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds the capabilities selected by <see cref="ModCardPileViewSpec" /> to the vanilla pile screen.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 <see cref="ModCardPileViewSpec" /> 选择的能力添加到原版牌堆界面。
    ///     </para>
    /// </summary>
    internal sealed class ModCardPileScreenViewPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<NCardPileScreen, ModCardPileScreenViewController> Controllers =
            [];

        private static readonly AccessTools.FieldRef<NCardPileScreen, NCardGrid> GridRef =
            AccessTools.FieldRefAccess<NCardPileScreen, NCardGrid>("_grid");

        private static readonly AccessTools.FieldRef<NCardPileScreen, NButton> BackButtonRef =
            AccessTools.FieldRefAccess<NCardPileScreen, NButton>("_backButton");

        public static string PatchId => "ritsulib_card_pile_screen_mod_view";
        public static string Description => "Add opt-in card inspection, upgrade preview, and sorting to mod piles";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardPileScreen), nameof(NCardPileScreen._Ready))];
        }

        public static void Postfix(NCardPileScreen __instance)
        {
            if (!ModCardPileRegistry.TryGetByPileType(__instance.Pile.Type, out var definition))
                return;
            if (definition.View is not { HasAnyCapability: true } view)
                return;
            if (Controllers.TryGetValue(__instance, out _))
                return;

            var controller = new ModCardPileScreenViewController(
                __instance,
                definition,
                view,
                GridRef(__instance),
                BackButtonRef(__instance));

            Controllers.Add(__instance, controller);
            controller.Install();
        }
    }

    internal sealed class ModCardPileScreenViewController(
        NCardPileScreen screen,
        ModCardPileDefinition definition,
        ModCardPileViewSpec view,
        NCardGrid grid,
        NButton backButton)
    {
        private const string SortButtonScenePath = "res://scenes/screens/deck_view_screen/deck_view_sort_button.tscn";
        private const string DefaultSortBarTexturePath = "res://images/ui/color_tab_bar.png";
        private const string TickboxScenePath = "res://scenes/ui/tickbox.tscn";

        private readonly List<SortingOrders> _sortingPriority = view.CreateDefaultSorting();
        private readonly List<NButton> _controls = [backButton];
        private HBoxContainer? _toolbar;
        private Control? _toolbarBackground;
        private ModCardPileViewStyleContext StyleContext => new(definition, screen.Pile, screen);

        public void Install()
        {
            if (view.EnableCardInspect)
                InstallCardInspection();

            if (view.EnableSortBar)
                InstallToolbar();

            if (view.EnableUpgradePreviewToggle)
                InstallUpgradeToggle();

            screen.Pile.ContentsChanged += RefreshCards;
            screen.TreeExiting += OnScreenTreeExiting;
            grid.Resized += LayoutToolbar;
            ActiveScreenContext.Instance.Updated += UpdateInputState;
            UpdateInputState();
            RefreshCards();
        }

        public void RefreshCards()
        {
            if (!GodotObject.IsInstanceValid(grid))
                return;

            grid.SetCards([.. screen.Pile.Cards], screen.Pile.Type, [.. _sortingPriority]);
        }

        private void OnScreenTreeExiting()
        {
            screen.Pile.ContentsChanged -= RefreshCards;
            screen.TreeExiting -= OnScreenTreeExiting;
            grid.Resized -= LayoutToolbar;
            ActiveScreenContext.Instance.Updated -= UpdateInputState;
        }

        private void UpdateInputState()
        {
            var enabled = ActiveScreenContext.Instance.IsCurrent(screen);
            foreach (var control in _controls)
            {
                if (!GodotObject.IsInstanceValid(control))
                    continue;
                if (enabled)
                    control.Enable();
                else
                    control.Disable();
            }
        }

        private void InstallCardInspection()
        {
            grid.Connect(NCardGrid.SignalName.HolderPressed,
                Callable.From<NCardHolder>(holder => ShowCardDetail(holder.CardModel)));
            grid.Connect(NCardGrid.SignalName.HolderAltPressed,
                Callable.From<NCardHolder>(holder => ShowCardDetail(holder.CardModel)));
        }

        private void ShowCardDetail(CardModel? cardModel)
        {
            if (cardModel == null || !ActiveScreenContext.Instance.IsCurrent(screen))
                return;

            // ReSharper disable once RedundantEnumerableCastCall
            var cards = grid.CurrentlyDisplayedCards.OfType<CardModel>().ToList();
            var index = cards.IndexOf(cardModel);
            if (index < 0)
                return;

            var game = NGame.Instance;
            if (game == null)
                return;

            var inspectCardScreen = game.GetInspectCardScreen();
            inspectCardScreen.Open(cards, index, grid.IsShowingUpgrades);
        }

        private void InstallToolbar()
        {
            grid.YOffset = Math.Max(grid.YOffset, 100);

            var toolbar = new HBoxContainer
            {
                Name = $"RitsuLibCardPileViewToolbar_{definition.Id}",
                MouseFilter = Control.MouseFilterEnum.Pass,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            toolbar.AddThemeConstantOverride("separation", 30);

            var background = CreateToolbarBackground();
            if (background != null)
                grid.AddChild(background);

            grid.AddChild(toolbar);
            _toolbar = toolbar;
            _toolbarBackground = background;
            InstallSortButtons(toolbar);
            toolbar.MinimumSizeChanged += LayoutToolbar;
            LayoutToolbar();
        }

        private void LayoutToolbar()
        {
            if (_toolbar == null)
                return;

            var width = Math.Max(1f, _toolbar.GetCombinedMinimumSize().X);
            var scale = Math.Clamp((grid.Size.X - 64f) / (width + 48f), 0.01f, 1f);
            _toolbar.Size = new(width, 60f);
            _toolbar.Scale = Vector2.One * scale;
            _toolbar.Position = new((grid.Size.X - width * scale) * 0.5f, 92f);
            if (_toolbarBackground == null)
                return;

            _toolbarBackground.Position = _toolbar.Position - new Vector2(24f * scale, 4f);
            _toolbarBackground.Size = new((width + 48f) * scale, 68f);
        }

        private Control? CreateToolbarBackground()
        {
            TextureRect? background = null;
            try
            {
                background = new()
                {
                    Name = $"RitsuLibCardPileViewToolbarBg_{definition.Id}",
                    Texture = PreloadManager.Cache.GetAsset<Texture2D>(
                        view.ToolbarBackgroundTexturePath ?? DefaultSortBarTexturePath),
                    Material = ResolveToolbarBackgroundMaterial(),
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                };
                return background;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                background?.QueueFreeSafely();
                RitsuLibFramework.Logger.Warn(
                    $"[CardPiles] Could not load mod pile toolbar background for '{definition.Id}': {ex}");
                return null;
            }
        }

        private void InstallSortButtons(HBoxContainer toolbar)
        {
            foreach (var option in view.GetSortOptions())
            {
                var button = CreateSortButton(option);
                if (button == null)
                    continue;

                WhenReady(button, () =>
                {
                    button.SetLabel(GetSortLabel(option));
                    button.IsDescending = _sortingPriority.FirstOrDefault(order =>
                        order == option.Ascending() || order == option.Descending()) == option.Descending();
                    ApplySortButtonBackground(button);
                    SetSortButtonHue(button);
                });
                button.Connect(NClickableControl.SignalName.Released,
                    Callable.From<NButton>(_ => OnSortReleased(option, button)));
                _controls.Add(button);
                toolbar.AddChild(button);
            }
        }

        private NCardViewSortButton? CreateSortButton(ModCardPileSortOption option)
        {
            NCardViewSortButton? button = null;
            try
            {
                button = PreloadManager.Cache.GetScene(SortButtonScenePath)
                    .Instantiate<NCardViewSortButton>();
                button.Name = $"RitsuLibSort_{option}";
                button.CustomMinimumSize = new(220f, 42f);
                return button;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                button?.QueueFreeSafely();
                RitsuLibFramework.Logger.Warn(
                    $"[CardPiles] Could not create sort button '{option}' for '{definition.Id}': {ex}");
                return null;
            }
        }

        private static void WhenReady(Node node, Action action)
        {
            if (node.IsNodeReady())
                action();
            else
                node.Connect(Node.SignalName.Ready, Callable.From(action), (uint)GodotObject.ConnectFlags.OneShot);
        }

        private void SetSortButtonHue(NCardViewSortButton button)
        {
            if (view.DisableSortButtonHue)
                return;

            var material = ResolveSortButtonHueMaterial();
            if (material == null)
                return;

            button.SetHue(material);
        }

        private void ApplySortButtonBackground(NCardViewSortButton button)
        {
            if (string.IsNullOrWhiteSpace(view.SortButtonBackgroundTexturePath) &&
                view.SortButtonBackgroundMaterial == null &&
                view.SortButtonBackgroundMaterialProvider == null)
                return;

            var background = button.GetNodeOrNull<TextureRect>("%ButtonImage");
            if (background == null)
                return;

            if (!string.IsNullOrWhiteSpace(view.SortButtonBackgroundTexturePath))
                try
                {
                    background.Texture =
                        PreloadManager.Cache.GetAsset<Texture2D>(view.SortButtonBackgroundTexturePath);
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[CardPiles] Could not load sort button background texture for '{definition.Id}': {ex}");
                }

            var material = ResolveSortButtonBackgroundMaterial();
            if (material != null)
                background.Material = material;
        }

        private void OnSortReleased(ModCardPileSortOption option, NCardViewSortButton button)
        {
            if (!ActiveScreenContext.Instance.IsCurrent(screen))
                return;
            _sortingPriority.Remove(option.Ascending());
            _sortingPriority.Remove(option.Descending());
            _sortingPriority.Insert(0, button.IsDescending ? option.Descending() : option.Ascending());
            RefreshCards();
        }

        private void InstallUpgradeToggle()
        {
            NTickbox? toggle = null;
            try
            {
                toggle = new()
                {
                    Name = "RitsuLibViewUpgrades",
                    CustomMinimumSize = new(250f, 64f),
                    AnchorTop = 1f,
                    AnchorBottom = 1f,
                    OffsetLeft = 16f,
                    OffsetTop = -76f,
                    OffsetRight = 266f,
                    OffsetBottom = -12f,
                    GrowVertical = Control.GrowDirection.Begin,
                    Scale = Vector2.One * 0.75f,
                    FocusMode = Control.FocusModeEnum.All,
                    MouseFilter = Control.MouseFilterEnum.Stop,
                };

                var visuals = PreloadManager.Cache.GetScene(TickboxScenePath)
                    .Instantiate<Control>();
                toggle.AddUniqueChild(visuals);

                var label = new MegaLabel
                {
                    Name = "ViewUpgradesLabel",
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    CustomMinimumSize = new(170f, 42f),
                    Size = new(250f, 64f),
                    Position = new(64f, 0f),
                    AutoSizeEnabled = false,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                label.AddThemeFontOverride("font", PreloadManager.Cache.GetAsset<Font>(
                    "res://themes/kreon_bold_glyph_space_one.tres"));
                label.AddThemeFontSizeOverride("font_size", 28);
                label.AddThemeConstantOverride("outline_size", 12);
                label.AddThemeColorOverride("font_color",
                    view.UpgradePreviewLabelColor ?? new Color(0.937255f, 0.784314f, 0.317647f));
                label.AddThemeColorOverride("font_outline_color",
                    view.UpgradePreviewLabelOutlineColor ?? new Color(0f, 0f, 0f, 0.5f));
                label.SetTextAutoSize(new LocString("gameplay_ui", "VIEW_UPGRADES").GetFormattedText());
                toggle.AddChild(label);

                var upgradeToggle = toggle;
                WhenReady(toggle, () =>
                {
                    upgradeToggle.IsTicked = grid.IsShowingUpgrades;
                    label.Size = new(label.GetMinimumSize().X, 64f);
                    upgradeToggle.CustomMinimumSize = new(64f + label.Size.X, 64f);
                });

                toggle.Connect(NTickbox.SignalName.Toggled,
                    Callable.From<NTickbox>(tickbox =>
                    {
                        if (ActiveScreenContext.Instance.IsCurrent(screen))
                            grid.IsShowingUpgrades = tickbox.IsTicked;
                    }));
                _controls.Add(toggle);
                screen.AddChild(toggle);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                toggle?.QueueFreeSafely();
                RitsuLibFramework.Logger.Warn(
                    $"[CardPiles] Could not create upgrade preview toggle for '{definition.Id}': {ex}");
            }
        }

        private static string GetSortLabel(ModCardPileSortOption option)
        {
            var key = option switch
            {
                ModCardPileSortOption.Obtained => "SORT_OBTAINED",
                ModCardPileSortOption.Type => "SORT_TYPE",
                ModCardPileSortOption.Cost => "SORT_COST",
                ModCardPileSortOption.Alphabetical => "SORT_ALPHABET",
                ModCardPileSortOption.Rarity => "SORT_RARITY",
                _ => "SORT_OBTAINED",
            };

            return new LocString("gameplay_ui", key).GetRawText();
        }

        private Material? ResolveToolbarBackgroundMaterial()
        {
            if (view.ToolbarBackgroundMaterialProvider == null)
                return view.ToolbarBackgroundMaterial;

            try
            {
                return view.ToolbarBackgroundMaterialProvider(StyleContext) ?? view.ToolbarBackgroundMaterial;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardPiles] ToolbarBackgroundMaterialProvider for '{definition.Id}' threw: {ex}");
                return view.ToolbarBackgroundMaterial;
            }
        }

        private ShaderMaterial? ResolveSortButtonHueMaterial()
        {
            if (view.SortButtonHueMaterialProvider == null)
                return view.SortButtonHueMaterial;

            try
            {
                return view.SortButtonHueMaterialProvider(StyleContext) ?? view.SortButtonHueMaterial;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardPiles] SortButtonHueMaterialProvider for '{definition.Id}' threw: {ex}");
                return view.SortButtonHueMaterial;
            }
        }

        private Material? ResolveSortButtonBackgroundMaterial()
        {
            if (view.SortButtonBackgroundMaterialProvider == null)
                return view.SortButtonBackgroundMaterial;

            try
            {
                return view.SortButtonBackgroundMaterialProvider(StyleContext) ?? view.SortButtonBackgroundMaterial;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardPiles] SortButtonBackgroundMaterialProvider for '{definition.Id}' threw: {ex}");
                return view.SortButtonBackgroundMaterial;
            }
        }
    }
}
