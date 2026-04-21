using MegaCrit.Sts2.Core.Models;

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

            var lines = description.Length == 0 ? [] : description.Split('\n').ToList();

            for (var i = before.Count - 1; i >= 0; i--)
                lines.Insert(0, before[i]);

            lines.AddRange(after);

            description = string.Join('\n', lines);
        }

        private static IEnumerable<string> EnumerateKeywordIds(CardModel card)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in card.GetModKeywordIds())
                if (seen.Add(id))
                    yield return id;
        }
    }
}
