using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     Weakly attached per-<see cref="DynamicVar" /> tooltip factories (not serialized with game data).
    /// </summary>
    public static class DynamicVarTooltipRegistry
    {
        private static readonly AttachedState<DynamicVar, Func<DynamicVar, IHoverTip>?> TooltipFactories =
            new(() => null);

        /// <summary>
        ///     Associates <paramref name="dynamicVar" /> with <paramref name="tooltipFactory" />.
        /// </summary>
        public static void Set(DynamicVar dynamicVar, Func<DynamicVar, IHoverTip> tooltipFactory)
        {
            ArgumentNullException.ThrowIfNull(dynamicVar);
            ArgumentNullException.ThrowIfNull(tooltipFactory);
            TooltipFactories[dynamicVar] = tooltipFactory;
        }

        /// <summary>
        ///     Returns the registered factory for <paramref name="dynamicVar" />, if any.
        /// </summary>
        public static Func<DynamicVar, IHoverTip>? Get(DynamicVar dynamicVar)
        {
            ArgumentNullException.ThrowIfNull(dynamicVar);
            return TooltipFactories[dynamicVar];
        }

        /// <summary>
        ///     Invokes the registered factory for <paramref name="dynamicVar" />.
        /// </summary>
        public static IHoverTip? Create(DynamicVar dynamicVar)
        {
            ArgumentNullException.ThrowIfNull(dynamicVar);
            var factory = Get(dynamicVar);
            return factory?.Invoke(dynamicVar);
        }

        /// <summary>
        ///     Copies the tooltip factory from <paramref name="source" /> to <paramref name="destination" /> when present.
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
