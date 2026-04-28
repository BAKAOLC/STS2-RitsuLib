using System.Diagnostics.CodeAnalysis;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     External badge icon path override providers keyed by registration key.
    /// </summary>
    public static class ExternalBadgeIconOverrideRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, Func<string, string?>> IconPathProviders =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Registers or replaces an icon path provider.
        /// </summary>
        public static void RegisterIconPathProvider(string key, Func<string, string?> provider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(provider);
            lock (SyncRoot)
            {
                IconPathProviders[key] = provider;
            }
        }

        /// <summary>
        ///     Unregisters a previously registered provider key.
        /// </summary>
        public static bool UnregisterIconPathProvider(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            lock (SyncRoot)
            {
                return IconPathProviders.Remove(key);
            }
        }

        /// <summary>
        ///     Clears all registered providers.
        /// </summary>
        public static void Clear()
        {
            lock (SyncRoot)
            {
                IconPathProviders.Clear();
            }
        }

        internal static bool TryGetIconPath(string badgeId, [NotNullWhen(true)] out string? iconPath)
        {
            lock (SyncRoot)
            {
                foreach (var value in IconPathProviders.Values.Select(provider => provider(badgeId))
                             .Where(value => !string.IsNullOrWhiteSpace(value)))
                {
                    iconPath = value!;
                    return true;
                }
            }

            iconPath = string.Empty;
            return false;
        }
    }

    internal static class ModBadgeRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static List<Type> _badgeTypes = [];

        internal static IReadOnlyList<Type> GetRegisteredBadgeTypes()
        {
            lock (SyncRoot)
            {
                _badgeTypes = [.. ModContentRegistry.GetRegisteredBadgeTypes()];
                return _badgeTypes.ToArray();
            }
        }

        internal static bool TryGetBadgeTemplateByRegistrationId(string id, out ModBadgeTemplate template)
        {
            foreach (var type in GetRegisteredBadgeTypes())
            {
                var current = CreateTemplateInstance(type);
                if (current == null)
                    continue;
                if (!string.Equals(current.Id, id, StringComparison.OrdinalIgnoreCase))
                    continue;

                template = current;
                return true;
            }

            template = null!;
            return false;
        }

        internal static ModBadgeTemplate? CreateTemplateInstance(Type type)
        {
            try
            {
                return Activator.CreateInstance(type, true) as ModBadgeTemplate;
            }
            catch
            {
                return null;
            }
        }
    }

#if STS2_V_0_103_2
    internal sealed class RuntimeTemplateBadge : Badge
    {
        internal RuntimeTemplateBadge(ModBadgeTemplate template, SerializableRun run, ulong playerId) : base(run, playerId)
        {
            Template = template;
        }

        internal ModBadgeTemplate Template { get; }

        public override string Id => Template.Id;
        public override BadgeRarity Rarity => Template.Rarity(_run, _localPlayer);
        public override bool RequiresWin => Template.RequiresWin;
        public override bool MultiplayerOnly => Template.MultiplayerOnly;

        public override bool IsObtained()
        {
            return Template.IsObtained(_run, _localPlayer);
        }
    }
#else
    internal sealed class RuntimeTemplateBadge : Badge
    {
        internal RuntimeTemplateBadge(ModBadgeTemplate template, SerializableRun run, ulong playerId)
            : base(run, playerId, template.Id, template.RequiresWin, template.MultiplayerOnly)
        {
            Template = template;
        }

        internal ModBadgeTemplate Template { get; }

        public override BadgeRarity Rarity => Template.Rarity(_run, _localPlayer);

        public override bool IsObtained()
        {
            return Template.IsObtained(_run, _localPlayer);
        }
    }
#endif

    internal class BadgePoolCreateAllPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_badge_pool_create_all";
        public static string Description => "Append mod badges to BadgePool.CreateAll";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(BadgePool), nameof(BadgePool.CreateAll))];
        }

        // ReSharper disable once InconsistentNaming
        public static IReadOnlyCollection<Badge> Postfix(IReadOnlyCollection<Badge> __result, SerializableRun run,
            ulong playerId)
        {
            var list = __result.ToList();
            list.AddRange(ModBadgeRegistry.GetRegisteredBadgeTypes()
                .Select(ModBadgeRegistry.CreateTemplateInstance).OfType<ModBadgeTemplate>()
                .Select(template => new RuntimeTemplateBadge(template, run, playerId)));

            return list;
        }
    }

    internal class AssetCacheLoadBadgeFallbackPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_badge_asset_cache_fallback";
        public static string Description => "Provide placeholder resource for missing badge icon assets";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AssetCache), "LoadAsset")];
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(string path, ref Resource __result)
        {
            if (ResourceLoader.Exists(path))
                return true;
            if (!path.Contains("game_over_screen/badge_", StringComparison.OrdinalIgnoreCase))
                return true;

            __result = ResourceLoader.Load<Resource>(ImageHelper.GetImagePath("debug/placeholder_64.png"));
            return false;
        }
    }

    internal class NBadgeCreateIconPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_nbadge_create_icon";
        public static string Description => "Apply mod badge icon overrides for NBadge.Create";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NBadge), nameof(NBadge.Create), [typeof(string), typeof(BadgeRarity)], true)];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NBadge __result, string id)
        {
            if (__result == null || string.IsNullOrWhiteSpace(id))
                return;
            if (!TryResolveCustomIconPath(id, out var iconPath))
                return;
            if (!ResourceLoader.Exists(iconPath))
                return;

            var texture = ResourceLoader.Load<Texture2D>(iconPath);
            if (texture == null)
                return;
            __result.GetNode<TextureRect>("%Icon").Texture = texture;
        }

        private static bool TryResolveCustomIconPath(string badgeId, [NotNullWhen(true)] out string? iconPath)
        {
            if (ExternalBadgeIconOverrideRegistry.TryGetIconPath(badgeId, out iconPath))
                return true;

            if (!ModBadgeRegistry.TryGetBadgeTemplateByRegistrationId(badgeId, out var template) ||
                string.IsNullOrWhiteSpace(template.CustomBadgeIconPath))
            {
                iconPath = string.Empty;
                return false;
            }

            iconPath = template.CustomBadgeIconPath;
            return true;
        }
    }

    internal class BadgeIconGetterPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_badge_icon_getter";
        public static string Description => "Allow runtime mod badges to override BadgeIcon texture";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Badge), "BadgeIcon", null, true, MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(Badge __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            if (!TryResolveCustomIconPath(__instance, out var iconPath))
                return true;
            if (!ResourceLoader.Exists(iconPath))
                return true;

            var texture = ResourceLoader.Load<Texture2D>(iconPath);
            if (texture == null)
                return true;

            __result = texture;
            return false;
        }

        private static bool TryResolveCustomIconPath(Badge badge, [NotNullWhen(true)] out string? iconPath)
        {
            if (ExternalBadgeIconOverrideRegistry.TryGetIconPath(badge.Id, out iconPath))
                return true;

            if (badge is not RuntimeTemplateBadge runtimeBadge ||
                string.IsNullOrWhiteSpace(runtimeBadge.Template.CustomBadgeIconPath))
            {
                iconPath = string.Empty;
                return false;
            }

            iconPath = runtimeBadge.Template.CustomBadgeIconPath;
            return true;
        }
    }
}
