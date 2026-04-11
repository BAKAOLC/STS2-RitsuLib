using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Per-owner character relic visuals; applied before <see cref="IModRelicAssetOverrides" /> in
    ///     <see cref="RelicIconPathPatch" /> / <see cref="RelicTexturePatch" />.
    /// </summary>
    internal static class ModCharacterOwnedRelicVisualOverrideHelper
    {
        internal static bool TryRelicIconPath(RelicModel instance, ref string result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            if (overrides == null)
                return true;

            var profile = overrides.TryGetVanillaRelicVisualOverrideForOwnedRelic(instance);
            if (profile == null)
                return true;

            var path = profile.IconPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(RelicAssetProfile.IconPath)))
                return true;

            result = path;
            return false;
        }

        internal static bool TryRelicIconTexture(RelicModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            if (overrides == null)
                return true;

            var profile = overrides.TryGetVanillaRelicVisualOverrideForOwnedRelic(instance);
            if (profile == null)
                return true;

            var path = profile.IconPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(RelicAssetProfile.IconPath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryRelicIconOutlineTexture(RelicModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            if (overrides == null)
                return true;

            var profile = overrides.TryGetVanillaRelicVisualOverrideForOwnedRelic(instance);
            if (profile == null)
                return true;

            var path = profile.IconOutlinePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(RelicAssetProfile.IconOutlinePath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryRelicBigIconTexture(RelicModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            if (overrides == null)
                return true;

            var profile = overrides.TryGetVanillaRelicVisualOverrideForOwnedRelic(instance);
            if (profile == null)
                return true;

            var path = profile.BigIconPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(RelicAssetProfile.BigIconPath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        private static IModCharacterAssetOverrides? TryGetOwningCharacterOverrides(RelicModel instance)
        {
            return instance.Owner?.Character as IModCharacterAssetOverrides;
        }
    }
}
