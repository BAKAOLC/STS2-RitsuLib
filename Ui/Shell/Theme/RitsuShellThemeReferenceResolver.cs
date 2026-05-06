using System.Text.RegularExpressions;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Resolves W3C Design Tokens <c>{path.to.token}</c> references inside a merged token tree, in place.
    ///     A reference must denote a single leaf; the resolver replaces it with the leaf's resolved value.
    /// </summary>
    internal static partial class RitsuShellThemeReferenceResolver
    {
        private static readonly Regex SingleReferenceRegex = GetSingleReferenceRegex();

        /// <summary>
        ///     Resolves all <c>{ref}</c> references in <paramref name="root" />. Loops produce an exception in
        ///     <paramref name="errors" /> and the offending leaf is left with its raw string.
        /// </summary>
        /// <param name="root">Merged token root.</param>
        /// <param name="errors">Diagnostics accumulator.</param>
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
        ///     Looks up a leaf token by dotted path (e.g. <c>core.color.amber.500</c>).
        /// </summary>
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
