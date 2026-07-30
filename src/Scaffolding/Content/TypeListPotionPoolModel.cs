using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a potion-pool base class with legacy CLR type enumeration and energy-icon path overrides.
    ///     </para>
    ///     <para xml:lang="zh-CN">提供支持旧式 CLR 类型枚举和能量图标路径覆盖的药水池基类。</para>
    /// </summary>
    public abstract class TypeListPotionPoolModel : PotionPoolModel, IModBigEnergyIconPool, IModTextEnergyIconPool
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Legacy hook that enumerates potion types declared by the pool. Prefer registering each potion through
        ///         <c>ModContentRegistry.RegisterPotion&lt;TPool, TPotion&gt;()</c>,
        ///         <c>CreateContentPack.Potion&lt;TPool, TPotion&gt;()</c>, or a manifest
        ///         <c>PotionRegistrationEntry</c>, which lets <c>ModHelper.AddModelToPool</c> inject the potion without
        ///         duplicating entries when this property lists the same type. The default sequence is empty.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         用于枚举池所声明药水类型的旧式钩子。建议通过
        ///         <c>ModContentRegistry.RegisterPotion&lt;TPool, TPotion&gt;()</c>、
        ///         <c>CreateContentPack.Potion&lt;TPool, TPotion&gt;()</c> 或清单中的 <c>PotionRegistrationEntry</c>
        ///         逐个注册药水，由 <c>ModHelper.AddModelToPool</c> 注入，以免此属性列出相同类型时产生重复条目。
        ///         默认返回空序列。
        ///     </para>
        /// </summary>
        [Obsolete(
            "Prefer ModContentRegistry / CreateContentPack .Potion<TPool, TPotion>() or manifest PotionRegistrationEntry. "
            + "Listing types here duplicates ModHelper injection. Override only for legacy mods; suppress CS0618 if required.")]
        protected virtual IEnumerable<Type> PotionTypes => [];

        /// <inheritdoc cref="IModBigEnergyIconPool.BigEnergyIconPath" />
        public virtual string? BigEnergyIconPath => null;

        /// <inheritdoc cref="IModTextEnergyIconPool.TextEnergyIconPath" />
        public virtual string? TextEnergyIconPath => null;

        /// <inheritdoc />
        protected sealed override IEnumerable<PotionModel> GenerateAllPotions()
        {
#pragma warning disable CS0618 // Intentional: base invokes legacy PotionTypes hook; suppress warning at call site only
            var types = PotionTypes;
#pragma warning restore CS0618

            return
            [
                .. types
                    .Select(type => ModelDb.GetById<PotionModel>(ModelDb.GetId(type))),
            ];
        }
    }
}
