using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace STS2RitsuLib.Scaffolding.Content
{
    public abstract class ModCardTemplate(
        int baseCost,
        CardType type,
        CardRarity rarity,
        TargetType target,
        bool showInCardLibrary = true,
        bool autoAdd = true) : CardModel(baseCost, type, rarity, target, showInCardLibrary)
    {
        protected bool AutoAddRequested { get; } = autoAdd;
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        protected sealed override IEnumerable<IHoverTip> ExtraHoverTips =>
            AdditionalHoverTips
                .Concat(RegisteredKeywordIds.ToHoverTips())
                .Concat(this.GetModKeywordHoverTips())
                .ToArray();
    }
}
