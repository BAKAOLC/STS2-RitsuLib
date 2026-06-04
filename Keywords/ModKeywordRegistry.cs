using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Content;
using STS2RitsuLib.Utils;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Per-mod registration surface for hover-tip keywords. Definitions are stored in a single global map keyed by
    ///     normalized id; prefer <c>RegisterOwned</c> / <c>RegisterCardKeywordOwnedByLocNamespace</c> so ids stay mod-scoped
    ///     like fixed
    ///     model public entries.
    ///     悬停提示关键词的按 mod 注册入口。定义存储在按
    ///     规范化 id 索引的单一全局映射中；优先使用 <c>RegisterOwned</c>
    ///     <c>RegisterCardKeywordOwnedByLocNamespace</c>，使 id 像固定
    ///     模型公共条目一样保持在 mod 作用域内。
    /// </summary>
    public sealed class ModKeywordRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModKeywordRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ModKeywordDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<CardKeyword, ModKeywordDefinition> DefinitionsByCardKeyword = [];

        private readonly Logger _logger;

        private readonly string _modId;
        private string? _freezeReason;

        private ModKeywordRegistry(string modId)
        {
            _modId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

        /// <summary>
        ///     True after the framework freezes keyword registration (with content/timeline at model init).
        ///     框架在模型初始化时与 content/timeline 一起冻结关键词注册后为 true。
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     Convenience view of <see cref="IsFrozen" /> as <see cref="KeywordRegistrationState" />.
        ///     将 <see cref="IsFrozen" /> 作为 <see cref="KeywordRegistrationState" /> 查看时的便捷视图。
        /// </summary>
        public static KeywordRegistrationState State => IsFrozen
            ? KeywordRegistrationState.Frozen
            : KeywordRegistrationState.Open;

        /// <summary>
        ///     Returns the singleton registry for <paramref name="modId" />, creating it on first use.
        ///     返回 <paramref name="modId" /> 对应的单例注册表，首次使用时创建。
        /// </summary>
        public static ModKeywordRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

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
        ///     Resolves which mod registered <paramref name="keywordId" />, if any.
        ///     解析哪个 mod 注册了 <paramref name="keywordId" />，如果存在的话。
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
        ///     Registers a keyword with an id derived from <see cref="ModContentRegistry.GetQualifiedKeywordId" /> using
        ///     this registry’s mod id and <paramref name="localKeywordStem" />.
        ///     使用此注册表的 mod id 与 <paramref name="localKeywordStem" />，通过
        ///     <see cref="ModContentRegistry.GetQualifiedKeywordId" /> 派生出的 id 注册关键词。
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
        ///     <c>RegisterOwned</c> with default title/description key rules (same as legacy
        ///     <c>Register(string, titleTable, ...)</c>).
        ///     使用默认标题/描述 key 规则的 <c>RegisterOwned</c>（与旧版
        ///     <c>Register(string, titleTable, ...)</c> 相同）。
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
        ///     Registers a <c>card_keywords</c> entry whose id and loc stem both come from
        ///     <see cref="ModContentRegistry.GetQualifiedKeywordId" />(<paramref name="localKeywordStem" />): keys are
        ///     <c>{id}.title</c> and <c>{id}.description</c> on <c>card_keywords</c> (uppercase id).
        ///     <see cref="ModContentRegistry.GetQualifiedKeywordId" />(<paramref name="localKeywordStem" />)：
        ///     注册一个 <c>card_keywords</c> 条目，其 id 和本地化词干都来自
        ///     <see cref="ModContentRegistry.GetQualifiedKeywordId" />(<paramref name="localKeywordStem" />)：key 是
        ///     <c>card_keywords</c> 上的 <c>{id}.title</c> 和 <c>{id}.description</c>（大写 id）。
        ///     <see cref="ModContentRegistry.GetQualifiedKeywordId" />(<paramref name="localKeywordStem" />)：
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
        ///     <c>RegisterCardKeywordOwnedByLocNamespace</c> with legacy hover defaults.
        ///     使用 legacy hover 默认值的 <c>RegisterCardKeywordOwnedByLocNamespace</c>。
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
        ///     Registers a keyword with a raw global id. Prefer <c>RegisterOwned</c> to avoid cross-mod collisions.
        ///     使用 raw global id 注册 keyword。优先使用 <c>RegisterOwned</c> 以避免跨 mod 冲突。
        /// </summary>
        [Obsolete(
            "Flat keyword ids are global: they collide across mods and do not follow fixed public entry naming. Use RegisterOwned / RegisterCardKeywordOwnedByLocNamespace, or ModContentRegistry.GetQualifiedKeywordId for cross-mod references.")]
        public ModKeywordDefinition Register(
            string id,
            string titleTable,
            string? titleKey,
            string? descriptionTable,
            string? descriptionKey,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
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
        ///     Legacy <c>Register</c> signature preserved for older mods; forwards with prior hover-tip behavior.
        ///     为旧 mod 保留的 legacy <c>Register</c> 签名；以旧 hover-tip 行为转发。
        /// </summary>
        [Obsolete(
            "Flat keyword ids are global: they collide across mods and do not follow fixed public entry naming. Use RegisterOwned / RegisterCardKeywordOwnedByLocNamespace, or ModContentRegistry.GetQualifiedKeywordId for cross-mod references.")]
        public ModKeywordDefinition Register(
            string id,
            string titleTable = "card_keywords",
            string? titleKey = null,
            string? descriptionTable = null,
            string? descriptionKey = null,
            string? iconPath = null)
        {
            return RegisterCore(
                id,
                titleTable,
                titleKey,
                descriptionTable,
                descriptionKey,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     Registers a card keyword with a raw global id. Prefer <c>RegisterCardKeywordOwnedByLocNamespace</c>.
        ///     使用原始全局 id 注册卡牌关键词。优先使用 <c>RegisterCardKeywordOwnedByLocNamespace</c>。
        /// </summary>
        [Obsolete(
            "Flat keyword ids are global: they collide across mods and do not follow fixed public entry naming. Use RegisterCardKeywordOwnedByLocNamespace, or ModContentRegistry.GetQualifiedKeywordId for cross-mod references.")]
        public ModKeywordDefinition RegisterCardKeyword(
            string id,
            string? entryStem,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            var prefix = string.IsNullOrWhiteSpace(entryStem)
                ? StringHelper.Slugify(id)
                : entryStem.Trim();

            return RegisterCore(
                id,
                "card_keywords",
                $"{prefix}.title",
                "card_keywords",
                $"{prefix}.description",
                iconPath,
                cardDescriptionPlacement,
                includeInCardHoverTip);
        }

        /// <summary>
        ///     Legacy <c>RegisterCardKeyword</c> signature preserved for older mods; forwards with prior hover-tip behavior.
        ///     为旧 mod 保留的 legacy <c>RegisterCardKeyword</c> 签名；以旧 hover-tip 行为转发。
        /// </summary>
        [Obsolete(
            "Flat keyword ids are global: they collide across mods and do not follow fixed public entry naming. Use RegisterCardKeywordOwnedByLocNamespace, or ModContentRegistry.GetQualifiedKeywordId for cross-mod references.")]
        public ModKeywordDefinition RegisterCardKeyword(string id, string? entryStem = null, string? iconPath = null)
        {
            return RegisterCardKeyword(
                id,
                entryStem,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     Same as obsolete <c>Register</c> (full signature) without triggering obsolete warnings; for in-library
        ///     forwarding from manifests.
        ///     与 obsolete <c>Register</c>（完整签名）相同，但不会触发 obsolete warning；用于库内从 manifest 转发。
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
            }

            _logger.Info(
                $"[Keywords] Registered keyword: {normalizedId} (CardKeyword=0x{(int)cardKeywordValue:X8})");
            return definition;
        }

        /// <summary>
        ///     Tries to resolve a global definition by keyword id.
        ///     尝试按 keyword id 解析全局 definition。
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
        ///     Returns the definition for <paramref name="id" /> or throws <see cref="KeyNotFoundException" />.
        ///     返回 <paramref name="id" /> 的 definition，或抛出 <see cref="KeyNotFoundException" />。
        /// </summary>
        public static ModKeywordDefinition Get(string id)
        {
            return TryGet(id, out var definition)
                ? definition
                : throw new KeyNotFoundException($"Keyword '{NormalizeId(id)}' is not registered.");
        }

        /// <summary>
        ///     Reverse lookup: resolves the mod keyword <see cref="ModKeywordDefinition" /> that minted
        ///     <paramref name="value" />. Returns <c>false</c> for vanilla <see cref="CardKeyword" /> literals and
        ///     for any value that was never registered.
        ///     <c>false</c>。
        ///     反向查找：解析 minted <paramref name="value" /> 的 mod keyword
        ///     <see cref="ModKeywordDefinition" />。对原版 <see cref="CardKeyword" /> literal 和
        ///     任何从未注册的值返回 <c>false</c>。
        ///     <c>false</c>。
        /// </summary>
        public static bool TryGetByCardKeyword(CardKeyword value, out ModKeywordDefinition definition)
        {
            lock (SyncRoot)
            {
                return DefinitionsByCardKeyword.TryGetValue(value, out definition!);
            }
        }

        /// <summary>
        ///     Whether <paramref name="value" /> is a registered mod keyword (as opposed to a vanilla
        ///     <see cref="CardKeyword" /> literal or an unknown integer cast).
        ///     <paramref name="value" /> 是否为已注册的 mod keyword（而不是原版
        ///     <see cref="CardKeyword" /> literal 或未知整数转换）。
        /// </summary>
        public static bool IsModCardKeyword(CardKeyword value)
        {
            lock (SyncRoot)
            {
                return DefinitionsByCardKeyword.ContainsKey(value);
            }
        }

        /// <summary>
        ///     Resolves the deterministic <see cref="CardKeyword" /> value minted for <paramref name="id" />.
        ///     The id does not need to be registered, but registered ids can still provide hover-tip metadata.
        ///     Prefer this over passing a string when interacting with vanilla keyword APIs
        ///     (<c>CardModel.AddKeyword</c> / <c>Keywords.Contains</c>).
        ///     解析为 <paramref name="id" /> 确定性 minted 的 <see cref="CardKeyword" /> 值。该 id 不需要已注册，
        ///     但已注册 id 仍可提供 hover-tip 元数据。与原版 keyword API 交互时，
        ///     优先使用此方法而不是传递字符串（<c>CardModel.AddKeyword</c> /
        ///     <c>Keywords.Contains</c>）。
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
        ///     Resolves either a registered mod keyword id or a vanilla <see cref="CardKeyword" /> enum name.
        ///     Mod ids take precedence when a string could match both.
        ///     解析已注册的 mod keyword id 或原版 <see cref="CardKeyword" /> enum 名称。
        ///     当字符串可能同时匹配两者时，mod id 优先。
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
        ///     Returns the deterministic <see cref="CardKeyword" /> minted for <paramref name="id" />.
        ///     The id does not need to be registered.
        ///     返回为 <paramref name="id" /> 确定性 minted 的 <see cref="CardKeyword" />。
        ///     该 id 不需要已注册。
        /// </summary>
        public static CardKeyword GetCardKeyword(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return DynamicEnumValueRegistry<CardKeyword>.GetValue(id);
        }

        /// <summary>
        ///     Tries to resolve the string id that minted <paramref name="value" />.
        ///     尝试解析 minted <paramref name="value" /> 的字符串 id。
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
        ///     Snapshot of all registered keyword definitions, stable-ordered by id.
        ///     所有已注册 keyword definition 的快照，按 id 稳定排序。
        /// </summary>
        public static ModKeywordDefinition[] GetDefinitionsSnapshot()
        {
            lock (SyncRoot)
            {
                return Definitions.Values
                    .OrderBy(def => def.Id, StringComparer.Ordinal)
                    .ToArray();
            }
        }

        /// <summary>
        ///     Builds a vanilla <see cref="IHoverTip" /> for <paramref name="id" /> using registered title, description, and
        ///     icon.
        ///     使用已注册的 title、description 和
        ///     icon 为 <paramref name="id" /> 构建原版 <see cref="IHoverTip" />。
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
        ///     Title <see cref="LocString" /> for the keyword.
        ///     keyword 的 title <see cref="LocString" />。
        /// </summary>
        public static LocString GetTitle(string id)
        {
            var definition = Get(id);
            return new(definition.TitleTable, definition.TitleKey);
        }

        /// <summary>
        ///     Description <see cref="LocString" /> for the keyword.
        ///     keyword 的 description <see cref="LocString" />。
        /// </summary>
        public static LocString GetDescription(string id)
        {
            var definition = Get(id);
            return new(definition.DescriptionTable, definition.DescriptionKey);
        }

        /// <summary>
        ///     BBCode snippet suitable for inline card text (gold title + period).
        ///     适合内联卡牌文本的 BBCode 片段（金色标题 + 句点）。
        /// </summary>
        public static string GetCardText(string id)
        {
            var period = new LocString("card_keywords", "PERIOD");
            return "[gold]" + GetTitle(id).GetFormattedText() + "[/gold]" + period.GetRawText();
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
