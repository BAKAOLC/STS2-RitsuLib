using System.Text.RegularExpressions;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Resolves whole-value W3C Design Tokens references such as <c>{path.to.token}</c> within a merged
    ///         token tree. Each reference must identify a leaf token.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在合并后的令牌树中解析形如 <c>{path.to.token}</c> 的 W3C 设计令牌整值引用。
    ///         每个引用都必须指向一个叶令牌。
    ///     </para>
    /// </summary>
    internal static partial class RitsuShellThemeReferenceResolver
    {
        private static readonly Regex SingleReferenceRegex = GetSingleReferenceRegex();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves all whole-value references in <paramref name="root" /> in place. Missing targets and
        ///         cycles append diagnostics to <paramref name="errors" /> and leave the affected reference
        ///         unresolved.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         原地解析 <paramref name="root" /> 中的所有整值引用。目标缺失或出现循环引用时，会向
        ///         <paramref name="errors" /> 添加诊断，并保留受影响的未解析引用。
        ///     </para>
        /// </summary>
        /// <param name="root">
        ///     <para xml:lang="en">The merged token tree to update.</para>
        ///     <para xml:lang="zh-CN">要更新的合并令牌树。</para>
        /// </param>
        /// <param name="errors">
        ///     <para xml:lang="en">The collection that receives reference diagnostics.</para>
        ///     <para xml:lang="zh-CN">接收引用诊断的集合。</para>
        /// </param>
        public static void ResolveAll(Dictionary<string, object?> root, IList<string> errors)
        {
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            ResolveGroup(root, root, "", visiting, errors);
        }

        private static void ResolveGroup(Dictionary<string, object?> root, Dictionary<string, object?> group,
            string path, HashSet<string> visiting, IList<string> errors)
        {
            foreach (var key in group.Keys.ToList())
            {
                var value = group[key];
                var childPath = path.Length == 0 ? key : path + "." + key;
                switch (value)
                {
                    case LeafToken leaf:
                        group[key] = ResolveLeaf(root, leaf, childPath, visiting, errors);
                        break;
                    case Dictionary<string, object?> nested:
                        ResolveGroup(root, nested, childPath, visiting, errors);
                        break;
                }
            }
        }

        private static LeafToken ResolveLeaf(Dictionary<string, object?> root, LeafToken leaf, string ownPath,
            HashSet<string> visiting, IList<string> errors)
        {
            if (leaf.Value is not string s)
                return leaf;

            var match = SingleReferenceRegex.Match(s);
            if (!match.Success)
                return leaf;

            var refPath = match.Groups[1].Value.Trim();
            if (!visiting.Add(ownPath))
            {
                errors.Add($"Theme reference cycle at '{ownPath}'.");
                return leaf;
            }

            try
            {
                if (!TryFindLeaf(root, refPath, out var target))
                {
                    errors.Add($"Theme reference '{refPath}' (from '{ownPath}') did not resolve to a leaf.");
                    return leaf;
                }

                var resolvedTarget = ResolveLeaf(root, target!, refPath, visiting, errors);
                return leaf with
                {
                    Value = resolvedTarget.Value,
                    Type = leaf.Type ?? resolvedTarget.Type,
                };
            }
            finally
            {
                visiting.Remove(ownPath);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to find a leaf token by dotted path, such as <c>core.color.amber.500</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试按点分路径查找叶令牌，例如 <c>core.color.amber.500</c>。
        ///     </para>
        /// </summary>
        /// <param name="root">
        ///     <para xml:lang="en">The token tree to search.</para>
        ///     <para xml:lang="zh-CN">要搜索的令牌树。</para>
        /// </param>
        /// <param name="path">
        ///     <para xml:lang="en">The dotted path to the expected leaf.</para>
        ///     <para xml:lang="zh-CN">指向预期叶令牌的点分路径。</para>
        /// </param>
        /// <param name="leaf">
        ///     <para xml:lang="en">
        ///         Receives the matching leaf token, or <see langword="null" /> when the lookup fails.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         接收匹配的叶令牌；查找失败时为 <see langword="null" />。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if the path resolves to a leaf token; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若路径解析到叶令牌，则为 <see langword="true" />；否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryFindLeaf(Dictionary<string, object?> root, string path, out LeafToken? leaf)
        {
            leaf = null;
            object? cursor = root;
            foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                if (cursor is not Dictionary<string, object?> dict)
                    return false;
                if (!dict.TryGetValue(segment, out cursor))
                    return false;
            }

            if (cursor is not LeafToken leafToken)
                return false;
            leaf = leafToken;
            return true;
        }

        [GeneratedRegex(@"^\s*\{\s*([^{}]+?)\s*\}\s*$", RegexOptions.Compiled)]
        private static partial Regex GetSingleReferenceRegex();
    }
}
