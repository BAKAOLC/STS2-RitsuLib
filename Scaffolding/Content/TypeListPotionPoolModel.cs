using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    public abstract class TypeListPotionPoolModel : PotionPoolModel, IModTextEnergyIconPool
    {
        protected abstract IEnumerable<Type> PotionTypes { get; }

        /// <inheritdoc cref="IModTextEnergyIconPool.TextEnergyIconPath" />
        public virtual string? TextEnergyIconPath => null;

        protected sealed override IEnumerable<PotionModel> GenerateAllPotions()
        {
            return PotionTypes
                .Select(type => ModelDb.GetById<PotionModel>(ModelDb.GetId(type)))
                .ToArray();
        }
    }
}
