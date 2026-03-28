using MegaCrit.Sts2.Core.Helpers;

namespace STS2RitsuLib.Scaffolding.Characters
{
    /// <summary>
    ///     Resolves vanilla-style resource paths for character UI, combat, and VFX from a character entry id.
    /// </summary>
    public static class CharacterAssetPathHelper
    {
        /// <summary>
        ///     Energy icon path for <paramref name="energyColorName" /> via <see cref="EnergyIconHelper" />.
        /// </summary>
        public static string GetEnergyIconPath(string energyColorName)
        {
            return EnergyIconHelper.GetPath(energyColorName);
        }

        /// <summary>
        ///     Scene path for the combat energy counter widget.
        /// </summary>
        public static string GetEnergyCounterPath(string characterEntry)
        {
            return SceneHelper.GetScenePath($"combat/energy_counters/{Normalize(characterEntry)}_energy_counter");
        }

        /// <summary>
        ///     Scene path for in-combat creature visuals.
        /// </summary>
        public static string GetVisualsPath(string characterEntry)
        {
            return SceneHelper.GetScenePath($"creature_visuals/{Normalize(characterEntry)}");
        }

        /// <summary>
        ///     Character select background scene path.
        /// </summary>
        public static string GetCharacterSelectBackgroundPath(string characterEntry)
        {
            return SceneHelper.GetScenePath($"screens/char_select/char_select_bg_{Normalize(characterEntry)}");
        }

        /// <summary>
        ///     Unlocked character select portrait texture path.
        /// </summary>
        public static string GetCharacterSelectIconPath(string characterEntry)
        {
            return ImageHelper.GetImagePath($"packed/character_select/char_select_{Normalize(characterEntry)}.png");
        }

        /// <summary>
        ///     Locked character select portrait texture path.
        /// </summary>
        public static string GetCharacterSelectLockedIconPath(string characterEntry)
        {
            return ImageHelper.GetImagePath(
                $"packed/character_select/char_select_{Normalize(characterEntry)}_locked.png");
        }

        /// <summary>
        ///     Run map marker icon path for the character.
        /// </summary>
        public static string GetMapMarkerPath(string characterEntry)
        {
            return ImageHelper.GetImagePath($"packed/map/icons/map_marker_{Normalize(characterEntry)}.png");
        }

        /// <summary>
        ///     Card trail VFX scene path.
        /// </summary>
        public static string GetTrailPath(string characterEntry)
        {
            return SceneHelper.GetScenePath($"vfx/card_trail_{Normalize(characterEntry)}");
        }

        /// <summary>
        ///     Default asset paths used when validating or copying a vanilla-style character layout.
        /// </summary>
        public static IEnumerable<string> EnumerateDefaultCharacterAssets(string characterEntry)
        {
            yield return GetVisualsPath(characterEntry);
            yield return GetCharacterSelectBackgroundPath(characterEntry);
            yield return GetCharacterSelectIconPath(characterEntry);
            yield return GetCharacterSelectLockedIconPath(characterEntry);
            yield return GetMapMarkerPath(characterEntry);
            yield return GetTrailPath(characterEntry);
            yield return GetEnergyCounterPath(characterEntry);
        }

        private static string Normalize(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            return value.Trim().ToLowerInvariant();
        }
    }
}
