using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Per-mod registry for hover-tip keywords (titles, descriptions, icons) with global lookup by id.
    /// </summary>
    public sealed class ModKeywordRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModKeywordRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ModKeywordDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Logger _logger;

        private readonly string _modId;

        private ModKeywordRegistry(string modId)
        {
            _modId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

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

        /// <summary>
        ///     Registers a keyword for this mod; duplicates from the same mod return the existing definition (full
        ///     signature).
        /// </summary>
        /// <param name="id">Keyword id (trimmed, lowercased).</param>
        /// <param name="titleTable">Title localization table.</param>
        /// <param name="titleKey">Title key; defaults to <c>{id}.title</c> when null.</param>
        /// <param name="descriptionTable">Description table; defaults to <paramref name="titleTable" /> when null.</param>
        /// <param name="descriptionKey">Description key; defaults to <c>{id}.description</c> when null.</param>
        /// <param name="iconPath">Optional Godot resource path for the icon.</param>
        /// <param name="cardDescriptionPlacement">Inline card-description injection placement.</param>
        /// <param name="includeInCardHoverTip">Whether this id participates in template keyword hover-tip expansion.</param>
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
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(titleTable);

            var normalizedId = NormalizeId(id);
            var definition = new ModKeywordDefinition(
                _modId,
                normalizedId,
                titleTable,
                titleKey ?? $"{normalizedId}.title",
                descriptionTable ?? titleTable,
                descriptionKey ?? $"{normalizedId}.description",
                iconPath,
                cardDescriptionPlacement,
                includeInCardHoverTip);

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(normalizedId, out var existing))
                {
                    if (existing != definition)
                        throw new InvalidOperationException(
                            $"Keyword '{normalizedId}' is already registered by mod '{existing.ModId}'.");

                    return existing;
                }

                Definitions[normalizedId] = definition;
            }

            _logger.Info($"[Keywords] Registered keyword: {normalizedId}");
            return definition;
        }

        /// <summary>
        ///     Legacy <c>Register</c> signature preserved for older mods; forwards with prior hover-tip behavior.
        /// </summary>
        public ModKeywordDefinition Register(
            string id,
            string titleTable = "card_keywords",
            string? titleKey = null,
            string? descriptionTable = null,
            string? descriptionKey = null,
            string? iconPath = null)
        {
            return Register(
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
        ///     Convenience for card keywords: uses <c>card_keywords</c> and slugified keys (full signature).
        /// </summary>
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

            return Register(
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

        private static string NormalizeId(string id)
        {
            return id.Trim().ToLowerInvariant();
        }
    }
}
