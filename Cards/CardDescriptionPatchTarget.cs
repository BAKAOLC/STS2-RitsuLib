using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Cards
{
    internal static class CardDescriptionPatchTarget
    {
        private const string DescriptionPreviewTypeName = "DescriptionPreviewType";

        internal static ModPatchTarget Create()
        {
            return new(
                typeof(CardModel),
                nameof(CardModel.GetDescriptionForPile),
                [typeof(PileType), GetDescriptionPreviewType(), typeof(Creature)]);
        }

        internal static bool IsUpgradePreview(object? previewType)
        {
            return string.Equals(
                previewType?.ToString(),
                "Upgrade",
                StringComparison.Ordinal);
        }

        private static Type GetDescriptionPreviewType()
        {
            return typeof(CardModel).GetNestedType(DescriptionPreviewTypeName, BindingFlags.NonPublic)
                   ?? throw new MissingMemberException(
                       typeof(CardModel).FullName,
                       DescriptionPreviewTypeName);
        }
    }
}
