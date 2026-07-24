using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Relic pool base that builds relics from declared CLR types and can override energy icon paths on pools.
    ///     遗物池基类：从声明的 CLR 类型构建遗物，并可覆盖池上的能量图标路径。
    /// </summary>
    public abstract class TypeListRelicPoolModel : RelicPoolModel, IModBigEnergyIconPool, IModTextEnergyIconPool
    {
        /// <summary>
        ///     Legacy hook: enumerating relic types on the pool class. Prefer registering each relic through
        ///     <c>ModContentRegistry.RegisterRelic&lt;TPool, TRelic&gt;()</c>,
        ///     <c>CreateContentPack.Relic&lt;TPool, TRelic&gt;()</c>,
        ///     or a manifest <c>RelicRegistrationEntry</c> so <c>ModHelper.AddModelToPool</c> injects them without
        ///     duplicating the same <see cref="RelicModel" /> instances when this property also lists those types.
        ///     Defaults to an empty sequence.
        ///     旧式钩子：枚举池类上的遗物类型。建议改为通过以下方式逐个注册遗物：
        ///     <c>ModContentRegistry.RegisterRelic&lt;TPool, TRelic&gt;()</c>、
        ///     或 manifest <c>RelicRegistrationEntry</c>，让 <c>ModHelper.AddModelToPool</c> 注入它们，避免
        ///     当此属性也列出这些类型时重复生成同一批 <see cref="RelicModel" /> 实例。
        ///     默认为空序列。
        /// </summary>
        [Obsolete(
            "Prefer ModContentRegistry / CreateContentPack .Relic<TPool, TRelic>() or manifest RelicRegistrationEntry. "
            + "Listing types here duplicates ModHelper injection. Override only for legacy mods; suppress CS0618 if required.")]
        protected virtual IEnumerable<Type> RelicTypes => [];

        /// <inheritdoc cref="IModBigEnergyIconPool.BigEnergyIconPath" />
        public virtual string? BigEnergyIconPath => null;

        /// <inheritdoc cref="IModTextEnergyIconPool.TextEnergyIconPath" />
        public virtual string? TextEnergyIconPath => null;

        /// <inheritdoc />
        protected sealed override IEnumerable<RelicModel> GenerateAllRelics()
        {
#pragma warning disable CS0618 // Intentional: base invokes legacy RelicTypes hook; suppress warning at call site only
            var types = RelicTypes;
#pragma warning restore CS0618

            return
            [
                .. types
                    .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type))),
            ];
        }
    }
}
