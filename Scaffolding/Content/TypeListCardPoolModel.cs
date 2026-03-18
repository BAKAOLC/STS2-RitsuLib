using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    public abstract class TypeListCardPoolModel : CardPoolModel, IModTextEnergyIconPool
    {
        protected abstract IEnumerable<Type> CardTypes { get; }

        /// <inheritdoc cref="IModTextEnergyIconPool.TextEnergyIconPath" />
        public virtual string? TextEnergyIconPath => null;

        protected sealed override CardModel[] GenerateAllCards()
        {
            return CardTypes
                .Select(type => ModelDb.GetById<CardModel>(ModelDb.GetId(type)))
                .ToArray();
        }
    }
}
