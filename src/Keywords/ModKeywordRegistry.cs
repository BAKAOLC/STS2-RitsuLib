using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Content;
using STS2RitsuLib.Utils;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a per-mod registration surface for keywords. Definitions are stored in one global map keyed by
    ///         trimmed, case-insensitive IDs. Prefer <c>RegisterOwned</c> or
    ///         <c>RegisterCardKeywordOwnedByLocNamespace</c> so IDs follow the same mod-qualified naming convention as
    ///         fixed model entries.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供按模组划分的关键词注册入口。所有定义统一存放在全局映射中；ID 会移除首尾空白，比较时
    ///         不区分大小写。应优先使用 <c>RegisterOwned</c> 或
    ///         <c>RegisterCardKeywordOwnedByLocNamespace</c>，使 ID 遵循与固定模型条目相同的模组限定命名规则。
    ///     </para>
    /// </summary>
    public sealed class ModKeywordRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModKeywordRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ModKeywordDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<CardKeyword, ModKeywordDefinition> DefinitionsByCardKeyword = [];

        private static int _hasCardDescriptionPlacements;

        private readonly Logger _logger;

        private readonly string _modId;
        private string? _freezeReason;

        private ModKeywordRegistry(string modId)
        {
            _modId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the framework has frozen keyword registration alongside content and timeline
        ///         registration during model initialization.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取框架是否已在模型初始化期间随内容和时间线注册表一同冻结关键词注册。
        ///     </para>
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets <see cref="IsFrozen" /> as a <see cref="KeywordRegistrationState" /> value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         以 <see cref="KeywordRegistrationState" /> 值的形式获取 <see cref="IsFrozen" /> 状态。
        ///     </para>
        /// </summary>
        public static KeywordRegistrationState State => IsFrozen
            ? KeywordRegistrationState.Frozen
            : KeywordRegistrationState.Open;

        internal static bool HasCardDescriptionPlacements =>
            Volatile.Read(ref _hasCardDescriptionPlacements) != 0;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the singleton registry for <paramref name="modId" />, creating it on first use.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="modId" /> 对应的单例注册表；首次使用时创建。
        ///     </para>
        /// </summary>
        public static ModKeywordRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            modId = modId.Trim();

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var existing))
                    return existing;

                var created = new ModKeywordRegistry(modId);
                Registries[modId] = created;
                return created;
            }
        }

        internal static void FreezeRegistrations(string reason)
        {
            ModKeywordRegistry[] registriesSnapshot;
            lock (SyncRoot)
            {
                if (IsFrozen)
                    return;

                IsFrozen = true;
                foreach (var registry in Registries.Values)
                    registry._freezeReason = reason;

                registriesSnapshot = [.. Registries.Values];
            }

            foreach (var registry in registriesSnapshot)
                registry._logger.Info($"[Keywords] Keyword registration is now frozen ({reason}).");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get the ID of the mod that registered <paramref name="keywordId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取注册 <paramref name="keywordId" /> 的模组 ID。
        ///     </para>
        /// </summary>
        public static bool TryGetOwnerModId(string keywordId, out string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(NormalizeId(keywordId), out var def))
                {
                    modId = def.ModId;
                    return true;
                }
            }

            modId = string.Empty;
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a keyword under the mod-qualified ID that
        ///         <see cref="ModContentRegistry.GetQualifiedKeywordId" /> produces from this registry's mod ID and
        ///         <paramref name="localKeywordStem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         根据当前注册表的模组 ID 和 <paramref name="localKeywordStem" />，使用
        ///         <see cref="ModContentRegistry.GetQualifiedKeywordId" /> 生成限定 ID 并注册关键词。
        ///     </para>
        /// </summary>
        public ModKeywordDefinition RegisterOwned(
            string localKeywordStem,
            string titleTable,
            string? titleKey,
            string? descriptionTable,
            string? descriptionKey,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localKeywordStem);
            var id = ModContentRegistry.GetQualifiedKeywordId(_modId, localKeywordStem);
            return RegisterCore(
                id,
                titleTable,
                titleKey,
                descriptionTable,
                descriptionKey,
                iconPath,
                cardDescriptionPlacement,
                includeInCardHoverTip);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers an owned keyword using the default title and description key rules from the legacy
        ///         <c>Register(string, titleTable, ...)</c> overload.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用旧版 <c>Register(string, titleTable, ...)</c> 重载的默认标题和描述键规则注册归属当前模组的
        ///         关键词。
        ///     </para>
        /// </summary>
        public ModKeywordDefinition RegisterOwned(
            string localKeywordStem,
            string titleTable = "card_keywords",
            string? titleKey = null,
            string? descriptionTable = null,
            string? descriptionKey = null,
            string? iconPath = null)
        {
            return RegisterOwned(
                localKeywordStem,
                titleTable,
                titleKey,
                descriptionTable,
                descriptionKey,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a <c>card_keywords</c> entry whose ID and localization-key stem are both produced by
        ///         <see cref="ModContentRegistry.GetQualifiedKeywordId" />. The keys are <c>{id}.title</c> and
        ///         <c>{id}.description</c> in <c>card_keywords</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册 <c>card_keywords</c> 条目，其 ID 和本地化键前缀均由
        ///         <see cref="ModContentRegistry.GetQualifiedKeywordId" /> 生成。本地化键为
        ///         <c>card_keywords</c> 表中的 <c>{id}.title</c> 和 <c>{id}.description</c>。
        ///     </para>
        /// </summary>
        public ModKeywordDefinition RegisterCardKeywordOwnedByLocNamespace(
            string localKeywordStem,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localKeywordStem);

            var id = ModContentRegistry.GetQualifiedKeywordId(_modId, localKeywordStem);

            return RegisterCore(
                id,
                "card_keywords",
                $"{id}.title",
                "card_keywords",
                $"{id}.description",
                iconPath,
                cardDescriptionPlacement,
                includeInCardHoverTip);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers an owned <c>card_keywords</c> entry using the legacy hover-tip defaults.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用旧版悬停提示默认值注册归属当前模组的 <c>card_keywords</c> 条目。
        ///     </para>
        /// </summary>
        public ModKeywordDefinition RegisterCardKeywordOwnedByLocNamespace(
            string localKeywordStem,
            string? iconPath = null)
        {
            return RegisterCardKeywordOwnedByLocNamespace(
                localKeywordStem,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     <para xml:lang="en">Implements keyword registration after an owned ID has been resolved.</para>
        ///     <para xml:lang="zh-CN">在解析归属当前模组的 ID 后执行关键词注册。</para>
        /// </summary>
        internal ModKeywordDefinition RegisterCore(
            string id,
            string titleTable,
            string? titleKey,
            string? descriptionTable,
            string? descriptionKey,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(titleTable);

            EnsureMutable("register keywords");

            var normalizedId = NormalizeId(id);
            var cardKeywordValue = DynamicEnumValueRegistry<CardKeyword>.Register(_modId, normalizedId).Value;
            var definition = new ModKeywordDefinition(
                _modId,
                normalizedId,
                titleTable,
                titleKey ?? $"{normalizedId}.title",
                descriptionTable ?? titleTable,
                descriptionKey ?? $"{normalizedId}.description",
                iconPath,
                cardDescriptionPlacement,
                includeInCardHoverTip)
            {
                CardKeywordValue = cardKeywordValue,
            };

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(normalizedId, out var existing))
                {
                    if (existing != definition)
                        throw new InvalidOperationException(
                            $"Keyword '{normalizedId}' is already registered by mod '{existing.ModId}' with different data; ids are global and must not be reused with conflicting definitions.");

                    return existing;
                }

                Definitions[normalizedId] = definition;
                DefinitionsByCardKeyword[cardKeywordValue] = definition;
                if (definition.CardDescriptionPlacement != ModKeywordCardDescriptionPlacement.None)
                    Volatile.Write(ref _hasCardDescriptionPlacements, 1);
            }

            _logger.Info(
                $"[Keywords] Registered keyword: {normalizedId} (CardKeyword=0x{(int)cardKeywordValue:X8})");
            return definition;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get a globally registered definition by keyword ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试按关键词 ID 获取全局注册定义。
        ///     </para>
        /// </summary>
        public static bool TryGet(string id, out ModKeywordDefinition definition)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            lock (SyncRoot)
            {
                return Definitions.TryGetValue(NormalizeId(id), out definition!);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the definition registered for <paramref name="id" />.</para>
        ///     <para xml:lang="zh-CN">获取为 <paramref name="id" /> 注册的定义。</para>
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        ///     <para xml:lang="en">No keyword with the specified ID is registered.</para>
        ///     <para xml:lang="zh-CN">未注册具有指定 ID 的关键词。</para>
        /// </exception>
        public static ModKeywordDefinition Get(string id)
        {
            return TryGet(id, out var definition)
                ? definition
                : throw new KeyNotFoundException($"Keyword '{NormalizeId(id)}' is not registered.");
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the registered mod keyword definition for <paramref name="value" />.</para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="value" /> 对应的已注册模组关键词定义。</para>
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        ///     <para xml:lang="en">The value is not a registered mod keyword.</para>
        ///     <para xml:lang="zh-CN">该值不是已注册的模组关键词。</para>
        /// </exception>
        public static ModKeywordDefinition Get(CardKeyword value)
        {
            return TryGetByCardKeyword(value, out var definition)
                ? definition
                : throw new KeyNotFoundException(
                    $"CardKeyword '0x{(int)value:X8}' is not a registered mod keyword.");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to reverse-map <paramref name="value" /> to the registered mod keyword definition that minted
        ///         it. Native <see cref="CardKeyword" /> values and unregistered numeric values do not resolve.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试将 <paramref name="value" /> 反向映射到生成该值的已注册模组关键词定义。原版
        ///         <see cref="CardKeyword" /> 值和未注册的数值不会匹配。
        ///     </para>
        /// </summary>
        public static bool TryGetByCardKeyword(CardKeyword value, out ModKeywordDefinition definition)
        {
            lock (SyncRoot)
            {
                return DefinitionsByCardKeyword.TryGetValue(value, out definition!);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether <paramref name="value" /> is a registered mod keyword rather than a native
        ///         <see cref="CardKeyword" /> value or an unknown numeric value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="value" /> 是否为已注册的模组关键词，而非原版
        ///         <see cref="CardKeyword" /> 值或未知数值。
        ///     </para>
        /// </summary>
        public static bool IsModCardKeyword(CardKeyword value)
        {
            lock (SyncRoot)
            {
                return DefinitionsByCardKeyword.ContainsKey(value);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get the deterministic <see cref="CardKeyword" /> value for <paramref name="id" />. The ID
        ///         does not need to be registered, but only registered IDs provide keyword metadata. Use the returned
        ///         enum value with native APIs such as <c>CardModel.AddKeyword</c> and <c>Keywords.Contains</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取 <paramref name="id" /> 对应的确定性 <see cref="CardKeyword" /> 值。ID 无需预先注册，
        ///         但只有已注册 ID 才能提供关键词元数据。可将返回的枚举值传给
        ///         <c>CardModel.AddKeyword</c>、<c>Keywords.Contains</c> 等原版 API。
        ///     </para>
        /// </summary>
        public static bool TryGetCardKeyword(string id, out CardKeyword value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            try
            {
                value = DynamicEnumValueRegistry<CardKeyword>.GetValue(id);
                return true;
            }
            catch (InvalidOperationException)
            {
                value = CardKeyword.None;
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to resolve a string to a <see cref="CardKeyword" />. Registered mod IDs take precedence,
        ///         followed by native enum names or numeric literals; any other ID is used to compute a deterministic
        ///         dynamic value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试将字符串解析为 <see cref="CardKeyword" />。解析顺序依次为已注册的模组关键词 ID、原版枚举名称
        ///         或数字字面量，最后根据其他 ID 计算确定性的动态值。
        ///     </para>
        /// </summary>
        public static bool TryResolveCardKeyword(string idOrEnumName, out CardKeyword value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(idOrEnumName);

            if (!TryGet(idOrEnumName, out var definition))
                return Enum.TryParse(idOrEnumName.Trim(), true, out value) ||
                       TryGetCardKeyword(idOrEnumName, out value);
            value = definition.CardKeywordValue;
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the deterministic <see cref="CardKeyword" /> value for <paramref name="id" />. The ID does not
        ///         need to be registered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="id" /> 对应的确定性 <see cref="CardKeyword" /> 值。ID 无需预先注册。
        ///     </para>
        /// </summary>
        public static CardKeyword GetCardKeyword(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return DynamicEnumValueRegistry<CardKeyword>.GetValue(id);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get the registered string ID associated with <paramref name="value" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取与 <paramref name="value" /> 关联的已注册字符串 ID。
        ///     </para>
        /// </summary>
        public static bool TryGetId(CardKeyword value, out string id)
        {
            lock (SyncRoot)
            {
                if (DefinitionsByCardKeyword.TryGetValue(value, out var def))
                {
                    id = def.Id;
                    return true;
                }
            }

            id = string.Empty;
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a snapshot of all registered keyword definitions in stable ID order.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回所有已注册关键词定义的快照，并按 ID 进行确定性排序。
        ///     </para>
        /// </summary>
        public static ModKeywordDefinition[] GetDefinitionsSnapshot()
        {
            lock (SyncRoot)
            {
                return
                [
                    .. Definitions.Values
                        .OrderBy(def => def.Id, StringComparer.Ordinal),
                ];
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a native <see cref="IHoverTip" /> for <paramref name="id" /> using its registered title,
        ///         description, and icon.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用注册的标题、描述和图标，为 <paramref name="id" /> 创建原版 <see cref="IHoverTip" />。
        ///     </para>
        /// </summary>
        public static IHoverTip CreateHoverTip(string id)
        {
            var definition = Get(id);
            Texture2D? icon = null;

            if (!string.IsNullOrWhiteSpace(definition.IconPath) && ResourceLoader.Exists(definition.IconPath))
                icon = ResourceLoader.Load<Texture2D>(definition.IconPath);

            return new HoverTip(GetTitle(id), GetDescription(id), icon);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a native <see cref="IHoverTip" /> for the registered mod keyword
        ///         <paramref name="value" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为已注册的模组关键词 <paramref name="value" /> 创建原版 <see cref="IHoverTip" />。
        ///     </para>
        /// </summary>
        public static IHoverTip CreateHoverTip(CardKeyword value)
        {
            return CreateHoverTip(Get(value).Id);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the keyword title as a <see cref="LocString" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取关键词标题的 <see cref="LocString" />。
        ///     </para>
        /// </summary>
        public static LocString GetTitle(string id)
        {
            var definition = Get(id);
            return new(definition.TitleTable, definition.TitleKey);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the registered mod keyword title as a <see cref="LocString" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已注册模组关键词标题的 <see cref="LocString" />。
        ///     </para>
        /// </summary>
        public static LocString GetTitle(CardKeyword value)
        {
            return GetTitle(Get(value).Id);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the keyword description as a <see cref="LocString" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取关键词描述的 <see cref="LocString" />。
        ///     </para>
        /// </summary>
        public static LocString GetDescription(string id)
        {
            var definition = Get(id);
            return new(definition.DescriptionTable, definition.DescriptionKey);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the registered mod keyword description as a <see cref="LocString" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已注册模组关键词描述的 <see cref="LocString" />。
        ///     </para>
        /// </summary>
        public static LocString GetDescription(CardKeyword value)
        {
            return GetDescription(Get(value).Id);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the keyword's inline card BBCode: a gold title followed by the localized keyword period.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取关键词的内联卡牌 BBCode，即金色标题及其后的本地化关键词句号。
        ///     </para>
        /// </summary>
        public static string GetCardText(string id)
        {
            var period = new LocString("card_keywords", "PERIOD");
            return "[gold]" + GetTitle(id).GetFormattedText() + "[/gold]" + period.GetRawText();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the registered mod keyword's inline card BBCode: a gold title followed by the localized keyword
        ///         period.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已注册模组关键词的内联卡牌 BBCode，即金色标题及其后的本地化关键词句号。
        ///     </para>
        /// </summary>
        public static string GetCardText(CardKeyword value)
        {
            return GetCardText(Get(value).Id);
        }

        private void EnsureMutable(string operation)
        {
            if (!IsFrozen)
                return;

            throw new InvalidOperationException(
                $"Cannot {operation} after keyword registration has been frozen ({_freezeReason ?? "unknown"}). " +
                "Register keywords from your mod initializer before model initialization.");
        }

        private static string NormalizeId(string id)
        {
            return id.Trim();
        }
    }
}
