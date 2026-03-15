using MegaCrit.Sts2.Core.Helpers;

namespace STS2RitsuLib.Scaffolding.Characters
{
    public static class CharacterAssetPathHelper
    {
        public static string GetEnergyIconPath(string energyColorName)
        {
            return EnergyIconHelper.GetPath(energyColorName);
        }

        public static string GetEnergyCounterPath(string characterEntry)
        {
            return SceneHelper.GetScenePath($"combat/energy_counters/{Normalize(characterEntry)}_energy_counter");
        }

        public static string GetVisualsPath(string characterEntry)
        {
            return SceneHelper.GetScenePath($"creature_visuals/{Normalize(characterEntry)}");
        }

        public static string GetCharacterSelectBackgroundPath(string characterEntry)
        {
            return SceneHelper.GetScenePath($"screens/char_select/char_select_bg_{Normalize(characterEntry)}");
        }

        public static string GetCharacterSelectIconPath(string characterEntry)
        {
            return ImageHelper.GetImagePath($"packed/character_select/char_select_{Normalize(characterEntry)}.png");
        }

        public static string GetCharacterSelectLockedIconPath(string characterEntry)
        {
            return ImageHelper.GetImagePath(
                $"packed/character_select/char_select_{Normalize(characterEntry)}_locked.png");
        }

        public static string GetMapMarkerPath(string characterEntry)
        {
            return ImageHelper.GetImagePath($"packed/map/icons/map_marker_{Normalize(characterEntry)}.png");
        }

        public static string GetTrailPath(string characterEntry)
        {
            return SceneHelper.GetScenePath($"vfx/card_trail_{Normalize(characterEntry)}");
        }

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
