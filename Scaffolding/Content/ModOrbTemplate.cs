using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace STS2RitsuLib.Scaffolding.Content
{
    public abstract class ModOrbTemplate : OrbModel
    {
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        protected sealed override IEnumerable<IHoverTip> ExtraHoverTips =>
            AdditionalHoverTips
                .Concat(RegisteredKeywordIds.ToHoverTips())
                .Concat(this.GetModKeywordHoverTips())
                .ToArray();

        public override Color DarkenedColor => Colors.DarkSlateGray;
    }
}
