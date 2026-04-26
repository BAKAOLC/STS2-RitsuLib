using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Card pool base that can enumerate legacy CLR card types, expose optional pool frame material overrides,
    ///     and map energy icon paths for UI patches.
    /// </summary>
    public abstract class TypeListCardPoolModel : CardPoolModel, IModBigEnergyIconPool, IModTextEnergyIconPool,
        IModCardPoolFrameMaterial
    {
        /// <summary>
        ///     Legacy hook: enumerating card types on the pool class. Prefer registering each card through
        ///     <c>ModContentRegistry.RegisterCard&lt;TPool, TCard&gt;()</c>, <c>CreateContentPack.Card&lt;TPool, TCard&gt;()</c>,
        ///     or a manifest <c>CardRegistrationEntry</c> so <c>ModHelper.AddModelToPool</c> injects them without
        ///     duplicating the same <see cref="CardModel" /> instances when this property also lists those types.
        ///     Defaults to an empty sequence.
        /// </summary>
        [Obsolete(
            "Prefer ModContentRegistry / CreateContentPack .Card<TPool, TCard>() or manifest CardRegistrationEntry. "
            + "Listing types here duplicates ModHelper injection. Override only for legacy mods; suppress CS0618 if required.")]
        protected virtual IEnumerable<Type> CardTypes => [];

        /// <summary>
        ///     Path-based fallback for the card frame material.
        ///     Only used when <see cref="PoolFrameMaterial" /> is null.
        ///     Override this if you want to reference a pre-existing <c>.tres</c> material file.
        /// </summary>
        public override string CardFrameMaterialPath => "card_frame_colorless";

        /// <inheritdoc cref="IModBigEnergyIconPool.BigEnergyIconPath" />
        public virtual string? BigEnergyIconPath => null;

        /// <summary>
        ///     Directly supply a <see cref="Material" /> for all card frames in this pool.
        ///     When non-null, <see cref="CardFrameMaterialPath" /> is ignored.
        /// </summary>
        public virtual Material? PoolFrameMaterial => null;

        /// <inheritdoc cref="IModTextEnergyIconPool.TextEnergyIconPath" />
        public virtual string? TextEnergyIconPath => null;

        /// <inheritdoc />
        protected sealed override CardModel[] GenerateAllCards()
        {
#pragma warning disable CS0618 // Intentional: base invokes legacy CardTypes hook; suppress warning at call site only
            var types = CardTypes;
#pragma warning restore CS0618

            return types
                .Select(type => ModelDb.GetById<CardModel>(ModelDb.GetId(type)))
                .ToArray();
        }
    }
}
