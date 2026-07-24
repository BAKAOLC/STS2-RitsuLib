using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Card pool base that can enumerate legacy CLR card types, expose optional pool frame material overrides,
    ///     and map energy icon paths for UI patches.
    ///     卡牌池基类：可枚举旧式 CLR 卡牌类型，公开可选的池边框材质覆盖，
    ///     并为 UI 补丁映射能量图标路径。
    /// </summary>
    public abstract class TypeListCardPoolModel : CardPoolModel, IModBigEnergyIconPool, IModTextEnergyIconPool,
        IModCardPoolFrameMaterial, IModCardPoolAssetOverrides, IModCardPoolDeckViewStyle
    {
        /// <summary>
        ///     Legacy hook: enumerating card types on the pool class. Prefer registering each card through
        ///     <c>ModContentRegistry.RegisterCard&lt;TPool, TCard&gt;()</c>, <c>CreateContentPack.Card&lt;TPool, TCard&gt;()</c>,
        ///     or a manifest <c>CardRegistrationEntry</c> so <c>ModHelper.AddModelToPool</c> injects them without
        ///     duplicating the same <see cref="CardModel" /> instances when this property also lists those types.
        ///     Defaults to an empty sequence.
        ///     旧式钩子：枚举池类上的卡牌类型。建议改为通过以下方式逐张注册卡牌：
        ///     <c>ModContentRegistry.RegisterCard&lt;TPool, TCard&gt;()</c>、<c>CreateContentPack.Card&lt;TPool, TCard&gt;()</c>，
        ///     或 manifest <c>CardRegistrationEntry</c>，让 <c>ModHelper.AddModelToPool</c> 注入它们，避免
        ///     当此属性也列出这些类型时重复生成同一批 <see cref="CardModel" /> 实例。
        ///     默认为空序列。
        /// </summary>
        [Obsolete(
            "Prefer ModContentRegistry / CreateContentPack .Card<TPool, TCard>() or manifest CardRegistrationEntry. "
            + "Listing types here duplicates ModHelper injection. Override only for legacy mods; suppress CS0618 if required.")]
        protected virtual IEnumerable<Type> CardTypes => [];

        /// <summary>
        ///     Path-based fallback for the card frame material.
        ///     Only used when <see cref="PoolFrameMaterial" /> is null.
        ///     Override this if you want to reference a pre-existing <c>.tres</c> material file.
        ///     卡牌边框材质的基于路径的回退。
        ///     仅当 <see cref="PoolFrameMaterial" /> 为 null 时使用。
        ///     如果要引用已有的 <c>.tres</c> 材质文件，请重写此项。
        /// </summary>
        public override string CardFrameMaterialPath => "card_frame_colorless";

        /// <inheritdoc cref="IModBigEnergyIconPool.BigEnergyIconPath" />
        public virtual string? BigEnergyIconPath => null;

        /// <inheritdoc cref="IModCardPoolAssetOverrides.AssetProfile" />
        public virtual CardPoolAssetProfile AssetProfile => CardPoolAssetProfile.Empty;

        /// <inheritdoc cref="IModCardPoolDeckViewStyle.DeckViewStyle" />
        public virtual CardPoolDeckViewStyle? DeckViewStyle => AssetProfile.DeckViewStyle;

        /// <summary>
        ///     Directly supply a <see cref="Material" /> for all card frames in this pool.
        ///     When non-null, <see cref="CardFrameMaterialPath" /> is ignored.
        ///     为此池中的所有卡牌边框直接提供 <see cref="Material" />。
        ///     非 null 时会忽略 <see cref="CardFrameMaterialPath" />。
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

            return
            [
                .. types
                    .Select(type => ModelDb.GetById<CardModel>(ModelDb.GetId(type))),
            ];
        }
    }
}
