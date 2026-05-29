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
        ///     remapping as <see cref="EnumerateCandidatePaths" />. Relies solely on
        ///     <see cref="ResourceLoader.Exists(string, string)" />, which already consults the resource cache
        ///     (covering <see cref="Resource.TakeOverPath" /> / in-memory resources) before checking the file
        ///     system, so an unresolved path is reported as missing instead of being masked by looser type-hint or
        ///     cache probes.
        ///     运行中游戏的 <see cref="ResourceLoader" /> 是否识别该路径，使用与
        ///     <see cref="EnumerateCandidatePaths" /> 相同的重映射。仅依赖
        ///     <see cref="ResourceLoader.Exists(string, string)" />——它在检查文件系统前已先查资源缓存
        ///     （覆盖 <see cref="Resource.TakeOverPath" /> / 内存资源），因此无法解析的路径会被如实判定为缺失，
        ///     而不会被更宽松的 type_hint 或缓存探测掩盖。
        /// </summary>
        public static bool ResourceExists(string? rawPath)
        {
            return !string.IsNullOrWhiteSpace(rawPath) &&
                   EnumerateCandidatePaths(rawPath).Any(candidate => ResourceLoader.Exists(candidate));
        }

        /// <summary>
        ///     Loads the first <see cref="EnumerateCandidatePaths" /> candidate that both resolves and yields a
        ///     resource assignable to <typeparamref name="T" />. Loading is untyped and then cast, so a resource
        ///     whose concrete class differs from a narrow request (e.g. an <c>AtlasTexture</c> where
        ///     <c>CompressedTexture2D</c> was requested) is matched against <typeparamref name="T" /> directly rather
        ///     than failing inside <see cref="ResourceLoader.Load{T}(string, string, ResourceLoader.CacheMode)" />.
        ///     The candidate that is loaded is the one that actually resolves, avoiding the "checked one path, loaded
        ///     another" mismatch with the raw input. Returns <see langword="false" /> without loading when
        ///     <paramref name="rawPath" /> is null or blank (i.e. nothing was defined).
        ///     加载第一个既能解析、又能转换为 <typeparamref name="T" /> 的 <see cref="EnumerateCandidatePaths" /> 候选。
        ///     采用 untyped 加载再转型，因此具体类与较窄请求不同的资源（例如请求 <c>CompressedTexture2D</c> 实际是
        ///     <c>AtlasTexture</c>）会直接按 <typeparamref name="T" /> 匹配，而不是在
        ///     <see cref="ResourceLoader.Load{T}(string, string, ResourceLoader.CacheMode)" /> 内部失败。实际加载的是
        ///     真正解析成功的那个候选，避免「检查的是一个路径、加载的是另一个」的错位。当
        ///     <paramref name="rawPath" /> 为 null 或空白（即未定义）时不加载并返回 <see langword="false" />。
        /// </summary>
        public static bool TryLoad<T>(string? rawPath, [NotNullWhen(true)] out T? resource)
            where T : class
        {
            resource = null;
            if (string.IsNullOrWhiteSpace(rawPath))
                return false;

            foreach (var candidate in EnumerateCandidatePaths(rawPath))
            {
                if (!ResourceLoader.Exists(candidate))
                    continue;

                if (ResourceLoader.Load(candidate) is not T typed) continue;
                resource = typed;
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
