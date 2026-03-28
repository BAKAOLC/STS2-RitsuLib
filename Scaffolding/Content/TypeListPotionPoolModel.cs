using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Potion pool base that builds potions from declared CLR types and can override energy icon paths on pools.
    /// </summary>
    public abstract class TypeListPotionPoolModel : PotionPoolModel, IModBigEnergyIconPool, IModTextEnergyIconPool
    {
        /// <summary>
        ///     Potion model CLR types to include in this pool; each must be registered in <see cref="ModelDb" />.
        /// </summary>
        protected abstract IEnumerable<Type> PotionTypes { get; }

        /// <inheritdoc cref="IModBigEnergyIconPool.BigEnergyIconPath" />
        public virtual string? BigEnergyIconPath => null;

        /// <inheritdoc cref="IModTextEnergyIconPool.TextEnergyIconPath" />
        public virtual string? TextEnergyIconPath => null;

        /// <inheritdoc />
        protected sealed override IEnumerable<PotionModel> GenerateAllPotions()
        {
            return PotionTypes
                .Select(type => ModelDb.GetById<PotionModel>(ModelDb.GetId(type)))
                .ToArray();
        }
    }
}
