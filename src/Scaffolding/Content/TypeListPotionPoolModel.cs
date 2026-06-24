using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Potion pool base that builds potions from declared CLR types and can override energy icon paths on pools.
    ///     药水池基类：从声明的 CLR 类型构建药水，并可覆盖池上的能量图标路径。
    /// </summary>
    public abstract class TypeListPotionPoolModel : PotionPoolModel, IModBigEnergyIconPool, IModTextEnergyIconPool
    {
        /// <summary>
        ///     Legacy hook: enumerating potion types on the pool class. Prefer registering each potion through
        ///     <c>ModContentRegistry.RegisterPotion&lt;TPool, TPotion&gt;()</c>,
        ///     <c>CreateContentPack.Potion&lt;TPool, TPotion&gt;()</c>,
        ///     or a manifest <c>PotionRegistrationEntry</c> so <c>ModHelper.AddModelToPool</c> injects them without
        ///     duplicating the same <see cref="PotionModel" /> instances when this property also lists those types.
        ///     Defaults to an empty sequence.
        ///     旧式钩子：枚举池类上的药水类型。建议改为通过以下方式逐个注册药水：
        ///     <c>ModContentRegistry.RegisterPotion&lt;TPool, TPotion&gt;()</c>、
        ///     或 manifest <c>PotionRegistrationEntry</c>，让 <c>ModHelper.AddModelToPool</c> 注入它们，避免
        ///     当此属性也列出这些类型时重复生成同一批 <see cref="PotionModel" /> 实例。
        ///     默认为空序列。
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

            return types
                .Select(type => ModelDb.GetById<PotionModel>(ModelDb.GetId(type)))
                .ToArray();
        }
    }
}
