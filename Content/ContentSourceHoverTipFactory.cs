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
            ArgumentNullException.ThrowIfNull(model);

            if (!RitsuLibSettingsStore.IsModSourceHoverTipsEnabled())
            {
                tip = null!;
                return false;
            }

            var source = model is IContentSourceSupplier supplier
                ? Resolve(supplier)
                : Resolve(model.GetType());
            tip = CreateTip(source);
            return true;
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
            return ModKeywordRegistry.TryGetByCardKeyword(keyword, out var def)
                ? ResolveMod(def.ModId)
                : ContentSourceInfo.Vanilla;
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
            if (ModContentRegistry.TryGetOwnerModId(modelType, out var ownerModId))
                return ResolveMod(ownerModId);

            var assembly = modelType.Assembly;
            if (assembly == GameAssembly)
                return ContentSourceInfo.Vanilla;

            foreach (var mod in Sts2ModManagerCompat.EnumerateModsForManifestLookup())
            {
                if (mod.assembly != assembly)
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

            public string Format()
            {
                return $"{DisplayName} ({Id})";
            }
        }
    }
}
