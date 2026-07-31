using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a relic-pool base class with legacy CLR type enumeration and energy-icon path overrides.
    ///     </para>
    ///     <para xml:lang="zh-CN">提供支持旧式 CLR 类型枚举和能量图标路径覆盖的遗物池基类。</para>
    /// </summary>
    public abstract class TypeListRelicPoolModel : RelicPoolModel, IModBigEnergyIconPool, IModTextEnergyIconPool
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Legacy hook that enumerates relic types declared by the pool. Prefer registering each relic through
        ///         <c>ModContentRegistry.RegisterRelic&lt;TPool, TRelic&gt;()</c>,
        ///         <c>CreateContentPack.Relic&lt;TPool, TRelic&gt;()</c>, or a manifest
        ///         <c>RelicRegistrationEntry</c>, which lets <c>ModHelper.AddModelToPool</c> inject the relic without
        ///         duplicating entries when this property lists the same type. The default sequence is empty.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         用于枚举池所声明遗物类型的旧式钩子。建议通过
        ///         <c>ModContentRegistry.RegisterRelic&lt;TPool, TRelic&gt;()</c>、
        ///         <c>CreateContentPack.Relic&lt;TPool, TRelic&gt;()</c> 或清单中的 <c>RelicRegistrationEntry</c>
        ///         逐个注册遗物，由 <c>ModHelper.AddModelToPool</c> 注入，以免此属性列出相同类型时产生重复条目。
        ///         默认返回空序列。
        ///     </para>
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
