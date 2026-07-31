using System.Diagnostics.CodeAnalysis;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides extensions for attaching tooltips to <see cref="DynamicVar" /> instances and reading
    ///         <see cref="DynamicVarSet" /> values.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供为 <see cref="DynamicVar" /> 实例关联工具提示以及读取 <see cref="DynamicVarSet" /> 值的扩展方法。
    ///     </para>
    /// </summary>
    public static class DynamicVarExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">Registers a hover-tip factory for this variable.</para>
        ///     <para xml:lang="zh-CN">为此变量注册悬停提示工厂。</para>
        /// </summary>
        public static DynamicVar WithTooltip(this DynamicVar dynamicVar, Func<DynamicVar, IHoverTip> tooltipFactory)
        {
            ArgumentNullException.ThrowIfNull(dynamicVar);
            ArgumentNullException.ThrowIfNull(tooltipFactory);
            DynamicVarTooltipRegistry.Set(dynamicVar, tooltipFactory);
            return dynamicVar;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a localized <see cref="HoverTip" /> from localization-table keys, with an optional icon.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         根据本地化表键注册 <see cref="HoverTip" />，并可指定图标。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Registers a tooltip from the <c>static_hover_tips</c> table using
        ///         <c>{entryPrefix}.title</c> and <c>{entryPrefix}.description</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <c>{entryPrefix}.title</c> 和 <c>{entryPrefix}.description</c> 键，从
        ///         <c>static_hover_tips</c> 表注册工具提示。
        ///     </para>
        /// </summary>
        public static DynamicVar WithSharedTooltip(this DynamicVar dynamicVar, string entryPrefix,
            string? iconPath = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entryPrefix);
            return dynamicVar.WithTooltip("static_hover_tips", $"{entryPrefix}.title", "static_hover_tips",
                $"{entryPrefix}.description", iconPath);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a hover tip using this variable's registered factory, if any.</para>
        ///     <para xml:lang="zh-CN">使用为此变量注册的工厂创建悬停提示（如果有）。</para>
        /// </summary>
        public static IHoverTip? CreateHoverTip(this DynamicVar dynamicVar)
        {
            return DynamicVarTooltipRegistry.Create(dynamicVar);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to get a dynamic variable of type <typeparamref name="TVar" />.</para>
        ///     <para xml:lang="zh-CN">尝试获取 <typeparamref name="TVar" /> 类型的动态变量。</para>
        /// </summary>
        public static bool TryGet<TVar>(
            this DynamicVarSet dynamicVars,
            string key,
            [MaybeNullWhen(false)] out TVar value)
            where TVar : DynamicVar
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            if (dynamicVars.TryGetValue(key, out var dynamicVar) && dynamicVar is TVar typed)
            {
                value = typed;
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a required dynamic variable of type <typeparamref name="TVar" />.</para>
        ///     <para xml:lang="zh-CN">获取必需的 <typeparamref name="TVar" /> 类型动态变量。</para>
        /// </summary>
        public static TVar GetRequired<TVar>(this DynamicVarSet dynamicVars, string key)
            where TVar : DynamicVar
        {
            if (dynamicVars.TryGet<TVar>(key, out var value))
                return value;

            throw new KeyNotFoundException(
                $"Dynamic var '{key}' was missing or was not a {typeof(TVar).Name}.");
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a dynamic variable's integer value, or <paramref name="defaultValue" /> when absent.</para>
        ///     <para xml:lang="zh-CN">获取动态变量的整数值；变量不存在时返回 <paramref name="defaultValue" />。</para>
        /// </summary>
        public static int GetIntOrDefault(this DynamicVarSet dynamicVars, string key, int defaultValue = 0)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return dynamicVars.TryGetValue(key, out var value) ? value.IntValue : defaultValue;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the base value for <paramref name="key" />, or <paramref name="defaultValue" /> when
        ///         absent.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="key" /> 的基础值；变量不存在时返回 <paramref name="defaultValue" />。</para>
        /// </summary>
        public static decimal GetValueOrDefault(this DynamicVarSet dynamicVars, string key, decimal defaultValue = 0m)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return dynamicVars.TryGetValue(key, out var value) ? value.BaseValue : defaultValue;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns whether the base value for <paramref name="key" /> is greater than zero.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="key" /> 的基础值是否大于零。</para>
        /// </summary>
        public static bool HasPositiveValue(this DynamicVarSet dynamicVars, string key)
        {
            return dynamicVars.GetValueOrDefault(key) > 0m;
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to compute a RitsuLib computed dynamic variable.</para>
        ///     <para xml:lang="zh-CN">尝试计算 RitsuLib 计算型动态变量。</para>
        /// </summary>
        public static bool TryComputeValue(
            this DynamicVarSet dynamicVars,
            string key,
            out decimal value,
            Creature? target = null)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            if (dynamicVars.TryGetValue(key, out var dynamicVar) &&
                dynamicVar is IComputedDynamicVar computed)
            {
                value = computed.Calculate(target);
                return true;
            }

            value = 0m;
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">Computes a required RitsuLib computed dynamic variable.</para>
        ///     <para xml:lang="zh-CN">计算必需的 RitsuLib 计算型动态变量。</para>
        /// </summary>
        public static decimal GetComputedValue(
            this DynamicVarSet dynamicVars,
            string key,
            Creature? target = null)
        {
            if (dynamicVars.TryComputeValue(key, out var value, target))
                return value;

            throw new KeyNotFoundException(
                $"Dynamic var '{key}' was missing or did not implement {nameof(IComputedDynamicVar)}.");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Computes a computed variable, or reads a regular variable's base value. Returns
        ///         <paramref name="defaultValue" /> when the variable is absent.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         计算计算型变量；对于普通变量则读取基础值。变量不存在时返回 <paramref name="defaultValue" />。
        ///     </para>
        /// </summary>
        public static decimal EvaluateValueOrDefault(
            this DynamicVarSet dynamicVars,
            string key,
            decimal defaultValue = 0m,
            Creature? target = null)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            if (!dynamicVars.TryGetValue(key, out var dynamicVar))
                return defaultValue;

            return dynamicVar is IComputedDynamicVar computed
                ? computed.Calculate(target)
                : dynamicVar.BaseValue;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Computes a <see cref="ComputedDynamicVar" /> for <paramref name="target" />. Returns
        ///         <paramref name="defaultValue" /> when the key is absent or the variable has another type.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="target" /> 计算 <see cref="ComputedDynamicVar" />。键不存在或变量类型不匹配时
        ///         返回 <paramref name="defaultValue" />。
        ///     </para>
        /// </summary>
        public static decimal ComputeDynamicValue(this DynamicVarSet dynamicVars, string key, decimal defaultValue = 0m,
            Creature? target = null)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return dynamicVars.TryGet<ComputedDynamicVar>(key, out var value)
                ? value.Calculate(target)
                : defaultValue;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Computes a <see cref="ComputedEnergyVar" /> for <paramref name="target" />. Returns
        ///         <paramref name="defaultValue" /> when the key is absent or the variable has another type.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="target" /> 计算 <see cref="ComputedEnergyVar" />。键不存在或变量类型不匹配时
        ///         返回 <paramref name="defaultValue" />。
        ///     </para>
        /// </summary>
        public static decimal ComputeEnergyValue(this DynamicVarSet dynamicVars, string key, decimal defaultValue = 0m,
            Creature? target = null)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return dynamicVars.TryGet<ComputedEnergyVar>(key, out var value)
                ? value.Calculate(target)
                : defaultValue;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Computes a <see cref="ComputedPowerVar{T}" /> for <paramref name="target" />. Returns
        ///         <paramref name="defaultValue" /> when the key is absent or the variable has another type.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="target" /> 计算 <see cref="ComputedPowerVar{T}" />。键不存在或变量类型不匹配时
        ///         返回 <paramref name="defaultValue" />。
        ///     </para>
        /// </summary>
        public static decimal ComputePowerValue<T>(this DynamicVarSet dynamicVars, string key,
            decimal defaultValue = 0m, Creature? target = null) where T : PowerModel
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return dynamicVars.TryGet<ComputedPowerVar<T>>(key, out var value)
                ? value.Calculate(target)
                : defaultValue;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Computes a <see cref="ComputedStarsVar" /> for <paramref name="target" />. Returns
        ///         <paramref name="defaultValue" /> when the key is absent or the variable has another type.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="target" /> 计算 <see cref="ComputedStarsVar" />。键不存在或变量类型不匹配时
        ///         返回 <paramref name="defaultValue" />。
        ///     </para>
        /// </summary>
        public static decimal ComputeStarsValue(this DynamicVarSet dynamicVars, string key, decimal defaultValue = 0m,
            Creature? target = null)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return dynamicVars.TryGet<ComputedStarsVar>(key, out var value)
                ? value.Calculate(target)
                : defaultValue;
        }
    }
}
