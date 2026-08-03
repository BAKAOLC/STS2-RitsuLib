using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds card-pool filter buttons for registered mod characters and shared pools to the card library
    ///         compendium. Characters hidden by
    ///         <see cref="IModCharacterVanillaSelectionPolicy.HideInCardLibraryCompendium" /> are skipped.
    ///     </para>
    ///     <para xml:lang="en">
    ///         Existing filter icons are synchronized with <see cref="CharacterModel.IconTexture" /> so that
    ///         replacements registered through
    ///         <see
    ///             cref="ModContentRegistry.RegisterCharacterAssetReplacement(string, Scaffolding.Characters.CharacterAssetProfile)" />
    ///         are reflected in the compendium. Character rows use
    ///         <see cref="CardLibraryCompendiumPlacementDefaults.DefaultCharacterRowRules" /> unless the character
    ///         implements <see cref="IModCharacterCardLibraryCompendiumPlacement" />. Shared-pool filters without
    ///         placement rules are appended to the end of the filter strip.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在卡牌总览中为已注册的模组角色和共享牌池添加牌池筛选按钮。由
    ///         <see cref="IModCharacterVanillaSelectionPolicy.HideInCardLibraryCompendium" />
    ///         隐藏的角色会被跳过。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将现有筛选按钮的图标与 <see cref="CharacterModel.IconTexture" /> 同步，使通过
    ///         <see
    ///             cref="ModContentRegistry.RegisterCharacterAssetReplacement(string, Scaffolding.Characters.CharacterAssetProfile)" />
    ///         注册的替换也能反映在卡牌总览中。除非角色实现了
    ///         <see cref="IModCharacterCardLibraryCompendiumPlacement" />，否则角色行使用
    ///         <see cref="CardLibraryCompendiumPlacementDefaults.DefaultCharacterRowRules" />。未指定放置规则的共享牌池
    ///         筛选按钮会追加到筛选栏末尾。
    ///     </para>
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CardLibraryCompendiumPatch : IPatchMethod
    {
        private const float DefaultFilterSize = 64f;
        private const float DefaultImageSize = 56f;
        private const float FilterGridHeightLimit = 192f;
        private const int DefaultColumnCount = 4;

        public static string PatchId => "card_library_compendium_mod_character_filter";

        public static string Description =>
            "Sync card library compendium pool-filter icons to CharacterModel.IconTexture; add mod character filter buttons";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardLibrary), nameof(NCardLibrary._Ready))];
        }

        public static void Postfix(
            NCardLibrary __instance,
            Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters,
            Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters)
        {
            SyncExistingFilterIcons(____cardPoolFilters);

            if (!TryGetCompendiumTemplateFilter(__instance, ____cardPoolFilters, out var referenceFilter) ||
                referenceFilter.GetParent() is not { } filterParent)
                return;

            var modCharacters = ModContentRegistry.GetModCharacters()
                .Where(character => !____cardPoolFilters.ContainsKey(character))
                .ToArray();
            var sharedPoolFilters = ModContentRegistry.GetCardLibraryCompendiumSharedPoolFilters();
            if (modCharacters.Length == 0 && sharedPoolFilters.Count == 0)
            {
                QueueFinalFilterLayout(filterParent);
                return;
            }

            ShaderMaterial? referenceMat = null;
            if (referenceFilter.GetNodeOrNull<Control>("Image") is { Material: ShaderMaterial refMat })
                referenceMat = refMat;
            var referenceIcon = TryGetReferenceFilterTexture(referenceFilter);

            var updateCallable = Callable.From<NCardPoolFilter>(__instance.UpdateCardPoolFilter);

            var planned = CardLibraryCompendiumPlacementResolver.BuildPlannedRows(
                modCharacters,
                sharedPoolFilters,
                RitsuLibFramework.Logger);
            if (planned.Count == 0)
            {
                QueueFinalFilterLayout(filterParent);
                return;
            }

            var strip = CardLibraryCompendiumStripSnapshot.Capture(filterParent);
            CardLibraryCompendiumPlacementResolver.AssignTargetsAndSort(
                __instance,
                filterParent,
                strip,
                planned,
                RitsuLibFramework.Logger);

            foreach (var row in planned)
                TryBuildFilter(row, referenceMat, referenceIcon, referenceFilter);

            CardLibraryCompendiumPlacementResolver.InsertRowsInOrder(filterParent, strip, planned);
            QueueFinalFilterLayout(filterParent);

            foreach (var row in planned)
            {
                if (row.BuiltFilter is not { } filter || row.ResolvedPool is not { } pool)
                    continue;

                TryRegisterFilter(
                    __instance,
                    row,
                    filter,
                    pool,
                    ____poolFilters,
                    ____cardPoolFilters,
                    updateCallable);
            }
        }

        private static void QueueFinalFilterLayout(Node filterParent)
        {
            Callable.From(() => ApplyFinalFilterLayout(filterParent)).CallDeferred();
        }

        private static void SyncExistingFilterIcons(
            Dictionary<CharacterModel, NCardPoolFilter> cardPoolFilters)
        {
            foreach (var (character, filter) in cardPoolFilters)
                try
                {
                    if (filter.GetNodeOrNull<TextureRect>("Image") is not { } image)
                        continue;

                    var texture = TryGetCharacterIconTexture(character);
                    if (texture is null)
                        continue;

                    image.Texture = texture;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[CardLibrary] Failed to sync compendium icon for {DescribeCharacter(character)}: {ex.Message}");
                }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Finds a pool-filter control whose Image <see cref="ShaderMaterial" /> can be cloned and whose icon
        ///         can serve as the fallback for shared-pool filters. If the base game has already created character
        ///         filters, the leftmost such filter is selected; otherwise, the first available vanilla filter in
        ///         <see cref="CardLibraryCompendiumVanillaFilterNames.AllInStripOrder" /> is used.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         查找一个牌池筛选控件，以便克隆其 Image <see cref="ShaderMaterial" />，并将其图标用作共享牌池
        ///         筛选按钮的回退图标。如果游戏本体已创建角色筛选按钮，则选择其中最靠左的一个；否则使用
        ///         <see cref="CardLibraryCompendiumVanillaFilterNames.AllInStripOrder" />
        ///         中第一个实际存在的原版筛选按钮。
        ///     </para>
        /// </summary>
        private static bool TryGetCompendiumTemplateFilter(
            NCardLibrary library,
            Dictionary<CharacterModel, NCardPoolFilter> cardPoolFilters,
            out NCardPoolFilter referenceFilter)
        {
            if (cardPoolFilters.Count > 0)
            {
                referenceFilter = GetLeftmostPoolFilterInStripModSubset(cardPoolFilters);
                return true;
            }

            foreach (var name in CardLibraryCompendiumVanillaFilterNames.AllInStripOrder)
                if (library.GetNodeOrNull<NCardPoolFilter>(name) is { } f)
                {
                    referenceFilter = f;
                    return true;
                }

            referenceFilter = null!;
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the leftmost <see cref="NCardPoolFilter" /> in the compendium filter strip that also appears
        ///         in <paramref name="cardPoolFilters" />. Falls back to <c>Values.First()</c> when the strip cannot be
        ///         inspected or contains none of those filters.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回卡牌总览筛选栏中同时存在于 <paramref name="cardPoolFilters" /> 的最左侧
        ///         <see cref="NCardPoolFilter" />。无法检查筛选栏或其中没有对应筛选按钮时，回退到
        ///         <c>Values.First()</c>。
        ///     </para>
        /// </summary>
        private static NCardPoolFilter GetLeftmostPoolFilterInStripModSubset(
            Dictionary<CharacterModel, NCardPoolFilter> cardPoolFilters)
        {
            var fallback = cardPoolFilters.Values.First();
            if (fallback.GetParent() is not { } strip)
                return fallback;

            for (var i = 0; i < strip.GetChildCount(); i++)
            {
                if (strip.GetChild(i) is not NCardPoolFilter f)
                    continue;
                if (!cardPoolFilters.ContainsValue(f))
                    continue;
                return f;
            }

            return fallback;
        }

        private static NCardPoolFilter CreateFilter(
            CharacterModel character,
            string? iconTexturePath,
            ShaderMaterial? referenceMat,
            Texture2D? fallbackIcon)
        {
            const float imagePos = (DefaultFilterSize - DefaultImageSize) / 2f;

            var filter = new NCardPoolFilter
            {
                Name = $"MOD_FILTER_{character.Id.Entry}",
                CustomMinimumSize = new(DefaultFilterSize, DefaultFilterSize),
                Size = new(DefaultFilterSize, DefaultFilterSize),
                FocusMode = Control.FocusModeEnum.All,
            };

            var mat = (ShaderMaterial?)referenceMat?.Duplicate();

            var image = new TextureRect
            {
                Name = "Image",
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Size = new(DefaultImageSize, DefaultImageSize),
                Position = new(imagePos, imagePos),
                Scale = new(0.9f, 0.9f),
                PivotOffset = Vector2.One * (DefaultImageSize / 2f),
                Material = mat ?? MaterialUtils.CreateHsvShaderMaterial(1, 1, 1),
                Texture = ResolveFilterIconTexture(character, iconTexturePath, fallbackIcon),
            };

            filter.AddChild(image);
            image.Owner = filter;

            var reticlePath = SceneHelper.GetScenePath("ui/selection_reticle");
            var reticle = PreloadManager.Cache.GetScene(reticlePath).Instantiate<NSelectionReticle>();
            ConfigureFilterReticle(reticle);
            filter.AddChild(reticle);
            reticle.Owner = filter;

            return filter;
        }

        private static Texture2D? ResolveFilterIconTexture(
            CharacterModel character,
            string? iconTexturePath,
            Texture2D? fallbackIcon)
        {
            if (TryLoadTexture(
                    iconTexturePath,
                    character,
                    nameof(IModCharacterAssetOverrides.CustomIconTexturePath),
                    $"character {DescribeCharacter(character)}") is { } iconTexture)
                return iconTexture;

            return TryGetCharacterIconTexture(character) ?? fallbackIcon;
        }

        private static NCardPoolFilter CreateSharedPoolFilter(
            CardLibraryCompendiumSharedPoolFilterRegistration registration,
            ShaderMaterial? referenceMat,
            Texture2D? fallbackIcon)
        {
            const float imagePos = (DefaultFilterSize - DefaultImageSize) / 2f;

            var filter = new NCardPoolFilter
            {
                Name = $"MOD_FILTER_SHARED_{registration.StableId}",
                CustomMinimumSize = new(DefaultFilterSize, DefaultFilterSize),
                Size = new(DefaultFilterSize, DefaultFilterSize),
                FocusMode = Control.FocusModeEnum.All,
            };

            var mat = (ShaderMaterial?)referenceMat?.Duplicate();

            var image = new TextureRect
            {
                Name = "Image",
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Size = new(DefaultImageSize, DefaultImageSize),
                Position = new(imagePos, imagePos),
                Scale = new(0.9f, 0.9f),
                PivotOffset = Vector2.One * (DefaultImageSize / 2f),
                Material = mat ?? MaterialUtils.CreateHsvShaderMaterial(1, 1, 1),
                Texture = ResolveSharedPoolFilterIcon(registration, fallbackIcon),
            };

            filter.AddChild(image);
            image.Owner = filter;

            var reticlePath = SceneHelper.GetScenePath("ui/selection_reticle");
            var reticle = PreloadManager.Cache.GetScene(reticlePath).Instantiate<NSelectionReticle>();
            ConfigureFilterReticle(reticle);
            filter.AddChild(reticle);
            reticle.Owner = filter;

            var id = ModContentRegistry.GetCompoundId(registration.OwningModId, "POOLFILTER", registration.StableId);
            if (LocManager.Instance is { } loc && loc.GetTable("card_library").HasEntry(id))
                filter.Loc = new("card_library", id);

            return filter;
        }

        private static void ApplyFinalFilterLayout(Node filterParent)
        {
            if (filterParent is not GridContainer grid)
                return;

            var filters = grid.GetChildren().OfType<NCardPoolFilter>().ToArray();
            if (filters.Length == 0)
                return;

            var visibleFilterCount = filters.Count(static filter => filter.Visible);
            if (visibleFilterCount == 0)
                return;
            var (columns, scale) = FindBestFilterLayout(
                visibleFilterCount,
                DefaultFilterSize * DefaultColumnCount);

            var filterSize = Vector2.One * (DefaultFilterSize * scale);
            var imageSize = Vector2.One * (DefaultImageSize * scale);
            foreach (var filter in filters)
            {
                filter.CustomMinimumSize = filterSize;
                filter.Size = filterSize;

                if (filter.GetNodeOrNull<Control>("Image") is { } image)
                {
                    image.CustomMinimumSize = imageSize;
                    image.Size = imageSize;
                    image.PivotOffset = imageSize / 2f;
                    image.Position = (filterSize - imageSize) / 2f;

                    if (image.GetNodeOrNull<Control>("Shadow") is { } shadow)
                    {
                        shadow.Size = imageSize;
                        shadow.PivotOffset = imageSize / 2f;
                    }
                }

                if (filter.GetNodeOrNull<NSelectionReticle>("%SelectionReticle") is { } reticle)
                    ConfigureFilterReticle(reticle);
            }

            grid.Columns = columns;
        }

        private static (int columns, float scale) FindBestFilterLayout(int filterCount, float widthLimit)
        {
            var bestColumns = DefaultColumnCount;
            var bestScale = 0f;
            for (var columns = DefaultColumnCount; columns <= Math.Max(DefaultColumnCount, filterCount); columns++)
            {
                var rows = Math.Max(1, Mathf.CeilToInt(filterCount / (float)columns));
                var widthScale = widthLimit / (DefaultFilterSize * columns);
                var heightScale = FilterGridHeightLimit / (DefaultFilterSize * rows);
                var scale = MathF.Min(1f, MathF.Min(widthScale, heightScale));
                if (scale <= bestScale)
                    continue;

                bestColumns = columns;
                bestScale = scale;
            }

            return (bestColumns, bestScale);
        }

        private static void ConfigureFilterReticle(NSelectionReticle reticle)
        {
            reticle.Name = "SelectionReticle";
            reticle.UniqueNameInOwner = true;
            reticle.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            reticle.PivotOffset = reticle.Size / 2.0f;
            reticle.ZIndex = 10;
        }

        private static Texture2D? ResolveSharedPoolFilterIcon(
            CardLibraryCompendiumSharedPoolFilterRegistration registration,
            Texture2D? fallbackIcon)
        {
            var path = registration.IconTexturePath;
            if (TryLoadTexture(
                    path,
                    registration,
                    nameof(CardLibraryCompendiumSharedPoolFilterRegistration.IconTexturePath),
                    $"shared filter '{registration.StableId}'") is { } iconTexture)
                return iconTexture;

            return fallbackIcon;
        }

        private static void TryBuildFilter(
            CardLibraryCompendiumPlacementResolver.PlannedRow row,
            ShaderMaterial? referenceMat,
            Texture2D? referenceIcon,
            NCardPoolFilter referenceFilter)
        {
            try
            {
                if (row.Character is { } ch)
                {
                    string? iconTexturePath = null;
                    if (ch is IModCharacterAssetOverrides assetOverrides)
                        iconTexturePath = assetOverrides.CustomIconTexturePath;

                    row.BuiltFilter = CreateFilter(ch, iconTexturePath, referenceMat, referenceIcon);
                }
                else if (row.Shared is { } reg)
                {
                    row.BuiltFilter = CreateSharedPoolFilter(
                        reg,
                        referenceMat,
                        referenceIcon ?? TryGetReferenceFilterTexture(referenceFilter));
                }
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardLibrary] Skipping compendium filter '{row.StableKey}': failed to create button. {ex.Message}");
                row.BuiltFilter = null;
            }
        }

        private static void TryRegisterFilter(
            NCardLibrary library,
            CardLibraryCompendiumPlacementResolver.PlannedRow row,
            NCardPoolFilter filter,
            CardPoolModel pool,
            Dictionary<NCardPoolFilter, Func<CardModel, bool>> poolFilters,
            Dictionary<CharacterModel, NCardPoolFilter> cardPoolFilters,
            Callable updateCallable)
        {
            try
            {
                if (!poolFilters.TryAdd(filter, c => pool.AllCardIds.Contains(c.Id)))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[CardLibrary] Skipping duplicate compendium pool-filter registration for '{row.StableKey}'.");
                    return;
                }

                if (row.Character is { } ch && !cardPoolFilters.TryAdd(ch, filter))
                    RitsuLibFramework.Logger.Warn(
                        $"[CardLibrary] Character compendium filter already exists for {DescribeCharacter(ch)}.");

                filter.Connect(NCardPoolFilter.SignalName.Toggled, updateCallable);
                filter.Connect(Control.SignalName.FocusEntered,
                    Callable.From(delegate { library._lastHoveredControl = filter; }));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardLibrary] Failed to register compendium filter '{row.StableKey}': {ex.Message}");
            }
        }

        private static Texture2D? TryGetReferenceFilterTexture(NCardPoolFilter referenceFilter)
        {
            try
            {
                return referenceFilter.GetNodeOrNull<TextureRect>("Image") is { Texture: { } refTexture }
                    ? refTexture
                    : null;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardLibrary] Failed to inspect reference compendium filter icon: {ex.Message}");
                return null;
            }
        }

        private static Texture2D? TryGetCharacterIconTexture(CharacterModel character)
        {
            try
            {
                return character.IconTexture;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardLibrary] Failed to load compendium icon for {DescribeCharacter(character)}: {ex.Message}");
                return null;
            }
        }

        private static Texture2D? TryLoadTexture(
            string? path,
            object owner,
            string memberName,
            string ownerLabel)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                if (!AssetPathDiagnostics.Exists(path, owner, memberName))
                    return null;

                if (GodotResourcePath.TryLoad<Texture2D>(path, out var iconTexture))
                    return iconTexture;

                RitsuLibFramework.Logger.Warn(
                    $"[CardLibrary] Could not load Texture2D for {ownerLabel}.{memberName}: '{path}'.");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardLibrary] Failed to load Texture2D for {ownerLabel}.{memberName}: '{path}'. {ex.Message}");
            }

            return null;
        }

        private static string DescribeCharacter(CharacterModel character)
        {
            try
            {
                return character.Id.ToString();
            }
            catch
            {
                return character.GetType().Name;
            }
        }
    }
}
