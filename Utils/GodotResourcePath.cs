using System.Diagnostics.CodeAnalysis;
using Godot;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Public helpers for Godot project paths: <c>res://</c>, <c>user://</c>, <c>uid://</c> remapping and
    ///     resource presence checks aligned with <see cref="ResourceLoader" /> and <see cref="ResourceUid" />.
    ///     Godot 项目路径的公共辅助方法：<c>res://</c>、<c>user://</c>、<c>uid://</c> 重映射以及
    ///     与 <see cref="ResourceLoader" /> 和 <see cref="ResourceUid" /> 对齐的资源存在性检查。
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
        ///     生成引擎可能用于同一逻辑资源的路径：修剪后的输入、<c>uid://</c> →
        ///     <c>res://</c>（适用时），以及 <see cref="ResourceUid.EnsurePath" /> 替代路径。
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
        ///     通过 <see cref="ResourceUid.EnsurePath" /> 解析 <paramref name="pathOrUid" />（UID 或路径 → 项目
        ///     路径）。当 UID 未知或解析失败时返回 <see langword="false" />。
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
        ///     运行中游戏的 <see cref="ResourceLoader" /> 是否识别该路径，使用与
        ///     <see cref="EnumerateCandidatePaths" /> 相同的重映射、可选 <c>type_hint</c> 检查以及
        ///     缓存（例如 <see cref="Resource.TakeOverPath" /> 场景）。
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
