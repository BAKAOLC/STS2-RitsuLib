using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Data;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Localization;
using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Content
{
    internal static class ContentSourceHoverTipFactory
    {
        private const string TitleKey = "ritsulib.modSourceHoverTip.title";
        private const string LocTableStem = "MOD_SETTINGS";
        private const string TipIdPrefix = "ritsulib:content_source:";

        private static readonly Assembly GameAssembly = typeof(AbstractModel).Assembly;

        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<Type, ContentSourceInfo> SourceByModelType = [];

        private static readonly Dictionary<string, ContentSourceInfo> SourceByModId =
            new(StringComparer.OrdinalIgnoreCase);

        internal static bool TryCreate(AbstractModel model, out IHoverTip tip)
        {
            if (!TryResolve(model, out var source))
            {
                tip = null!;
                return false;
            }

            tip = CreateTip(source);
            return true;
        }

        internal static bool TryResolve(AbstractModel model, out ContentSourceInfo source)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (!RitsuLibSettingsStore.IsModSourceHoverTipsEnabled() || !IsModelSectionEnabled(model))
            {
                source = default;
                return false;
            }

            source = model is IContentSourceSupplier supplier
                ? Resolve(supplier)
                : Resolve(model.GetType());
            return ShouldShow(source);
        }

        internal static bool ShouldShow(ContentSourceInfo source)
        {
            return !source.IsVanilla || RitsuLibSettingsStore.ShouldIncludeVanillaModSourceHoverTips();
        }

        private static bool IsModelSectionEnabled(AbstractModel model)
        {
            return model switch
            {
                CardModel => RitsuLibSettingsStore.ShouldShowCardModSourceHoverTips(),
                RelicModel => RitsuLibSettingsStore.ShouldShowRelicModSourceHoverTips(),
                PotionModel => RitsuLibSettingsStore.ShouldShowPotionModSourceHoverTips(),
                PowerModel => RitsuLibSettingsStore.ShouldShowPowerModSourceHoverTips(),
                OrbModel => RitsuLibSettingsStore.ShouldShowOrbModSourceHoverTips(),
                EnchantmentModel => RitsuLibSettingsStore.ShouldShowEnchantmentModSourceHoverTips(),
                AfflictionModel => RitsuLibSettingsStore.ShouldShowAfflictionModSourceHoverTips(),
                MonsterModel => RitsuLibSettingsStore.ShouldShowCreatureModSourceHoverTips(),
                _ => true,
            };
        }

        private static HoverTip CreateTip(ContentSourceInfo source)
        {
            return new(GetTitle(), source.Format())
            {
                Id = TipIdPrefix + source.Id,
            };
        }

        internal static LocString GetTitle()
        {
            I18NLocTableBridge.TryRegister(Const.ModId, ModSettingsLocalization.Instance, LocTableStem);
            var tableId = I18NLocTableBridge.GetTableId(Const.ModId, LocTableStem);
            return new(tableId, TitleKey);
        }

        internal static ContentSourceInfo Resolve(Type modelType)
        {
            lock (SyncRoot)
            {
                if (SourceByModelType.TryGetValue(modelType, out var cached))
                    return cached;
            }

            var resolved = ResolveUncached(modelType);
            lock (SyncRoot)
            {
                SourceByModelType[modelType] = resolved;
            }

            return resolved;
        }

        internal static ContentSourceInfo ResolveKeyword(CardKeyword keyword)
        {
            if (Enum.IsDefined(keyword))
                return ContentSourceInfo.Vanilla;

            if (ModKeywordRegistry.TryGetByCardKeyword(keyword, out var def))
                return ResolveMod(def.ModId);

            return TryResolveBaseLibKeyword(keyword, out var baseLibSource)
                ? baseLibSource
                : ContentSourceInfo.Unknown;
        }

        internal static ContentSourceInfo Resolve(IContentSourceSupplier supplier)
        {
            var source = supplier.ContentSource;
            var modId = NormalizeModId(source.ModId);
            if (string.Equals(modId, ContentSourceInfo.Vanilla.Id, StringComparison.OrdinalIgnoreCase))
                return ContentSourceInfo.Vanilla;

            var displayName = string.IsNullOrWhiteSpace(source.DisplayName)
                ? ModSettingsLocalization.ResolveModName(modId, modId)
                : source.DisplayName;
            return CacheModSource(new(modId, NormalizeDisplayName(displayName, modId)));
        }

        private static ContentSourceInfo ResolveUncached(Type modelType)
        {
            var assembly = modelType.Assembly;
            if (assembly == GameAssembly)
                return ContentSourceInfo.Vanilla;

            if (ModContentRegistry.TryGetOwnerModId(modelType, out var ownerModId))
                return ResolveMod(ownerModId);

            foreach (var mod in Sts2ModManagerCompat.EnumerateModsForManifestLookup())
            {
                if (!Sts2ModManagerCompat.GetAssemblies(mod).Contains(assembly))
                    continue;

                var modId = NormalizeModId(mod.manifest?.id, assembly);
                var displayName = NormalizeDisplayName(mod.manifest?.name, modId);
                return CacheModSource(new(modId, displayName));
            }

            var fallbackId = assembly.GetName().Name ?? modelType.Namespace ?? modelType.Name;
            return new(fallbackId, fallbackId);
        }

        private static ContentSourceInfo ResolveMod(string modId)
        {
            lock (SyncRoot)
            {
                if (SourceByModId.TryGetValue(modId, out var cached))
                    return cached;
            }

            var displayName = ModSettingsLocalization.ResolveModName(modId, modId);
            return CacheModSource(new(modId, NormalizeDisplayName(displayName, modId)));
        }

        private static bool TryResolveBaseLibKeyword(CardKeyword keyword, out ContentSourceInfo source)
        {
            if (!BaseLibKeywordSourceResolver.Contains(keyword))
            {
                source = default;
                return false;
            }

            source = ContentSourceInfo.BaseLib;
            return true;
        }

        private static ContentSourceInfo CacheModSource(ContentSourceInfo source)
        {
            lock (SyncRoot)
            {
                SourceByModId[source.Id] = source;
            }

            return source;
        }

        private static string NormalizeModId(string? modId, Assembly assembly)
        {
            if (!string.IsNullOrWhiteSpace(modId))
                return NormalizeModId(modId);

            return assembly.GetName().Name ?? "<unknown>";
        }

        private static string NormalizeModId(string? modId)
        {
            return string.IsNullOrWhiteSpace(modId) ? "<unknown>" : modId.Trim();
        }

        private static string NormalizeDisplayName(string? displayName, string fallback)
        {
            return string.IsNullOrWhiteSpace(displayName) ? fallback : displayName.Trim();
        }

        internal readonly record struct ContentSourceInfo(string Id, string DisplayName)
        {
            public static ContentSourceInfo Vanilla { get; } = new("Vanilla", "Slay The Spire2");
            public static ContentSourceInfo BaseLib { get; } = new("BaseLib", "BaseLib");
            public static ContentSourceInfo Unknown { get; } = new("Unknown", "Unknown");

            public bool IsVanilla => string.Equals(Id, Vanilla.Id, StringComparison.OrdinalIgnoreCase);

            public string Format()
            {
                return string.Equals(DisplayName, Id, StringComparison.OrdinalIgnoreCase)
                    ? DisplayName
                    : $"{DisplayName} ({Id})";
            }
        }

        private static class BaseLibKeywordSourceResolver
        {
            private const string CustomKeywordsTypeName = "BaseLib.Patches.Content.CustomKeywords";
            private const string KeywordIdsFieldName = "KeywordIDs";

            private static readonly Lazy<IDictionary?> KeywordIds = new(FindKeywordIds);

            internal static bool Contains(CardKeyword keyword)
            {
                return KeywordIds.Value?.Contains((int)keyword) == true;
            }

            private static IDictionary? FindKeywordIds()
            {
                var type = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Select(assembly => assembly.GetType(CustomKeywordsTypeName, false))
                    .FirstOrDefault(static type => type != null);
                var field = type?.GetField(
                    KeywordIdsFieldName,
                    BindingFlags.Public | BindingFlags.Static);
                return field?.GetValue(null) as IDictionary;
            }
        }
    }
}
