using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     Extension helpers for binding tooltips to <see cref="DynamicVar" /> instances and reading
    ///     <see cref="DynamicVarSet" /> values.
    /// </summary>
    public static class DynamicVarExtensions
    {
        /// <summary>
        ///     Registers a factory that builds a hover tip for this variable (see
        ///     <see cref="DynamicVarTooltipRegistry" />).
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
        /// </summary>
        public static IHoverTip? CreateHoverTip(this DynamicVar dynamicVar)
        {
            return DynamicVarTooltipRegistry.Create(dynamicVar);
        }

        /// <summary>
        ///     Reads an integer dynamic var, or <paramref name="defaultValue" /> when missing.
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
        /// </summary>
        public static decimal GetValueOrDefault(this DynamicVarSet dynamicVars, string key, decimal defaultValue = 0m)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return dynamicVars.TryGetValue(key, out var value) ? value.BaseValue : defaultValue;
        }

        /// <summary>
        ///     Returns whether the numeric value for <paramref name="key" /> is strictly greater than zero.
        /// </summary>
        public static bool HasPositiveValue(this DynamicVarSet dynamicVars, string key)
        {
            return dynamicVars.GetValueOrDefault(key) > 0m;
        }
    }
}
