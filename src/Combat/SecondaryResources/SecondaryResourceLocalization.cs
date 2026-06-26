using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SmartFormat.Core.Extensions;
using STS2RitsuLib.Localization.SmartFormat;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Dynamic variable that carries the secondary-resource id used by <see cref="SecondaryResourceIconsFormatter" />.
    ///     携带次级资源 id 的动态变量，供 <see cref="SecondaryResourceIconsFormatter" /> 使用。
    /// </summary>
    public class SecondaryResourceVar : DynamicVar
    {
        /// <summary>
        ///     Creates a secondary-resource dynamic variable.
        ///     创建一个次级资源动态变量。
        /// </summary>
        public SecondaryResourceVar(string name, string resourceId, decimal baseValue)
            : base(name, baseValue)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            ResourceId = resourceId.Trim();
        }

        /// <summary>
        ///     Full secondary-resource compound id.
        ///     完整的次级资源 compound id。
        /// </summary>
        public string ResourceId { get; }
    }

    /// <summary>
    ///     Factory helpers for secondary-resource localization variables.
    ///     次级资源本地化变量的工厂辅助工具。
    /// </summary>
    public static class SecondaryResourceVars
    {
        /// <summary>
        ///     Creates a secondary-resource variable from a full resource compound id.
        ///     使用完整资源 compound id 创建次级资源变量。
        /// </summary>
        public static SecondaryResourceVar For(string name, string resourceId, decimal baseValue)
        {
            return new(name, resourceId, baseValue);
        }

        /// <summary>
        ///     Creates a secondary-resource variable from a mod id and local resource id.
        ///     使用 mod id 和本地资源 id 创建次级资源变量。
        /// </summary>
        public static SecondaryResourceVar ForLocal(
            string name,
            string modId,
            string localId,
            decimal baseValue)
        {
            return new(name, ModSecondaryResourceRegistry.GetResourceId(modId, localId), baseValue);
        }
    }

    /// <summary>
    ///     SmartFormat source for the fixed secondary-resource localization marker.
    ///     固定次级资源本地化 marker 的 SmartFormat source。
    /// </summary>
    public sealed class SecondaryResourceLocStringSource : ISource
    {
        /// <summary>
        ///     Fixed selector used by localization JSON, for example
        ///     <c>{secondaryResource:secondaryResourceIcons(charge,1)}</c>.
        ///     本地化 JSON 使用的固定 selector，例如
        ///     <c>{secondaryResource:secondaryResourceIcons(charge,1)}</c>。
        /// </summary>
        public const string SelectorName = "secondaryResource";

        /// <inheritdoc />
        public bool TryEvaluateSelector(ISelectorInfo selectorInfo)
        {
            ArgumentNullException.ThrowIfNull(selectorInfo);

            if (selectorInfo.SelectorIndex != 0
                || !StringComparer.Ordinal.Equals(selectorInfo.SelectorText, SelectorName))
                return false;

            selectorInfo.Result = SecondaryResourceLocStringMarker.Instance;
            return true;
        }
    }

    /// <summary>
    ///     SmartFormat formatter for secondary-resource rich-text icons.
    ///     次级资源富文本图标的 SmartFormat formatter。
    /// </summary>
    public sealed class SecondaryResourceIconsFormatter : IFormatter
    {
        /// <inheritdoc />
        public string Name
        {
            get => "secondaryResourceIcons";
            set => throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool CanAutoDetect { get; set; }

        /// <inheritdoc />
        public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
        {
            if (!TryResolve(formattingInfo, out var resourceId, out var amount, out var dynamicVar))
                return false;

            if (!SecondaryResourceText.TryGetIconTag(resourceId, out var iconTag))
                throw new LocException($"Unknown secondary resource icon id='{resourceId}'");

            var text = amount is > 0 and < 4
                ? string.Concat(Enumerable.Repeat(iconTag, amount))
                : dynamicVar == null
                    ? $"{amount}{iconTag}"
                    : dynamicVar.ToHighlightedString(false) + iconTag;

            formattingInfo.Write(text);
            return true;
        }

        private static bool TryResolve(
            IFormattingInfo formattingInfo,
            out string resourceId,
            out int amount,
            out DynamicVar? dynamicVar)
        {
            resourceId = string.Empty;
            amount = 0;
            dynamicVar = null;

            var options = formattingInfo.FormatterOptions?.Trim() ?? string.Empty;
            switch (formattingInfo.CurrentValue)
            {
                case SecondaryResourceLocStringMarker:
                    return TryResolveMarkerOptions(formattingInfo.FormatterOptions, out resourceId, out amount);
                case SecondaryResourceVar secondaryResourceVar:
                    resourceId = secondaryResourceVar.ResourceId;
                    amount = Convert.ToInt32(secondaryResourceVar.PreviewValue);
                    dynamicVar = secondaryResourceVar;
                    return true;
                case DynamicVar value:
                    if (string.IsNullOrWhiteSpace(options))
                        return false;

                    resourceId = options;
                    amount = Convert.ToInt32(value.PreviewValue);
                    dynamicVar = value;
                    return true;
                case SecondaryResourceDefinition definition:
                    resourceId = definition.Id;
                    amount = TryParseAmount(options, out var definitionAmount) ? definitionAmount : 1;
                    return true;
                case string value:
                    resourceId = value;
                    amount = TryParseAmount(options, out var stringAmount) ? stringAmount : 1;
                    return true;
                case decimal value:
                    if (string.IsNullOrWhiteSpace(options))
                        return false;

                    resourceId = options;
                    amount = (int)value;
                    return true;
                case int value:
                    if (string.IsNullOrWhiteSpace(options))
                        return false;

                    resourceId = options;
                    amount = value;
                    return true;
                case SecondaryResourcePaymentLine line:
                    resourceId = line.ResourceId;
                    amount = line.CostsX ? line.Value : line.Cost;
                    return true;
                default:
                    throw new LocException(
                        $"Unknown value='{formattingInfo.CurrentValue}' type={formattingInfo.CurrentValue?.GetType()}");
            }
        }

        private static bool TryParseAmount(string value, out int amount)
        {
            if (int.TryParse(value, out amount))
            {
                amount = Math.Max(0, amount);
                return true;
            }

            amount = 0;
            return false;
        }

        private static bool TryResolveMarkerOptions(string? options, out string resourceId, out int amount)
        {
            resourceId = string.Empty;
            amount = 1;

            var parts = (options ?? string.Empty)
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length is < 1 or > 2)
                return false;

            resourceId = parts[0];
            return parts.Length != 2 || TryParseAmount(parts[1], out amount);
        }
    }

    internal sealed class SecondaryResourceLocStringMarker
    {
        public static readonly SecondaryResourceLocStringMarker Instance = new();

        private SecondaryResourceLocStringMarker()
        {
        }
    }

    /// <summary>
    ///     Text helpers for secondary-resource rich-text icons.
    ///     次级资源富文本图标的文本辅助工具。
    /// </summary>
    public static class SecondaryResourceText
    {
        /// <summary>
        ///     Returns the rich-text icon tag for a registered secondary resource.
        ///     返回已注册次级资源的富文本图标标签。
        /// </summary>
        public static string GetIconTag(string resourceId)
        {
            return TryGetIconTag(resourceId, out var iconTag)
                ? iconTag
                : throw new KeyNotFoundException($"Secondary resource is not registered or has no icon: {resourceId}");
        }

        /// <summary>
        ///     Attempts to return the rich-text icon tag for a registered secondary resource.
        ///     尝试返回已注册次级资源的富文本图标标签。
        /// </summary>
        public static bool TryGetIconTag(string resourceId, out string iconTag)
        {
            iconTag = string.Empty;
            if (!TryResolveDefinition(resourceId, out var definition))
                return false;

            var path = definition.SmallIconPath ?? definition.LargeIconPath;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            iconTag = $"[img]{path}[/img]";
            return true;
        }

        /// <summary>
        ///     Returns a title LocString for the resource when the effective key exists.
        ///     当资源的实际标题 key 存在时返回标题 LocString。
        /// </summary>
        public static LocString? GetTitle(SecondaryResourceDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);
            return TryGetLocString(definition.EffectiveLocTable, definition.EffectiveTitleKey);
        }

        /// <summary>
        ///     Returns a title LocString with secondary-resource amount variables when the effective key exists.
        ///     当资源的实际标题 key 存在时，返回带次级资源数量变量的标题 LocString。
        /// </summary>
        public static LocString? GetTitle(
            SecondaryResourceDefinition definition,
            int amount,
            int? maxAmount = null)
        {
            var title = GetTitle(definition);
            AddAmountVariables(title, amount, maxAmount);
            return title;
        }

        /// <summary>
        ///     Returns the formatted resource title, falling back to a readable local id.
        ///     返回格式化后的资源标题；缺失时回退为可读的本地 id。
        /// </summary>
        public static string GetTitleText(SecondaryResourceDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);
            return GetTitle(definition)?.GetFormattedText() ?? definition.EffectiveTitleKey;
        }

        /// <summary>
        ///     Returns a description LocString for the resource when the effective key exists.
        ///     当资源的实际描述 key 存在时返回描述 LocString。
        /// </summary>
        public static LocString? GetDescription(SecondaryResourceDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);
            return TryGetLocString(definition.EffectiveLocTable, definition.EffectiveDescriptionKey);
        }

        /// <summary>
        ///     Returns a description LocString with secondary-resource amount variables when the effective key exists.
        ///     当资源的实际描述 key 存在时，返回带次级资源数量变量的描述 LocString。
        /// </summary>
        public static LocString? GetDescription(
            SecondaryResourceDefinition definition,
            int amount,
            int? maxAmount = null)
        {
            var description = GetDescription(definition);
            AddAmountVariables(description, amount, maxAmount);
            return description;
        }

        /// <summary>
        ///     Returns the formatted resource description, falling back to the full resource id.
        ///     返回格式化后的资源描述；缺失时回退为完整资源 id。
        /// </summary>
        public static string GetDescriptionText(SecondaryResourceDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);
            return GetDescription(definition)?.GetFormattedText() ?? definition.EffectiveDescriptionKey;
        }

        private static bool TryResolveDefinition(string resourceId, out SecondaryResourceDefinition definition)
        {
            definition = null!;
            if (string.IsNullOrWhiteSpace(resourceId))
                return false;

            var id = resourceId.Trim();
            if (ModSecondaryResourceRegistry.TryGet(id, out definition))
                return true;

            var matches = ModSecondaryResourceRegistry.GetDefinitionsSnapshot()
                .Where(candidate => string.Equals(candidate.LocalId, id, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            if (matches.Length != 1)
                return false;

            definition = matches[0];
            return true;
        }

        private static LocString? TryGetLocString(string table, string key)
        {
            try
            {
                return LocManager.Instance.GetTable(table).GetLocString(key);
            }
            catch
            {
                return null;
            }
        }

        private static void AddAmountVariables(LocString? locString, int amount, int? maxAmount)
        {
            if (locString == null)
                return;

            locString.Add("Amount", amount);
            locString.Add("HasMaxAmount", maxAmount.HasValue);
            if (maxAmount.HasValue)
                locString.Add("MaxAmount", maxAmount.Value);
        }
    }

    internal static class SecondaryResourceLocalizationBootstrap
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            var registry = ModSmartFormatExtensionRegistry.For(Const.ModId);
            registry.RegisterSource<SecondaryResourceLocStringSource>();
            registry.Register<SecondaryResourceIconsFormatter>();
        }
    }
}
