using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    public abstract class TypeListCardPoolModel : CardPoolModel, IModBigEnergyIconPool, IModTextEnergyIconPool,
        IModCardPoolFrameMaterial
    {
        protected abstract IEnumerable<Type> CardTypes { get; }

        /// <summary>
        ///     Path-based fallback for the card frame material.
        ///     Only used when <see cref="PoolFrameMaterial" /> is null.
        ///     Override this if you want to reference a pre-existing <c>.tres</c> material file.
        /// </summary>
        public override string CardFrameMaterialPath => "card_frame_colorless_mat";

        /// <inheritdoc cref="IModBigEnergyIconPool.BigEnergyIconPath" />
        public virtual string? BigEnergyIconPath => null;

        /// <summary>
        ///     Directly supply a <see cref="Material" /> for all card frames in this pool.
        ///     When non-null, <see cref="CardFrameMaterialPath" /> is ignored.
        /// </summary>
        public virtual Material? PoolFrameMaterial => null;

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
