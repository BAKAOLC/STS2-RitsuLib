using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.Keywords
{
    public sealed class ModKeywordRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModKeywordRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ModKeywordDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly string _modId;

        private ModKeywordRegistry(string modId)
        {
            _modId = modId;
        }

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

        public ModKeywordDefinition Register(
            string id,
            string titleTable = "card_keywords",
            string? titleKey = null,
            string? descriptionTable = null,
            string? descriptionKey = null,
            string? iconPath = null)
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
                iconPath);

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

            RitsuLibFramework.Logger.Info($"[Keywords] Registered keyword: {normalizedId} ({_modId})");
            return definition;
        }

        public ModKeywordDefinition RegisterCardKeyword(string id, string? locKeyPrefix = null, string? iconPath = null)
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
                iconPath);
        }

        public static bool TryGet(string id, out ModKeywordDefinition definition)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            lock (SyncRoot)
            {
                return Definitions.TryGetValue(NormalizeId(id), out definition!);
            }
        }

        public static ModKeywordDefinition Get(string id)
        {
            return TryGet(id, out var definition)
                ? definition
                : throw new KeyNotFoundException($"Keyword '{NormalizeId(id)}' is not registered.");
        }

        public static IHoverTip CreateHoverTip(string id)
        {
            var definition = Get(id);
            Texture2D? icon = null;

            if (!string.IsNullOrWhiteSpace(definition.IconPath) && ResourceLoader.Exists(definition.IconPath))
                icon = ResourceLoader.Load<Texture2D>(definition.IconPath);

            return new HoverTip(GetTitle(id), GetDescription(id), icon);
        }

        public static LocString GetTitle(string id)
        {
            var definition = Get(id);
            return new(definition.TitleTable, definition.TitleKey);
        }

        public static LocString GetDescription(string id)
        {
            var definition = Get(id);
            return new(definition.DescriptionTable, definition.DescriptionKey);
        }

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
