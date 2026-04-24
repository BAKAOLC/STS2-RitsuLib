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
    ///     Adds a pool-filter button for each registered mod character in the card library compendium, and
    ///     re-applies pool-filter art from <see cref="CharacterModel.IconTexture" /> (so
    ///     <see
    ///         cref="ModContentRegistry.RegisterCharacterAssetReplacement(string, Scaffolding.Characters.CharacterAssetProfile)" />
    ///     icons match everywhere).
    ///     Without this patch, mod character cards are not visible in any filter category, and opening
    ///     the card library during a run with a mod character causes a KeyNotFoundException crash.
    ///     Buttons are inserted before the colorless pool filter when possible (then ancients, misc),
    ///     so they stay with playable-character filters rather than after misc/token-style pools.
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
            if (____cardPoolFilters.Count == 0)
                return;

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
            if (modCharacters.Length == 0)
                return;

            var referenceFilter = ____cardPoolFilters.Values.First();
            var filterParent = referenceFilter.GetParent();
            if (filterParent == null) return;

            var useOrderedInsert = TryGetModFilterInsertIndex(__instance, filterParent, out var insertIndex);

            ShaderMaterial? referenceMat = null;
            if (referenceFilter.GetNodeOrNull<Control>("Image") is { Material: ShaderMaterial refMat })
                referenceMat = refMat;

            var updateCallable = Callable.From<NCardPoolFilter>(__instance.UpdateCardPoolFilter);

            var nextIndex = insertIndex;
            foreach (var character in modCharacters)
            {
                string? iconTexturePath = null;
                if (character is IModCharacterAssetOverrides assetOverrides)
                    iconTexturePath = assetOverrides.CustomIconTexturePath;

                var filter = CreateFilter(character, iconTexturePath, referenceMat);
                filterParent.AddChild(filter, true);
                if (useOrderedInsert)
                {
                    filterParent.MoveChild(filter, nextIndex);
                    nextIndex++;
                }

                var pool = character.CardPool;
                ____poolFilters.Add(filter, c => pool.AllCardIds.Contains(c.Id));
                ____cardPoolFilters.Add(character, filter);

                filter.Connect(NCardPoolFilter.SignalName.Toggled, updateCallable);
                filter.Connect(Control.SignalName.FocusEntered,
                    Callable.From(delegate { __instance._lastHoveredControl = filter; }));
            }
        }

        /// <summary>
        ///     Prefer inserting mod character filters immediately before non-character pool toggles: colorless, then
        ///     ancients, then misc (vanilla has no separate token node; those pools follow). Falls back when no anchor
        ///     resolves under <paramref name="expectedParent" />.
        /// </summary>
        private static bool TryGetModFilterInsertIndex(
            NCardLibrary library,
            Node expectedParent,
            out int insertIndex)
        {
            ReadOnlySpan<string> anchorNames =
            [
                "%ColorlessPool",
                "%AncientsPool",
                "%MiscPool",
            ];

            foreach (var name in anchorNames)
            {
                if (library.GetNodeOrNull<NCardPoolFilter>(name) is not { } anchor)
                    continue;
                if (anchor.GetParent() != expectedParent)
                    continue;

                insertIndex = anchor.GetIndex();
                return true;
            }

            insertIndex = 0;
            return false;
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
    }
}
