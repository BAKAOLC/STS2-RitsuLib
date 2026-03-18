using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Adds a pool-filter button for each registered mod character in the card library compendium.
    ///     Without this patch, mod character cards are not visible in any filter category, and opening
    ///     the card library during a run with a mod character causes a KeyNotFoundException crash.
    /// </summary>
    public class CardLibraryCompendiumPatch : IPatchMethod
    {
        public static string PatchId => "card_library_compendium_mod_character_filter";

        public static string Description =>
            "Add mod character pool filter buttons to the card library compendium";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardLibrary), nameof(NCardLibrary._Ready))];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(
                NCardLibrary __instance,
                Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters,
                Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters)
            // ReSharper restore InconsistentNaming
        {
            var modCharacters = ModContentRegistry.GetModCharacters().ToArray();
            if (modCharacters.Length == 0) return;
            if (____cardPoolFilters.Count == 0) return;

            var referenceFilter = ____cardPoolFilters.Values.First();
            var filterParent = referenceFilter.GetParent();
            if (filterParent == null) return;

            ShaderMaterial? referenceMat = null;
            if (referenceFilter.GetNodeOrNull<Control>("Image") is { } refImage &&
                refImage.Material is ShaderMaterial refMat)
                referenceMat = refMat;

            var updateMethod = AccessTools.Method(typeof(NCardLibrary), "UpdateCardPoolFilter");
            var updateCallable = Callable.From<NCardPoolFilter>(f => updateMethod.Invoke(__instance, [f]));
            var lastHoveredField = AccessTools.Field(typeof(NCardLibrary), "_lastHoveredControl");

            foreach (var character in modCharacters)
            {
                string? iconTexturePath = null;
                if (character is IModCharacterAssetOverrides assetOverrides)
                    iconTexturePath = assetOverrides.CustomIconTexturePath;

                var filter = CreateFilter(character, iconTexturePath, referenceMat);
                filterParent.AddChild(filter, true);

                var pool = character.CardPool;
                ____poolFilters.Add(filter, c => pool.AllCardIds.Contains(c.Id));
                ____cardPoolFilters.Add(character, filter);

                filter.Connect(NCardPoolFilter.SignalName.Toggled, updateCallable);
                filter.Connect(Control.SignalName.FocusEntered,
                    Callable.From(delegate { lastHoveredField.SetValue(__instance, filter); }));
            }
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

            if (mat != null)
                image.Material = mat;

            if (!string.IsNullOrWhiteSpace(iconTexturePath) && ResourceLoader.Exists(iconTexturePath))
                image.Texture = ResourceLoader.Load<Texture2D>(iconTexturePath);

            filter.AddChild(image);

            var reticlePath = SceneHelper.GetScenePath("ui/selection_reticle");
            if (!ResourceLoader.Exists(reticlePath)) return filter;
            var reticle = ResourceLoader.Load<PackedScene>(reticlePath).Instantiate<NSelectionReticle>();
            reticle.Name = "SelectionReticle";
            reticle.UniqueNameInOwner = true;
            filter.AddChild(reticle);

            return filter;
        }
    }
}
