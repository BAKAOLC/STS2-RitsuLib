using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    public abstract class TypeListRelicPoolModel : RelicPoolModel, IModTextEnergyIconPool
    {
        protected abstract IEnumerable<Type> RelicTypes { get; }

        /// <inheritdoc cref="IModTextEnergyIconPool.TextEnergyIconPath" />
        public virtual string? TextEnergyIconPath => null;

        protected sealed override IEnumerable<RelicModel> GenerateAllRelics()
        {
            return RelicTypes
                .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
                .ToArray();
        }
    }
}
