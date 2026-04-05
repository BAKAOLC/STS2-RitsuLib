using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Injects registered keyword BBCode into <see cref="CardModel" /> description strings based on
    ///     <see cref="ModKeywordDefinition.CardDescriptionPlacement" />.
    /// </summary>
    internal static class ModKeywordCardDescriptionInjector
    {
        internal static void AppendFragments(CardModel card, ref string description)
        {
            description ??= string.Empty;

            var before = new List<string>();
            var after = new List<string>();

            foreach (var id in EnumerateKeywordIds(card))
            {
                if (!ModKeywordRegistry.TryGet(id, out var def))
                    continue;

                switch (def.CardDescriptionPlacement)
                {
                    case ModKeywordCardDescriptionPlacement.BeforeCardDescription:
                        before.Add(ModKeywordRegistry.GetCardText(id));
                        break;
                    case ModKeywordCardDescriptionPlacement.AfterCardDescription:
                        after.Add(ModKeywordRegistry.GetCardText(id));
                        break;
                    case ModKeywordCardDescriptionPlacement.None:
                        break;
                }
            }

            if (before.Count == 0 && after.Count == 0)
                return;

            List<string> lines;
            if (description.Length == 0)
                lines = new List<string>();
            else
                lines = description.Split('\n').ToList();

            for (var i = before.Count - 1; i >= 0; i--)
                lines.Insert(0, before[i]);

            foreach (var line in after)
                lines.Add(line);

            description = string.Join('\n', lines);
        }

        private static IEnumerable<string> EnumerateKeywordIds(CardModel card)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (card is ModCardTemplate mod)
                foreach (var id in mod.EnumerateDeclaredModKeywordIds())
                {
                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    var normalized = id.Trim().ToLowerInvariant();
                    if (string.IsNullOrEmpty(normalized) || !seen.Add(normalized))
                        continue;

                    yield return normalized;
                }

            foreach (var id in card.GetModKeywordIds())
            {
                if (!seen.Add(id))
                    continue;

                yield return id;
            }
        }
    }
}
