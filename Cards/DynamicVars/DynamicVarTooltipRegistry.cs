using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     Weakly attached per-<see cref="DynamicVar" /> tooltip factories (not serialized with game data).
    ///     弱附加到每个 <see cref="DynamicVar" /> 的工具提示工厂（不会随游戏数据序列化）。
    /// </summary>
    public static class DynamicVarTooltipRegistry
    {
        private static readonly AttachedState<DynamicVar, Func<DynamicVar, IHoverTip>?> TooltipFactories =
            new(() => null);

        /// <summary>
        ///     Associates <paramref name="dynamicVar" /> with <paramref name="tooltipFactory" />.
        ///     将 <paramref name="dynamicVar" /> 与 <paramref name="tooltipFactory" /> 关联。
        /// </summary>
        public static void Set(DynamicVar dynamicVar, Func<DynamicVar, IHoverTip> tooltipFactory)
        {
            ArgumentNullException.ThrowIfNull(dynamicVar);
            ArgumentNullException.ThrowIfNull(tooltipFactory);
            TooltipFactories[dynamicVar] = tooltipFactory;
        }

        /// <summary>
        ///     Returns the registered factory for <paramref name="dynamicVar" />, if any.
        ///     返回 <paramref name="dynamicVar" /> 的已注册工厂（如果存在）。
        /// </summary>
        public static Func<DynamicVar, IHoverTip>? Get(DynamicVar dynamicVar)
        {
            ArgumentNullException.ThrowIfNull(dynamicVar);
            return TooltipFactories[dynamicVar];
        }

        /// <summary>
        ///     Invokes the registered factory for <paramref name="dynamicVar" />.
        ///     调用 <paramref name="dynamicVar" /> 的已注册工厂。
        /// </summary>
        public static IHoverTip? Create(DynamicVar dynamicVar)
        {
            ArgumentNullException.ThrowIfNull(dynamicVar);
            var factory = Get(dynamicVar);
            return factory?.Invoke(dynamicVar);
        }

        /// <summary>
        ///     Copies the tooltip factory from <paramref name="source" /> to <paramref name="destination" /> when present.
        ///     存在时，将工具提示工厂从 <paramref name="source" /> 复制到 <paramref name="destination" />。
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
