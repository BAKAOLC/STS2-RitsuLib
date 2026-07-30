using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a card-pool base class with legacy CLR type enumeration, optional frame-material overrides,
    ///         and energy-icon paths used by UI patches.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供卡牌池基类，支持旧式 CLR 类型枚举、可选的边框材质覆盖，以及供界面补丁使用的能量图标路径。
    ///     </para>
    /// </summary>
    public abstract class TypeListCardPoolModel : CardPoolModel, IModBigEnergyIconPool, IModTextEnergyIconPool,
        IModCardPoolFrameMaterial, IModCardPoolAssetOverrides, IModCardPoolDeckViewStyle
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Legacy hook that enumerates card types declared by the pool. Prefer registering each card through
        ///         <c>ModContentRegistry.RegisterCard&lt;TPool, TCard&gt;()</c>,
        ///         <c>CreateContentPack.Card&lt;TPool, TCard&gt;()</c>, or a manifest
        ///         <c>CardRegistrationEntry</c>, which lets <c>ModHelper.AddModelToPool</c> inject the card without
        ///         duplicating entries when this property lists the same type. The default sequence is empty.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         用于枚举池所声明卡牌类型的旧式钩子。建议通过
        ///         <c>ModContentRegistry.RegisterCard&lt;TPool, TCard&gt;()</c>、
        ///         <c>CreateContentPack.Card&lt;TPool, TCard&gt;()</c> 或清单中的 <c>CardRegistrationEntry</c>
        ///         逐张注册卡牌，由 <c>ModHelper.AddModelToPool</c> 注入，以免此属性列出相同类型时产生重复条目。
        ///         默认返回空序列。
        ///     </para>
        /// </summary>
        [Obsolete(
            "Prefer ModContentRegistry / CreateContentPack .Card<TPool, TCard>() or manifest CardRegistrationEntry. "
            + "Listing types here duplicates ModHelper injection. Override only for legacy mods; suppress CS0618 if required.")]
        protected virtual IEnumerable<Type> CardTypes => [];

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the path-based fallback for the card-frame material. This property is used only when
        ///         <see cref="PoolFrameMaterial" /> is <see langword="null" />; override it to reference an existing
        ///         <c>.tres</c> material resource.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取卡牌边框材质的路径回退。仅当 <see cref="PoolFrameMaterial" /> 为 <see langword="null" /> 时使用此属性；
        ///         可重写它以引用已有的 <c>.tres</c> 材质资源。
        ///     </para>
        /// </summary>
        public override string CardFrameMaterialPath => "card_frame_colorless";

        /// <inheritdoc cref="IModBigEnergyIconPool.BigEnergyIconPath" />
        public virtual string? BigEnergyIconPath => null;

        /// <inheritdoc cref="IModCardPoolAssetOverrides.AssetProfile" />
        public virtual CardPoolAssetProfile AssetProfile => CardPoolAssetProfile.Empty;

        /// <inheritdoc cref="IModCardPoolDeckViewStyle.DeckViewStyle" />
        public virtual CardPoolDeckViewStyle? DeckViewStyle => AssetProfile.DeckViewStyle;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the <see cref="Material" /> used by every card frame in this pool. When non-null,
        ///         <see cref="CardFrameMaterialPath" /> is ignored.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取此池中所有卡牌边框使用的 <see cref="Material" />。非空时会忽略
        ///         <see cref="CardFrameMaterialPath" />。
        ///     </para>
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
