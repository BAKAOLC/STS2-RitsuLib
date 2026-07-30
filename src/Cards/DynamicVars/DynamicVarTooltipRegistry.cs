using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     <para xml:lang="en">Stores weakly attached tooltip factories for <see cref="DynamicVar" /> instances.</para>
    ///     <para xml:lang="zh-CN">存储弱关联到 <see cref="DynamicVar" /> 实例的工具提示工厂。</para>
    /// </summary>
    public static class DynamicVarTooltipRegistry
    {
        private static readonly AttachedState<DynamicVar, Func<DynamicVar, IHoverTip>?> TooltipFactories =
            new(() => null);

        /// <summary>
        ///     <para xml:lang="en">Associates <paramref name="dynamicVar" /> with <paramref name="tooltipFactory" />.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="dynamicVar" /> 与 <paramref name="tooltipFactory" /> 关联。</para>
        /// </summary>
        public static void Set(DynamicVar dynamicVar, Func<DynamicVar, IHoverTip> tooltipFactory)
        {
            ArgumentNullException.ThrowIfNull(dynamicVar);
            ArgumentNullException.ThrowIfNull(tooltipFactory);
            TooltipFactories[dynamicVar] = tooltipFactory;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the factory registered for <paramref name="dynamicVar" />, if any.</para>
        ///     <para xml:lang="zh-CN">获取为 <paramref name="dynamicVar" /> 注册的工厂（如果有）。</para>
        /// </summary>
        public static Func<DynamicVar, IHoverTip>? Get(DynamicVar dynamicVar)
        {
            ArgumentNullException.ThrowIfNull(dynamicVar);
            return TooltipFactories[dynamicVar];
        }

        /// <summary>
        ///     <para xml:lang="en">Invokes the factory registered for <paramref name="dynamicVar" />, if any.</para>
        ///     <para xml:lang="zh-CN">调用为 <paramref name="dynamicVar" /> 注册的工厂（如果有）。</para>
        /// </summary>
        public static IHoverTip? Create(DynamicVar dynamicVar)
        {
            ArgumentNullException.ThrowIfNull(dynamicVar);
            var factory = Get(dynamicVar);
            if (factory is null)
                return null;

            try
            {
                return factory(dynamicVar);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DynamicVarTooltipRegistry] Tooltip factory failed for '{dynamicVar.Name}': {ex}");
                return null;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Copies the tooltip factory from <paramref name="source" /> to <paramref name="destination" />,
        ///         if present.
        ///     </para>
        ///     <para xml:lang="zh-CN">将工具提示工厂从 <paramref name="source" /> 复制到 <paramref name="destination" />（如果有）。</para>
        /// </summary>
        public static void CopyTo(DynamicVar source, DynamicVar destination)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);

            var factory = Get(source);
            if (factory != null)
                TooltipFactories[destination] = factory;
        }
    }
}
