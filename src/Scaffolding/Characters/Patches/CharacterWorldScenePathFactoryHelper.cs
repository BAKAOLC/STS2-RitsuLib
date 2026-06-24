using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;
using STS2RitsuLib.Scaffolding.Godot;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    internal static class CharacterWorldScenePathFactoryHelper
    {
        internal static TNode CreateFromSceneOrTexture<TNode>(
            CharacterModel character,
            string path,
            string memberName,
            PackedScene.GenEditState editState)
            where TNode : Node, new()
        {
            Exception? lastFailure = null;
            foreach (var candidate in EnumerateCandidatePaths(character, path, memberName))
                try
                {
                    if (TryCreate(candidate, editState, out TNode created))
                    {
                        if (!string.Equals(candidate, path, StringComparison.Ordinal))
                            RitsuLibFramework.Logger.Warn(
                                $"[Godot] Falling back to character world asset '{candidate}' for {DescribeCharacter(character)}.{memberName}.");

                        return created;
                    }

                    RitsuLibFramework.Logger.Warn(
                        $"[Godot] Character world asset '{candidate}' for {DescribeCharacter(character)}.{memberName} did not resolve as {nameof(PackedScene)} or {nameof(Texture2D)}.");
                }
                catch (Exception ex)
                {
                    lastFailure = ex;
                    RitsuLibFramework.Logger.Warn(
                        $"[Godot] Failed to auto-convert {DescribeCharacter(character)}.{memberName} '{candidate}' to {typeof(TNode).Name}: {ex.Message}.");
                }

            throw new InvalidOperationException(
                $"Could not create {typeof(TNode).Name} for {DescribeCharacter(character)}.{memberName} from '{path}'.",
                lastFailure);
        }

        private static bool TryCreate<TNode>(
            string path,
            PackedScene.GenEditState editState,
            out TNode created)
            where TNode : Node, new()
        {
            var scene = ContentAssetOverridePatchHelper.ResolveScene(path);
            if (scene != null)
            {
                created = RitsuGodotNodeFactories.CreateFromScene<TNode>(scene, editState);
                return true;
            }

            var texture = ContentAssetOverridePatchHelper.ResolveTexture2D(path);
            if (texture != null)
            {
                created = RitsuGodotNodeFactories.CreateFromResource<TNode>(texture);
                return true;
            }

            created = null!;
            return false;
        }

        private static IEnumerable<string> EnumerateCandidatePaths(
            CharacterModel character,
            string path,
            string memberName)
        {
            if (!string.IsNullOrWhiteSpace(path))
                yield return path;

            var entry = character.Id.Entry;
            if (!string.IsNullOrWhiteSpace(entry))
            {
                var profilePath = SelectPath(CharacterAssetProfiles.FromCharacterId(entry), memberName);
                if (!string.IsNullOrWhiteSpace(profilePath) &&
                    !string.Equals(profilePath, path, StringComparison.Ordinal))
                    yield return profilePath;
            }

            if (string.Equals(
                    entry,
                    CharacterAssetProfiles.DefaultPlaceholderCharacterId,
                    StringComparison.OrdinalIgnoreCase))
                yield break;

            var placeholderPath = SelectPath(
                CharacterAssetProfiles.FromCharacterId(CharacterAssetProfiles.DefaultPlaceholderCharacterId),
                memberName);
            if (!string.IsNullOrWhiteSpace(placeholderPath) &&
                !string.Equals(placeholderPath, path, StringComparison.Ordinal))
                yield return placeholderPath;
        }

        private static string? SelectPath(CharacterAssetProfile profile, string memberName)
        {
            return memberName switch
            {
                nameof(IModCharacterAssetOverrides.CustomMerchantAnimPath) => profile.Scenes?.MerchantAnimPath,
                nameof(IModCharacterAssetOverrides.CustomRestSiteAnimPath) => profile.Scenes?.RestSiteAnimPath,
                _ => null,
            };
        }

        private static string DescribeCharacter(CharacterModel character)
        {
            try
            {
                return $"{character.GetType().Name}<{character.Id.Entry}>";
            }
            catch
            {
                return character.GetType().Name;
            }
        }
    }
}
