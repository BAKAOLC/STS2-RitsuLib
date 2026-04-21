using System.Diagnostics.CodeAnalysis;
using Godot;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Public helpers for Godot project paths: <c>res://</c>, <c>user://</c>, <c>uid://</c> remapping and
    ///     resource presence checks aligned with <see cref="ResourceLoader" /> and <see cref="ResourceUid" />.
    /// </summary>
    public static class GodotResourcePath
    {
        private static readonly string[] ResourceExistenceTypeHints =
        [
            nameof(Texture2D),
            nameof(CompressedTexture2D),
            nameof(Image),
            nameof(PackedScene),
            nameof(Material),
            nameof(Resource),
        ];

        /// <summary>
        ///     Yields paths the engine may use for the same logical asset: the trimmed input, <c>uid://</c> →
        ///     <c>res://</c> (when applicable), and <see cref="ResourceUid.EnsurePath" /> alternatives.
        /// </summary>
        public static IEnumerable<string> EnumerateCandidatePaths(string? rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                yield break;

            foreach (var candidate in EnumerateEnginePathCandidates(rawPath.Trim()))
                yield return candidate;
        }

        /// <summary>
        ///     Resolves <paramref name="pathOrUid" /> via <see cref="ResourceUid.EnsurePath" /> (UID or path → project
        ///     path). Returns <see langword="false" /> when the UID is unknown or resolution fails.
        /// </summary>
        public static bool TryEnsurePath(string? pathOrUid, [NotNullWhen(true)] out string? path)
        {
            path = null;
            if (string.IsNullOrWhiteSpace(pathOrUid))
                return false;

            var ensured = ResourceUid.EnsurePath(pathOrUid.Trim());
            if (string.IsNullOrEmpty(ensured))
                return false;

            path = ensured;
            return true;
        }

        /// <summary>
        ///     Whether the running game’s <see cref="ResourceLoader" /> recognizes the path, using the same
        ///     remapping as <see cref="EnumerateCandidatePaths" />, optional <c>type_hint</c> checks, and the
        ///     cache (e.g. <see cref="Resource.TakeOverPath" /> scenarios).
        /// </summary>
        public static bool ResourceExists(string? rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                return false;

            foreach (var candidate in EnumerateCandidatePaths(rawPath))
            {
                if (ResourceLoader.Exists(candidate))
                    return true;

                if (ResourceExistenceTypeHints.Any(hint => ResourceLoader.Exists(candidate, hint))) return true;

                if (ResourceLoader.HasCached(candidate))
                    return true;
            }

            return false;
        }

        private static IEnumerable<string> EnumerateEnginePathCandidates(string trimmed)
        {
            yield return trimmed;

            if (trimmed.StartsWith("uid://", StringComparison.OrdinalIgnoreCase))
            {
                var resolved = ResourceUid.UidToPath(trimmed);
                if (!string.IsNullOrEmpty(resolved) &&
                    !string.Equals(resolved, trimmed, StringComparison.Ordinal))
                    yield return resolved;

                yield break;
            }

            var ensured = ResourceUid.EnsurePath(trimmed);
            if (!string.IsNullOrEmpty(ensured) &&
                !string.Equals(ensured, trimmed, StringComparison.Ordinal))
                yield return ensured;
        }
    }
}
