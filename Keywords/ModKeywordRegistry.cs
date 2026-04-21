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
    ///     normalized id; prefer <c>RegisterOwned</c> / <c>RegisterCardKeywordOwned</c> so ids stay mod-scoped like fixed
    ///     model public entries.
    /// </summary>
    public sealed class ModKeywordRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModKeywordRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ModKeywordDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<CardKeyword, ModKeywordDefinition> DefinitionsByCardKeyword = [];

        private static readonly DynamicEnumValueMinter<CardKeyword> CardKeywordMinter = new();

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
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     Convenience view of <see cref="IsFrozen" /> as <see cref="KeywordRegistrationState" />.
        /// </summary>
        public static KeywordRegistrationState State => IsFrozen
            ? KeywordRegistrationState.Frozen
            : KeywordRegistrationState.Open;

        /// <summary>
        ///     Returns the singleton registry for <paramref name="modId" />, creating it on first use.
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
        ///     Registers a <c>card_keywords</c> entry whose id is mod-qualified from <paramref name="localKeywordStem" />.
        /// </summary>
        public ModKeywordDefinition RegisterCardKeywordOwnedByLocNamespace(
            string localKeywordStem,
            string? locNamespace,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localKeywordStem);

            var ns = string.IsNullOrWhiteSpace(locNamespace)
                ? StringHelper.Slugify(_modId)
                : locNamespace;

            var stem = StringHelper.Slugify(ns) + "_" + StringHelper.Slugify(localKeywordStem);
            var id = ModContentRegistry.GetQualifiedKeywordId(_modId, localKeywordStem);

            return RegisterCore(
                id,
                "card_keywords",
                $"{stem}.title",
                "card_keywords",
                $"{stem}.description",
                iconPath,
                cardDescriptionPlacement,
                includeInCardHoverTip);
        }

        /// <summary>
        ///     <c>RegisterCardKeywordOwnedByLocNamespace</c> with legacy hover defaults.
        /// </summary>
        public ModKeywordDefinition RegisterCardKeywordOwnedByLocNamespace(
            string localKeywordStem,
            string? locNamespace = null,
            string? iconPath = null)
        {
            return RegisterCardKeywordOwnedByLocNamespace(
                localKeywordStem,
                locNamespace,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     Registers a <c>card_keywords</c> entry whose id is mod-qualified from <paramref name="localKeywordStem" />.
        /// </summary>
        [Obsolete(
            "Pitfall: locKeyPrefix is NOT a prefix that affects only the modid/namespace portion. It is the full card_keywords entry stem used to form '{stem}.title' and '{stem}.description'. Prefer RegisterCardKeywordOwnedByLocNamespace (default stem: '<modid>_<keyword>').")]
        public ModKeywordDefinition RegisterCardKeywordOwned(
            string localKeywordStem,
            string? locKeyPrefix,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localKeywordStem);

            var prefix = string.IsNullOrWhiteSpace(locKeyPrefix)
                ? StringHelper.Slugify(localKeywordStem)
                : locKeyPrefix.Trim();

            var id = ModContentRegistry.GetQualifiedKeywordId(_modId, localKeywordStem);
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
        ///     <c>RegisterCardKeywordOwned</c> with legacy hover defaults.
        /// </summary>
        [Obsolete(
            "Pitfall: locKeyPrefix is NOT a prefix that affects only the modid/namespace portion. It is the full card_keywords entry stem used to form '{stem}.title' and '{stem}.description'. Prefer RegisterCardKeywordOwnedByLocNamespace (default stem: '<modid>_<keyword>').")]
        public ModKeywordDefinition RegisterCardKeywordOwned(
            string localKeywordStem,
            string? locKeyPrefix = null,
            string? iconPath = null)
        {
            return RegisterCardKeywordOwned(
                localKeywordStem,
                locKeyPrefix,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     Registers a keyword with a raw global id. Prefer <c>RegisterOwned</c> to avoid cross-mod collisions.
        /// </summary>
        [Obsolete(
            "Flat keyword ids are global: they collide across mods and do not follow fixed public entry naming. Use RegisterOwned / RegisterCardKeywordOwned, or ModContentRegistry.GetQualifiedKeywordId for cross-mod references.")]
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
        /// </summary>
        [Obsolete(
            "Flat keyword ids are global: they collide across mods and do not follow fixed public entry naming. Use RegisterOwned / RegisterCardKeywordOwned, or ModContentRegistry.GetQualifiedKeywordId for cross-mod references.")]
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
        ///     Registers a card keyword with a raw global id. Prefer <c>RegisterCardKeywordOwned</c>.
        /// </summary>
        [Obsolete(
            "Flat keyword ids are global: they collide across mods and do not follow fixed public entry naming. Use RegisterCardKeywordOwned, or ModContentRegistry.GetQualifiedKeywordId for cross-mod references.")]
        public ModKeywordDefinition RegisterCardKeyword(
            string id,
            string? locKeyPrefix,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            var prefix = string.IsNullOrWhiteSpace(locKeyPrefix)
                ? StringHelper.Slugify(id)
                : locKeyPrefix.Trim();

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
        /// </summary>
        [Obsolete(
            "Flat keyword ids are global: they collide across mods and do not follow fixed public entry naming. Use RegisterCardKeywordOwned, or ModContentRegistry.GetQualifiedKeywordId for cross-mod references.")]
        public ModKeywordDefinition RegisterCardKeyword(string id, string? locKeyPrefix = null, string? iconPath = null)
        {
            return RegisterCardKeyword(
                id,
                locKeyPrefix,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     Same as obsolete <c>Register</c> (full signature) without triggering obsolete warnings; for in-library
        ///     forwarding from manifests.
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
            var cardKeywordValue = CardKeywordMinter.Mint(normalizedId);
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
        /// </summary>
        public static bool IsModCardKeyword(CardKeyword value)
        {
            lock (SyncRoot)
            {
                return DefinitionsByCardKeyword.ContainsKey(value);
            }
        }

        /// <summary>
        ///     Resolves the <see cref="CardKeyword" /> value minted for <paramref name="id" />. Prefer this over
        ///     passing a string when interacting with vanilla keyword APIs (<c>CardModel.AddKeyword</c> /
        ///     <c>Keywords.Contains</c>).
        /// </summary>
        public static bool TryGetCardKeyword(string id, out CardKeyword value)
        {
            if (TryGet(id, out var definition))
            {
                value = definition.CardKeywordValue;
                return true;
            }

            value = CardKeyword.None;
            return false;
        }

        /// <summary>
        ///     Returns the <see cref="CardKeyword" /> minted for <paramref name="id" /> or throws
        ///     <see cref="KeyNotFoundException" /> when unregistered.
        /// </summary>
        public static CardKeyword GetCardKeyword(string id)
        {
            return Get(id).CardKeywordValue;
        }

        internal static ModKeywordDefinition[] GetDefinitionsSnapshot()
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
        /// </summary>
        public static LocString GetTitle(string id)
        {
            var definition = Get(id);
            return new(definition.TitleTable, definition.TitleKey);
        }

        /// <summary>
        ///     Description <see cref="LocString" /> for the keyword.
        /// </summary>
        public static LocString GetDescription(string id)
        {
            var definition = Get(id);
            return new(definition.DescriptionTable, definition.DescriptionKey);
        }

        /// <summary>
        ///     BBCode snippet suitable for inline card text (gold title + period).
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
            return id.Trim().ToLowerInvariant();
        }
    }
}
