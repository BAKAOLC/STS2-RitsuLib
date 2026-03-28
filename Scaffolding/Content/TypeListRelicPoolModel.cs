using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Relic pool base that builds relics from declared CLR types and can override energy icon paths on pools.
    /// </summary>
    public abstract class TypeListRelicPoolModel : RelicPoolModel, IModBigEnergyIconPool, IModTextEnergyIconPool
    {
        /// <summary>
        ///     Relic model CLR types to include in this pool; each must be registered in <see cref="ModelDb" />.
        /// </summary>
        protected abstract IEnumerable<Type> RelicTypes { get; }

        /// <inheritdoc cref="IModBigEnergyIconPool.BigEnergyIconPath" />
        public virtual string? BigEnergyIconPath => null;

        /// <inheritdoc cref="IModTextEnergyIconPool.TextEnergyIconPath" />
        public virtual string? TextEnergyIconPath => null;

        /// <inheritdoc />
        protected sealed override IEnumerable<RelicModel> GenerateAllRelics()
        {
            return RelicTypes
                .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
                .ToArray();
        }
    }
}
