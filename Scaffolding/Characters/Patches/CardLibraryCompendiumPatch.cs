using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
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
    ///     icons match everywhere).
    ///     Without this patch, mod character cards are not visible in any filter category, and opening
    ///     the card library during a run with a mod character causes a KeyNotFoundException crash.
    ///     Mod-character rows use <see cref="CardLibraryCompendiumPlacementDefaults.DefaultCharacterRowRules" /> unless
    ///     overridden via <see cref="IModCharacterCardLibraryCompendiumPlacement" /> (or the template virtual). Optional
    ///     shared-pool filters use end-of-strip placement when no rules are supplied. All rows share one placement pass
    ///     (vanilla anchor priority list, mod-to-mod constraint relaxation, unified sort, then insertion).
    /// </summary>
    public class CardLibraryCompendiumPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "card_library_compendium_mod_character_filter";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Sync card library compendium pool-filter icons to CharacterModel.IconTexture; add mod character filter buttons";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardLibrary), nameof(NCardLibrary._Ready))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Clones vanilla pool-filter UI for each mod character and wires pool predicates so compendium filtering
        ///     works without <c>KeyNotFoundException</c>.
        /// </summary>
        public static void Postfix(
                NCardLibrary __instance,
                Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters,
                Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters)
            // ReSharper restore InconsistentNaming
        {
            foreach (var (character, filter) in ____cardPoolFilters)
            {
                if (filter.GetNodeOrNull<TextureRect>("Image") is not { } image)
                    continue;

                var texture = character.IconTexture;
                if (texture is null)
                    continue;

                image.Texture = texture;
            }

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
                if (row.Character is { } ch)
                {
                    string? iconTexturePath = null;
                    if (ch is IModCharacterAssetOverrides assetOverrides)
                        iconTexturePath = assetOverrides.CustomIconTexturePath;

                    row.BuiltFilter = CreateFilter(ch, iconTexturePath, referenceMat);
                }
                else if (row.Shared is { } reg)
                {
                    row.BuiltFilter = CreateSharedPoolFilter(reg, referenceMat, referenceFilter);
                }

            CardLibraryCompendiumPlacementResolver.InsertRowsInOrder(filterParent, strip, planned);

            foreach (var row in planned)
            {
                if (row.BuiltFilter is not { } filter || row.ResolvedPool is not { } pool)
                    continue;

                ____poolFilters.Add(filter, c => pool.AllCardIds.Contains(c.Id));
                if (row.Character is { } ch)
                    ____cardPoolFilters.Add(ch, filter);

                filter.Connect(NCardPoolFilter.SignalName.Toggled, updateCallable);
                filter.Connect(Control.SignalName.FocusEntered,
                    Callable.From(delegate { __instance._lastHoveredControl = filter; }));
            }
        }

        /// <summary>
        ///     A pool-filter control to clone the Image <see cref="ShaderMaterial" /> from, and the fallback icon
        ///     source for shared compendium rows. When the base game has already created mod character filters,
        ///     the leftmost of those in the pool strip; otherwise the first present vanilla
        ///     <see cref="NCardPoolFilter" /> (strip order) from
        ///     <see cref="CardLibraryCompendiumVanillaFilterNames.AllInStripOrder" />.
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
            ShaderMaterial? referenceMat)
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
            image.Texture = ResolveFilterIconTexture(character, iconTexturePath);

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

        private static Texture2D ResolveFilterIconTexture(CharacterModel character, string? iconTexturePath)
        {
            if (!string.IsNullOrWhiteSpace(iconTexturePath) &&
                AssetPathDiagnostics.Exists(iconTexturePath, character,
                    nameof(IModCharacterAssetOverrides.CustomIconTexturePath)) &&
                ResourceLoader.Load<Texture2D>(iconTexturePath) is { } iconTexture)
                return iconTexture;

            return character.IconTexture;
        }

        private static NCardPoolFilter CreateSharedPoolFilter(
            CardLibraryCompendiumSharedPoolFilterRegistration registration,
            ShaderMaterial? referenceMat,
            NCardPoolFilter referenceFilter)
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
            image.Texture = ResolveSharedPoolFilterIcon(registration, referenceFilter);

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

        private static Texture2D ResolveSharedPoolFilterIcon(
            CardLibraryCompendiumSharedPoolFilterRegistration registration,
            NCardPoolFilter referenceFilter)
        {
            var path = registration.IconTexturePath;
            if (!string.IsNullOrWhiteSpace(path) &&
                AssetPathDiagnostics.Exists(path, registration,
                    nameof(CardLibraryCompendiumSharedPoolFilterRegistration.IconTexturePath)) &&
                ResourceLoader.Load<Texture2D>(path) is { } iconTexture)
                return iconTexture;

            if (referenceFilter.GetNodeOrNull<TextureRect>("Image") is { Texture: { } refTexture })
                return refTexture;

            throw new InvalidOperationException(
                "Card library compendium shared pool filter could not resolve an icon texture and the reference filter has no Texture2D.");
        }
    }
}
