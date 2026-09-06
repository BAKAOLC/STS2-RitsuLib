using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Ui.Catalog;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class RitsuDebugToolsPanel
    {
        private static readonly ConditionalWeakTable<AbstractModel, RitsuCatalogSearchDocument> SearchDocuments = new();

        private static RitsuCatalogSearchDocument CreateSearchDocument(AbstractModel model)
        {
            return model.IsMutable ? Create(model) : SearchDocuments.GetValue(model, Create);

            static RitsuCatalogSearchDocument Create(AbstractModel value)
            {
                return new(
                    value.Id.ToString(),
                    value is CardModel or PotionModel or RelicModel or PowerModel or OrbModel or
                        EnchantmentModel or AfflictionModel
                        ? () => SearchDescription(value)
                        : null,
                    value is CardModel or PotionModel or RelicModel or PowerModel or OrbModel or
                        EnchantmentModel or AfflictionModel
                        ? () => SearchKeywords(value)
                        : null);
            }
        }

        private static string SearchDescription(AbstractModel model)
        {
            return model switch
            {
                CardModel card => CreateCardPreviewModel(card).GetDescriptionForPile(PileType.None),
                PotionModel potion => potion.DynamicDescription.GetFormattedText(),
                RelicModel relic => relic.DynamicDescription.GetFormattedText(),
                PowerModel power => power.Description.GetFormattedText(),
                OrbModel orb => orb.Description.GetFormattedText(),
                EnchantmentModel enchantment => enchantment.DynamicDescription.GetFormattedText(),
                AfflictionModel affliction => affliction.DynamicDescription.GetFormattedText(),
                _ => string.Empty,
            };
        }

        private static string SearchKeywords(AbstractModel model)
        {
            var source = model is CardModel { IsMutable: false } card ? CreateCardPreviewModel(card) : model;
            var names = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            if (source is CardModel keywordCard)
                foreach (var keyword in keywordCard.Keywords)
                {
                    var title = keyword.GetTitle().GetFormattedText();
                    if (!string.IsNullOrWhiteSpace(title))
                        names.Add(title);
                }

            var tips = source switch
            {
                CardModel preview => preview.HoverTips,
                PotionModel potion => potion.ExtraHoverTips,
                RelicModel relic => relic.HoverTips,
                PowerModel power => power.HoverTips,
                OrbModel orb => orb.HoverTips,
                EnchantmentModel enchantment => enchantment.HoverTips,
                AfflictionModel affliction => affliction.HoverTips,
                _ => [],
            };
            foreach (var tip in tips.Take(128))
            {
                if (tip.CanonicalModel?.Id == model.Id)
                    continue;
                var title = tip switch
                {
                    HoverTip text => text.Title,
                    CardHoverTip cardTip => SafeTitle(cardTip.Card),
                    _ => null,
                };
                if (!string.IsNullOrWhiteSpace(title))
                    names.Add(title);
            }

            return string.Join('\n', names);
        }
    }
}
