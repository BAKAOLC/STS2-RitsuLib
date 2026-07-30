using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SmartFormat.Core.Extensions;
using STS2RitsuLib.Localization.SmartFormat;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Carries a secondary-resource ID and numeric value for
    ///         <see cref="SecondaryResourceIconsFormatter" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="SecondaryResourceIconsFormatter" /> 携带次级资源 ID 和数值。
    ///     </para>
    /// </summary>
    public class SecondaryResourceVar : DynamicVar
    {
        /// <summary>
        ///     <para xml:lang="en">Initializes a secondary-resource dynamic variable.</para>
        ///     <para xml:lang="zh-CN">初始化次级资源动态变量。</para>
        /// </summary>
        public SecondaryResourceVar(string name, string resourceId, decimal baseValue)
            : base(name, baseValue)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            ResourceId = resourceId.Trim();
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the full secondary-resource ID.</para>
        ///     <para xml:lang="zh-CN">获取完整次级资源 ID。</para>
        /// </summary>
        public string ResourceId { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">Creates secondary-resource localization variables.</para>
    ///     <para xml:lang="zh-CN">创建次级资源本地化变量。</para>
    /// </summary>
    public static class SecondaryResourceVars
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a variable from a full resource ID.</para>
        ///     <para xml:lang="zh-CN">使用完整资源 ID 创建变量。</para>
        /// </summary>
        public static SecondaryResourceVar For(string name, string resourceId, decimal baseValue)
        {
            return new(name, resourceId, baseValue);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a variable from a mod ID and mod-local resource ID.</para>
        ///     <para xml:lang="zh-CN">使用模组 ID 和模组内资源 ID 创建变量。</para>
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
    ///     <para xml:lang="en">Provides the fixed secondary-resource selector to SmartFormat.</para>
    ///     <para xml:lang="zh-CN">向 SmartFormat 提供固定的次级资源选择器。</para>
    /// </summary>
    public sealed class SecondaryResourceLocStringSource : ISource
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         The selector used by localization JSON, for example
        ///         <c>{secondaryResource:secondaryResourceIcons(charge,1)}</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         本地化 JSON 使用的选择器，例如
        ///         <c>{secondaryResource:secondaryResourceIcons(charge,1)}</c>。
        ///     </para>
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
    ///     <para xml:lang="en">Formats secondary-resource rich-text icons for SmartFormat.</para>
    ///     <para xml:lang="zh-CN">为 SmartFormat 格式化次级资源富文本图标。</para>
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
    ///     <para xml:lang="en">Provides localized text and rich-text icons for secondary resources.</para>
    ///     <para xml:lang="zh-CN">提供次级资源的本地化文本和富文本图标。</para>
    /// </summary>
    public static class SecondaryResourceText
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the rich-text icon tag for a registered resource.</para>
        ///     <para xml:lang="zh-CN">获取已注册资源的富文本图标标签。</para>
        /// </summary>
        public static string GetIconTag(string resourceId)
        {
            return TryGetIconTag(resourceId, out var iconTag)
                ? iconTag
                : throw new KeyNotFoundException($"Secondary resource is not registered or has no icon: {resourceId}");
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to get the rich-text icon tag for a registered resource.</para>
        ///     <para xml:lang="zh-CN">尝试获取已注册资源的富文本图标标签。</para>
        /// </summary>
        public static bool TryGetIconTag(string resourceId, out string iconTag)
        {
            iconTag = string.Empty;
            if (!TryResolveDefinition(resourceId, out var definition))
                return false;

            var path = definition.SmallIconPath ?? definition.LargeIconPath;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            iconTag = $"[img]{path.Trim()}[/img]";
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the resource title when its effective localization key exists.</para>
        ///     <para xml:lang="zh-CN">实际本地化键存在时获取资源标题。</para>
        /// </summary>
        public static LocString? GetTitle(SecondaryResourceDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);
            return TryGetLocString(definition.EffectiveLocTable, definition.EffectiveTitleKey);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the resource title with amount variables.</para>
        ///     <para xml:lang="zh-CN">获取带有数量变量的资源标题。</para>
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
        ///     <para xml:lang="en">Gets formatted title text, falling back to the effective localization key.</para>
        ///     <para xml:lang="zh-CN">获取格式化标题文本；没有文本时回退到实际本地化键。</para>
        /// </summary>
        public static string GetTitleText(SecondaryResourceDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);
            return GetTitle(definition)?.GetFormattedText() ?? definition.EffectiveTitleKey;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the resource description when its effective localization key exists.</para>
        ///     <para xml:lang="zh-CN">实际本地化键存在时获取资源说明。</para>
        /// </summary>
        public static LocString? GetDescription(SecondaryResourceDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);
            return TryGetLocString(definition.EffectiveLocTable, definition.EffectiveDescriptionKey);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the resource description with amount variables.</para>
        ///     <para xml:lang="zh-CN">获取带有数量变量的资源说明。</para>
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
        ///     <para xml:lang="en">Gets formatted description text, falling back to the effective localization key.</para>
        ///     <para xml:lang="zh-CN">获取格式化说明文本；没有文本时回退到实际本地化键。</para>
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
