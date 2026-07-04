using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Adds opt-in deck-view affordances to the vanilla pile screen for registered RitsuLib piles.
    ///     为已注册的 RitsuLib 牌堆给原版牌堆 screen 增加可选的牌组查看能力。
    /// </summary>
    internal sealed class ModCardPileScreenViewPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<NCardPileScreen, ModCardPileScreenViewController> Controllers =
            new();

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
        private ModCardPileViewStyleContext StyleContext => new(definition, screen.Pile, screen);

        public void Install()
        {
            if (view.EnableCardInspect)
                InstallCardInspection();

            if (view.EnableSortBar || view.EnableUpgradePreviewToggle)
                InstallToolbar();

            screen.Pile.ContentsChanged += RefreshCards;
            screen.TreeExiting += OnScreenTreeExiting;
            RefreshCards();
        }

        public void RefreshCards()
        {
            if (!GodotObject.IsInstanceValid(grid))
                return;

            grid.SetCards(screen.Pile.Cards.ToList(), screen.Pile.Type, [.. _sortingPriority]);
        }

        private void OnScreenTreeExiting()
        {
            screen.Pile.ContentsChanged -= RefreshCards;
            screen.TreeExiting -= OnScreenTreeExiting;
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
            if (cardModel == null)
                return;

            // ReSharper disable once RedundantEnumerableCastCall
            var cards = grid.CurrentlyDisplayedCards.OfType<CardModel>().ToList();
            var index = cards.IndexOf(cardModel);
            if (index < 0)
                return;

            var game = NGame.Instance;
            if (game == null)
                return;

            backButton.Disable();

            var inspectCardScreen = game.GetInspectCardScreen();
            inspectCardScreen.Open(cards, index, grid.IsShowingUpgrades);
            inspectCardScreen.Connect(CanvasItem.SignalName.VisibilityChanged, Callable.From(delegate
            {
                if (!inspectCardScreen.Visible && GodotObject.IsInstanceValid(backButton))
                    backButton.Enable();
            }), 4u);
        }

        private void InstallToolbar()
        {
            grid.YOffset = Math.Max(grid.YOffset, 100);

            var toolbar = new HBoxContainer
            {
                Name = $"RitsuLibCardPileViewToolbar_{definition.Id}",
                MouseFilter = Control.MouseFilterEnum.Pass,
                AnchorLeft = 0.5f,
                AnchorRight = 0.5f,
                AnchorTop = 0f,
                AnchorBottom = 0f,
                OffsetLeft = -560f,
                OffsetTop = 92f,
                OffsetRight = 560f,
                OffsetBottom = 152f,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            toolbar.AddThemeConstantOverride("separation", 30);

            var background = CreateToolbarBackground();
            if (background != null)
                grid.AddChild(background);

            grid.AddChild(toolbar);

            if (view.EnableSortBar)
                InstallSortButtons(toolbar);

            if (view.EnableUpgradePreviewToggle)
                InstallUpgradeToggle(toolbar);
        }

        private Control? CreateToolbarBackground()
        {
            try
            {
                return new TextureRect
                {
                    Name = $"RitsuLibCardPileViewToolbarBg_{definition.Id}",
                    Texture = PreloadManager.Cache.GetAsset<Texture2D>(
                        view.ToolbarBackgroundTexturePath ?? DefaultSortBarTexturePath),
                    Material = ResolveToolbarBackgroundMaterial(),
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    AnchorLeft = 0.5f,
                    AnchorRight = 0.5f,
                    AnchorTop = 0f,
                    AnchorBottom = 0f,
                    OffsetLeft = -620f,
                    OffsetTop = 88f,
                    OffsetRight = 620f,
                    OffsetBottom = 156f,
                };
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardPiles] Could not load mod pile toolbar background for '{definition.Id}': {ex.Message}");
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

                toolbar.AddChild(button);
                SetSortButtonLabel(button, GetSortLabel(option));
                SetSortButtonHue(button);
                button.Connect(NClickableControl.SignalName.Released,
                    Callable.From<NButton>(_ => OnSortReleased(option, button)));
            }
        }

        private NCardViewSortButton? CreateSortButton(ModCardPileSortOption option)
        {
            try
            {
                var button = PreloadManager.Cache.GetScene(SortButtonScenePath)
                    .Instantiate<NCardViewSortButton>();
                button.Name = $"RitsuLibSort_{option}";
                button.CustomMinimumSize = new(220f, 42f);
                return button;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardPiles] Could not create sort button '{option}' for '{definition.Id}': {ex.Message}");
                return null;
            }
        }

        private static void SetSortButtonLabel(NCardViewSortButton button, string label)
        {
            if (button.IsNodeReady())
                button.SetLabel(label);
            else
                Callable.From(() => button.SetLabel(label)).CallDeferred();
        }

        private void SetSortButtonHue(NCardViewSortButton button)
        {
            var material = ResolveSortButtonHueMaterial();
            if (material == null)
                return;

            if (button.IsNodeReady())
                button.SetHue(material);
            else
                Callable.From(() => button.SetHue(material)).CallDeferred();
        }

        private void OnSortReleased(ModCardPileSortOption option, NCardViewSortButton button)
        {
            _sortingPriority.Remove(option.Ascending());
            _sortingPriority.Remove(option.Descending());
            _sortingPriority.Insert(0, button.IsDescending ? option.Descending() : option.Ascending());
            RefreshCards();
        }

        private void InstallUpgradeToggle(HBoxContainer toolbar)
        {
            try
            {
                var toggle = new NTickbox
                {
                    Name = "RitsuLibViewUpgrades",
                    CustomMinimumSize = new(250f, 64f),
                    FocusMode = Control.FocusModeEnum.All,
                    MouseFilter = Control.MouseFilterEnum.Stop,
                };

                var visuals = PreloadManager.Cache.GetScene(TickboxScenePath)
                    .Instantiate<Control>();
                visuals.UniqueNameInOwner = true;
                toggle.AddChild(visuals);

                var label = new MegaLabel
                {
                    Name = "ViewUpgradesLabel",
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    CustomMinimumSize = new(170f, 42f),
                    Position = new(58f, 10f),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                label.SetTextAutoSize(new LocString("gameplay_ui", "VIEW_UPGRADES").GetFormattedText());
                if (view.UpgradePreviewLabelColor is { } labelColor)
                    label.AddThemeColorOverride("font_color", labelColor);
                if (view.UpgradePreviewLabelOutlineColor is { } outlineColor)
                    label.AddThemeColorOverride("font_outline_color", outlineColor);
                toggle.AddChild(label);

                toolbar.AddChild(toggle);
                Callable.From(() => toggle.IsTicked = false).CallDeferred();

                toggle.Connect(NTickbox.SignalName.Toggled,
                    Callable.From<NTickbox>(tickbox => { grid.IsShowingUpgrades = tickbox.IsTicked; }));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardPiles] Could not create upgrade preview toggle for '{definition.Id}': {ex.Message}");
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
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardPiles] ToolbarBackgroundMaterialProvider for '{definition.Id}' threw: {ex.Message}");
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
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardPiles] SortButtonHueMaterialProvider for '{definition.Id}' threw: {ex.Message}");
                return view.SortButtonHueMaterial;
            }
        }
    }
}
