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
    ///     Adds a pool-filter button for each registered mod character in the card library compendium (skips
    ///     characters with <see cref="IModCharacterVanillaSelectionPolicy.HideInCardLibraryCompendium" />), and
    ///     re-applies pool-filter art from <see cref="CharacterModel.IconTexture" /> (so
    ///     <see
    ///         cref="ModContentRegistry.RegisterCharacterAssetReplacement(string, Scaffolding.Characters.CharacterAssetProfile)" />
    ///     cref="ModContentRegistry.RegisterCharacterAssetReplacement(string, Scaffolding.Characters.CharacterAssetProfile)"
    ///     />
    ///     icons match everywhere).
    ///     Icons match everywhere).
    ///     Without this patch, mod character cards are not visible in any filter category, and opening
    ///     the card library during a run with a mod character causes a KeyNotFoundException crash.
    ///     Mod-character rows use <see cref="CardLibraryCompendiumPlacementDefaults.DefaultCharacterRowRules" /> unless
    ///     overridden via <see cref="IModCharacterCardLibraryCompendiumPlacement" /> (or the template virtual). Optional
    ///     shared-pool filters use end-of-strip placement when no rules are supplied. All rows share one placement pass
    ///     (vanilla anchor priority list, mod-to-mod constraint relaxation, unified sort, then insertion).
    ///     为卡牌库 compendium 中每个已注册 mod 角色添加牌池过滤按钮（跳过
    ///     带 <see cref="IModCharacterVanillaSelectionPolicy.HideInCardLibraryCompendium" /> 的角色），并
    ///     从 <see cref="CharacterModel.IconTexture" /> 重新应用牌池过滤美术（因此
    ///     <see />
    ///     图标在各处保持一致）。
    ///     图标在各处保持一致）。
    ///     没有此 patch 时，mod 角色卡牌不会在任何过滤类别中可见，并且在使用 mod 角色的跑局中打开
    ///     卡牌库会导致 KeyNotFoundException 崩溃。
    ///     mod 角色行使用 <see cref="CardLibraryCompendiumPlacementDefaults.DefaultCharacterRowRules" />，除非
    ///     通过 <see cref="IModCharacterCardLibraryCompendiumPlacement" />（或模板 virtual）覆盖。没有提供规则时，可选
    ///     共享池过滤器使用条带末尾放置。所有行共享一次放置流程
    ///     （原版锚点优先级列表、mod 到 mod 约束放宽、统一排序，然后插入）。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CardLibraryCompendiumPatch : IPatchMethod
    {
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

            var modCharacters = ModContentRegistry.GetModCharacters().ToArray();
            var sharedPoolFilters = ModContentRegistry.GetCardLibraryCompendiumSharedPoolFilters();
            if (modCharacters.Length == 0 && sharedPoolFilters.Count == 0)
                return;

            if (!TryGetCompendiumTemplateFilter(__instance, ____cardPoolFilters, out var referenceFilter) ||
                referenceFilter.GetParent() is not { } filterParent)
                return;

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
                return;

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
        ///     A pool-filter control to clone the Image <see cref="ShaderMaterial" /> from, and the fallback icon
        ///     source for shared compendium rows. When the base game has already created mod character filters,
        ///     the leftmost of those in the pool strip; otherwise the first present vanilla
        ///     <see cref="NCardPoolFilter" /> (strip order) from
        ///     <see cref="CardLibraryCompendiumVanillaFilterNames.AllInStripOrder" />.
        ///     用于克隆 Image <see cref="ShaderMaterial" /> 的牌池过滤控件，以及共享 compendium 行的 fallback 图标
        ///     来源。当基础游戏已经创建 mod 角色过滤器时，使用
        ///     牌池条带中这些过滤器最左侧的一个；否则使用
        ///     <see cref="CardLibraryCompendiumVanillaFilterNames.AllInStripOrder" /> 中第一个存在的原版
        ///     <see cref="NCardPoolFilter" />（条带顺序）。
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
        ///     Leftmost <see cref="NCardPoolFilter" /> under the compendium pool strip that is in
        ///     <paramref name="cardPoolFilters" />, for a stable clone source; otherwise
        ///     <c>Values.First()</c>.
        ///     compendium 牌池条带下、存在于
        ///     <paramref name="cardPoolFilters" /> 中的最左侧 <see cref="NCardPoolFilter" />，用作稳定克隆来源；否则使用
        ///     <c>Values.First()</c>。
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
            const float size = 64f;
            const float imageSize = 56f;
            const float imagePos = 4f;

            var filter = new NCardPoolFilter
            {
                Name = $"MOD_FILTER_{character.Id.Entry}",
                CustomMinimumSize = new(size, size),
                Size = new(size, size),
            };

            var mat = (ShaderMaterial?)referenceMat?.Duplicate();

            var image = new TextureRect
            {
                Name = "Image",
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Size = new(imageSize, imageSize),
                Position = new(imagePos, imagePos),
                Scale = new(0.9f, 0.9f),
                PivotOffset = new(28f, 28f),
            };

            image.Material = mat ?? MaterialUtils.CreateHsvShaderMaterial(1, 1, 1);
            image.Texture = ResolveFilterIconTexture(character, iconTexturePath, fallbackIcon);

            filter.AddChild(image);
            image.Owner = filter;

            var reticlePath = SceneHelper.GetScenePath("ui/selection_reticle");
            var reticle = PreloadManager.Cache.GetScene(reticlePath).Instantiate<NSelectionReticle>();
            reticle.Name = "SelectionReticle";
            reticle.UniqueNameInOwner = true;
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
            const float size = 64f;
            const float imageSize = 56f;
            const float imagePos = 4f;

            var filter = new NCardPoolFilter
            {
                Name = $"MOD_FILTER_SHARED_{registration.StableId}",
                CustomMinimumSize = new(size, size),
                Size = new(size, size),
            };

            var mat = (ShaderMaterial?)referenceMat?.Duplicate();

            var image = new TextureRect
            {
                Name = "Image",
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Size = new(imageSize, imageSize),
                Position = new(imagePos, imagePos),
                Scale = new(0.9f, 0.9f),
                PivotOffset = new(28f, 28f),
            };

            image.Material = mat ?? MaterialUtils.CreateHsvShaderMaterial(1, 1, 1);
            image.Texture = ResolveSharedPoolFilterIcon(registration, fallbackIcon);

            filter.AddChild(image);
            image.Owner = filter;

            var reticlePath = SceneHelper.GetScenePath("ui/selection_reticle");
            var reticle = PreloadManager.Cache.GetScene(reticlePath).Instantiate<NSelectionReticle>();
            reticle.Name = "SelectionReticle";
            reticle.UniqueNameInOwner = true;
            filter.AddChild(reticle);
            reticle.Owner = filter;

            var id = ModContentRegistry.GetCompoundId(registration.OwningModId, "POOLFILTER", registration.StableId);
            if (LocManager.Instance is { } loc && loc.GetTable("card_library").HasEntry(id))
                filter.Loc = new("card_library", id);

            return filter;
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
