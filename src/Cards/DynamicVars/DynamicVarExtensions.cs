using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     Extension helpers for binding tooltips to <see cref="DynamicVar" /> instances and reading
    ///     <see cref="DynamicVarSet" /> values.
    ///     用于将工具提示绑定到 <see cref="DynamicVar" /> 实例并读取 <see cref="DynamicVarSet" /> 值的扩展辅助方法。
    /// </summary>
    public static class DynamicVarExtensions
    {
        /// <summary>
        ///     Registers a factory that builds a hover tip for this variable (see
        ///     <see cref="DynamicVarTooltipRegistry" />).
        ///     注册一个为此变量构建悬停提示的工厂（见 <see cref="DynamicVarTooltipRegistry" />）。
        /// </summary>
        public static DynamicVar WithTooltip(this DynamicVar dynamicVar, Func<DynamicVar, IHoverTip> tooltipFactory)
        {
            ArgumentNullException.ThrowIfNull(dynamicVar);
            ArgumentNullException.ThrowIfNull(tooltipFactory);
            DynamicVarTooltipRegistry.Set(dynamicVar, tooltipFactory);
            return dynamicVar;
        }

        /// <summary>
        ///     Registers a localized <see cref="HoverTip" /> from table keys, optionally with a separate description
        ///     table/key and icon path.
        ///     根据表 key 注册本地化 <see cref="HoverTip" />，可选指定单独的描述表/key 和图标路径。
        /// </summary>
        public static DynamicVar WithTooltip(this DynamicVar dynamicVar, string titleTable,
            string titleKey,
            string? descriptionTable = null,
            string? descriptionKey = null,
            string? iconPath = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(titleTable);
            ArgumentException.ThrowIfNullOrWhiteSpace(titleKey);

            var resolvedDescriptionTable = descriptionTable ?? titleTable;
            var resolvedDescriptionKey =
                descriptionKey ?? titleKey.Replace(".title", ".description", StringComparison.Ordinal);

            return dynamicVar.WithTooltip(var =>
            {
                var title = new LocString(titleTable, titleKey);
                var description = new LocString(resolvedDescriptionTable, resolvedDescriptionKey);
                title.Add(var);
                description.Add(var);

                Texture2D? icon = null;
                if (!string.IsNullOrWhiteSpace(iconPath) && ResourceLoader.Exists(iconPath))
                    icon = ResourceLoader.Load<Texture2D>(iconPath);

                return new HoverTip(title, description, icon);
            });
        }

        /// <summary>
        ///     Shorthand for <c>static_hover_tips</c> entries sharing <paramref name="entryPrefix" />.title and
        ///     .description keys.
        ///     <c>static_hover_tips</c> 条目的简写形式，共用 <paramref name="entryPrefix" />.title 和 .description key。
        /// </summary>
        public static DynamicVar WithSharedTooltip(this DynamicVar dynamicVar, string entryPrefix,
            string? iconPath = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entryPrefix);
            return dynamicVar.WithTooltip("static_hover_tips", $"{entryPrefix}.title", "static_hover_tips",
                $"{entryPrefix}.description", iconPath);
        }

        /// <summary>
        ///     Builds a hover tip using the registry factory for this variable, if any.
        ///     使用此变量的注册表工厂构建悬停提示（如果存在）。
        /// </summary>
        public static IHoverTip? CreateHoverTip(this DynamicVar dynamicVar)
        {
            return DynamicVarTooltipRegistry.Create(dynamicVar);
        }

        /// <summary>
        ///     Reads an integer dynamic var, or <paramref name="defaultValue" /> when missing.
        ///     读取整数动态变量；缺失时返回 <paramref name="defaultValue" />。
        /// </summary>
        public static int GetIntOrDefault(this DynamicVarSet dynamicVars, string key, int defaultValue = 0)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return dynamicVars.TryGetValue(key, out var value) ? value.IntValue : defaultValue;
        }

        /// <summary>
        ///     Reads the base numeric value for <paramref name="key" />, or <paramref name="defaultValue" /> when
        ///     missing.
        ///     读取 <paramref name="key" /> 的基础数值；缺失时返回 <paramref name="defaultValue" />。
        /// </summary>
        public static decimal GetValueOrDefault(this DynamicVarSet dynamicVars, string key, decimal defaultValue = 0m)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return dynamicVars.TryGetValue(key, out var value) ? value.BaseValue : defaultValue;
        }

        /// <summary>
        ///     Returns whether the numeric value for <paramref name="key" /> is strictly greater than zero.
        ///     返回 <paramref name="key" /> 的数值是否严格大于零。
        /// </summary>
        public static bool HasPositiveValue(this DynamicVarSet dynamicVars, string key)
        {
            return dynamicVars.GetValueOrDefault(key) > 0m;
        }

        /// <summary>
        ///     Computes the current value of a <see cref="ComputedDynamicVar" />.
        ///     Returns <paramref name="defaultValue" /> when <paramref name="key" /> is missing or the variable is not
        ///     a <see cref="ComputedDynamicVar" />. Optionally accepts <paramref name="target" /> for target-aware
        ///     computation.
        ///     计算指定 ID 的 <see cref="ComputedDynamicVar" /> 的当前值。
        ///     <paramref name="key" /> 不存在或变量类型不匹配时返回 <paramref name="defaultValue" />。可提供
        ///     <paramref name="target" /> 用于目标感知计算。
        /// </summary>
        public static decimal ComputeDynamicValue(this DynamicVarSet dynamicVars, string key, decimal defaultValue = 0m, Creature? target = null)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return dynamicVars.TryGetValue(key, out var value) && value is ComputedDynamicVar cv
                ? cv.Calculate(target) : defaultValue;
        }

        /// <summary>
        ///     Computes the current value of a <see cref="ComputedEnergyVar" />.
        ///     Returns <paramref name="defaultValue" /> when <paramref name="key" /> is missing or the variable is not
        ///     a <see cref="ComputedEnergyVar" />. Optionally accepts <paramref name="target" /> for target-aware
        ///     computation.
        ///     计算指定 ID 的 <see cref="ComputedEnergyVar" /> 的当前值。
        ///     <paramref name="key" /> 不存在或变量类型不匹配时返回 <paramref name="defaultValue" />。可提供
        ///     <paramref name="target" /> 用于目标感知计算。
        /// </summary>
        public static decimal ComputeEnergyValue(this DynamicVarSet dynamicVars, string key, decimal defaultValue = 0m, Creature? target = null)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return dynamicVars.TryGetValue(key, out var value) && value is ComputedEnergyVar cv
                ? cv.Calculate(target) : defaultValue;
        }

        /// <summary>
        ///     Computes the current value of a <see cref="ComputedPowerVar{T}" />.
        ///     Returns <paramref name="defaultValue" /> when <paramref name="key" /> is missing or the variable is not
        ///     a <see cref="ComputedPowerVar{T}" />. Optionally accepts <paramref name="target" /> for target-aware
        ///     computation.
        ///     计算指定 ID 的 <see cref="ComputedPowerVar{T}" /> 的当前值。
        ///     <paramref name="key" /> 不存在或变量类型不匹配时返回 <paramref name="defaultValue" />。可提供
        ///     <paramref name="target" /> 用于目标感知计算。
        /// </summary>
        public static decimal ComputePowerValue<T>(this DynamicVarSet dynamicVars, string key, decimal defaultValue = 0m, Creature? target = null) where T : PowerModel
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return dynamicVars.TryGetValue(key, out var value) && value is ComputedPowerVar<T> cv
                ? cv.Calculate(target) : defaultValue;
        }

        /// <summary>
        ///     Computes the current value of a <see cref="ComputedStarsVar" />.
        ///     Returns <paramref name="defaultValue" /> when <paramref name="key" /> is missing or the variable is not
        ///     a <see cref="ComputedStarsVar" />. Optionally accepts <paramref name="target" /> for target-aware
        ///     computation.
        ///     计算指定 ID 的 <see cref="ComputedStarsVar" /> 的当前值。
        ///     <paramref name="key" /> 不存在或变量类型不匹配时返回 <paramref name="defaultValue" />。可提供
        ///     <paramref name="target" /> 用于目标感知计算。
        /// </summary>
        public static decimal ComputeStarsValue(this DynamicVarSet dynamicVars, string key, decimal defaultValue = 0m, Creature? target = null)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return dynamicVars.TryGetValue(key, out var value) && value is ComputedStarsVar cv
                ? cv.Calculate(target) : defaultValue;
        }
    }
}
