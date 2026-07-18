using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Injects registered keyword BBCode into <see cref="CardModel" /> description strings based on
    ///     <see cref="ModKeywordDefinition.CardDescriptionPlacement" />.
    ///     根据 <see cref="ModKeywordDefinition.CardDescriptionPlacement" /> 将已注册 keyword BBCode 注入
    ///     <see cref="CardModel" /> description 字符串。
    /// </summary>
    internal static class ModKeywordCardDescriptionInjector
    {
        internal static void AppendFragments(CardModel card, ref string description)
        {
            description ??= string.Empty;

            if (ModKeywordRegistry.IsFrozen && !ModKeywordRegistry.HasCardDescriptionPlacements)
                return;

            List<ModKeywordDefinition>? definitions = null;
            foreach (var keyword in card.Keywords)
            {
                if (!ModKeywordRegistry.TryGetByCardKeyword(keyword, out var definition) ||
                    definition.CardDescriptionPlacement == ModKeywordCardDescriptionPlacement.None)
                    continue;

                definitions ??= [];
                definitions.Add(definition);
            }

            if (definitions == null)
                return;

            definitions.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));

            List<string>? before = null;
            List<string>? after = null;
            foreach (var definition in definitions)
                switch (definition.CardDescriptionPlacement)
                {
                    case ModKeywordCardDescriptionPlacement.BeforeCardDescription:
                        before ??= [];
                        before.Add(ModKeywordRegistry.GetCardText(definition.Id));
                        break;
                    case ModKeywordCardDescriptionPlacement.AfterCardDescription:
                        after ??= [];
                        after.Add(ModKeywordRegistry.GetCardText(definition.Id));
                        break;
                    case ModKeywordCardDescriptionPlacement.None:
                        break;
                }

            var lines = description.Length == 0 ? [] : description.Split('\n').ToList();

            if (before != null)
                for (var i = before.Count - 1; i >= 0; i--)
                    lines.Insert(0, before[i]);

            if (after != null)
                lines.AddRange(after);

            description = string.Join('\n', lines);
        }
    }
}
