using System.Diagnostics.CodeAnalysis;
using Godot;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Public helpers for Godot project paths: <c>res://</c>, <c>user://</c>, <c>uid://</c> remapping
    ///         and resource presence checks aligned with <see cref="ResourceLoader" /> and <see cref="ResourceUid" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供 Godot 项目路径辅助方法，包括 <c>res://</c>、<c>user://</c>、<c>uid://</c> 重映射，以及与
    ///         <see cref="ResourceLoader" /> 和 <see cref="ResourceUid" /> 一致的资源存在性检查。
    ///     </para>
    /// </summary>
    public static class GodotResourcePath
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Yields paths the engine may use for the same logical asset: the trimmed input, <c>uid://</c> →
        ///         <c>res://</c> (when applicable), and <see cref="ResourceUid.EnsurePath" /> alternatives.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         生成引擎可能用于同一逻辑资源的路径：修剪后的输入、<c>uid://</c> → <c>res://</c>（适用时），以及
        ///         <see cref="ResourceUid.EnsurePath" /> 替代路径。
        ///     </para>
        /// </summary>
        public static IEnumerable<string> EnumerateCandidatePaths(string? rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                yield break;

            foreach (var candidate in EnumerateEnginePathCandidates(rawPath.Trim()))
                yield return candidate;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves <paramref name="pathOrUid" /> via <see cref="ResourceUid.EnsurePath" /> (UID or path →
        ///         project path). Returns <see langword="false" /> when the UID is unknown or resolution fails.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过 <see cref="ResourceUid.EnsurePath" /> 将 <paramref name="pathOrUid" />（UID 或路径）解析为项目路径。UID
        ///         未知或解析失败时返回 <see langword="false" />。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Returns whether <see cref="ResourceLoader" /> recognizes any normalized candidate path.
        ///         <see cref="ResourceLoader.Exists(string, string)" /> checks cached and in-memory resources before
        ///         the file system.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <see cref="ResourceLoader" /> 是否识别任一规范化候选路径。
        ///         <see cref="ResourceLoader.Exists(string, string)" /> 会先检查缓存和内存资源，再检查文件系统。
        ///     </para>
        /// </summary>
        public static bool ResourceExists(string? rawPath)
        {
            return !string.IsNullOrWhiteSpace(rawPath) &&
                   EnumerateCandidatePaths(rawPath).Any(candidate => ResourceLoader.Exists(candidate));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Loads the first resolvable candidate assignable to <typeparamref name="T" />. The resource is loaded
        ///         without a type hint and then cast, allowing compatible concrete resource types that differ from a
        ///         narrower request. Returns <see langword="false" /> without loading when <paramref name="rawPath" />
        ///         is null or blank.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         加载第一个可解析且可赋值给 <typeparamref name="T" /> 的候选资源。该方法不带类型提示加载资源后再转型，
        ///         因而允许具体资源类型与较窄请求不同但仍然兼容。<paramref name="rawPath" /> 为 null 或空白时不加载，
        ///         并返回 <see langword="false" />。
        ///     </para>
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
