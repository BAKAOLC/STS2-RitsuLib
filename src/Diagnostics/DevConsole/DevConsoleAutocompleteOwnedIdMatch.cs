using STS2RitsuLib.CardPiles;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Content;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.TopBar;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">Provides tail-aware autocomplete matching for public-entry IDs registered through RitsuLib.</para>
    ///     <para xml:lang="zh-CN">为通过 RitsuLib 注册的公共条目 ID 提供感知尾部片段的自动补全匹配。</para>
    /// </summary>
    public static class DevConsoleAutocompleteOwnedIdMatch
    {
        /// <summary>
        ///     <para xml:lang="en">Matches a full ID prefix or an owned ID's mod-qualified tail.</para>
        ///     <para xml:lang="zh-CN">匹配完整 ID 的前缀，或已知所属 ID 中带模组限定的尾部片段。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Ownership is resolved from current registry snapshots, including registrations added after an
        ///         earlier match.
        ///     </para>
        ///     <para xml:lang="zh-CN">所属关系通过当前注册表快照解析，因此也包含先前匹配之后新增的注册。</para>
        /// </remarks>
        public static bool Match(string candidate, string partial)
        {
            if (string.IsNullOrWhiteSpace(partial))
                return true;

            var token = partial.Trim();
            var entryId = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(candidate);

            if (entryId.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!TryGetOwnedTail(entryId, out var tail))
                return false;

            return tail.StartsWith(token, StringComparison.OrdinalIgnoreCase) ||
                   tail.Contains(token, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetOwnedTail(string candidate, out string tail)
        {
            tail = string.Empty;
            if (string.IsNullOrWhiteSpace(candidate))
                return false;

            if (!TryGetOwnerModId(candidate.Trim(), out var ownerModId))
                return false;

            var modPrefix = ModContentRegistry.NormalizePublicStem(ownerModId) + "_";
            if (!candidate.StartsWith(modPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            if (candidate.Length <= modPrefix.Length)
                return false;

            tail = candidate[modPrefix.Length..];
            return tail.Contains('_');
        }

        private static bool TryGetOwnerModId(string candidate, out string ownerModId)
        {
            foreach (var s in ModContentRegistry.GetRegisteredTypeSnapshots())
                if (string.Equals(s.ModelDbId?.Entry, candidate, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s.ExpectedPublicEntry, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    ownerModId = s.ModId;
                    return true;
                }

            if (ModKeywordRegistry.TryGetOwnerModId(candidate, out ownerModId) ||
                ModCardTagRegistry.TryGetOwnerModId(candidate, out ownerModId) ||
                ModCardPileRegistry.TryGetOwnerModId(candidate, out ownerModId))
                return true;

            if (ModTopBarButtonRegistry.TryGet(candidate, out var topBarButton))
            {
                ownerModId = topBarButton.ModId;
                return true;
            }

            ownerModId = string.Empty;
            return false;
        }
    }
}
