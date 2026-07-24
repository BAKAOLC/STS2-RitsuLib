using Godot;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     External override registry for card frame/portrait/banner materials.
    ///     Intended for models that cannot implement RitsuLib interfaces directly (for example, vanilla cards).
    ///     卡牌 frame/portrait/banner materials的外部覆盖注册表。
    ///     用于无法直接实现 RitsuLib 接口的模型（例如原版卡牌）。
    /// </summary>
    public static class ExternalCardMaterialOverrideRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, Func<CardModel, Material?>> FrameProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<CardModel, Material?>> PortraitProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<CardModel, Material?>> BannerProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<CardPoolModel, Material?>> PoolFrameProviders =
            new(StringComparer.Ordinal);

        /// <summary>
        ///     Registers or replaces a frame material provider.
        ///     注册或替换边框材质提供器。
        /// </summary>
        public static void RegisterFrameProvider(string key, Func<CardModel, Material?> provider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(provider);
            lock (SyncRoot)
            {
                FrameProviders[key] = provider;
            }

            RuntimeAssetRefreshCoordinator.Request(RuntimeAssetRefreshScope.Cards);
        }

        /// <summary>
        ///     Registers or replaces a portrait material provider.
        ///     注册或替换卡图材质提供器。
        /// </summary>
        public static void RegisterPortraitProvider(string key, Func<CardModel, Material?> provider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(provider);
            lock (SyncRoot)
            {
                PortraitProviders[key] = provider;
            }

            RuntimeAssetRefreshCoordinator.Request(RuntimeAssetRefreshScope.Cards);
        }

        /// <summary>
        ///     Registers or replaces a banner material provider.
        ///     注册或替换横幅材质提供器。
        /// </summary>
        public static void RegisterBannerProvider(string key, Func<CardModel, Material?> provider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(provider);
            lock (SyncRoot)
            {
                BannerProviders[key] = provider;
            }

            RuntimeAssetRefreshCoordinator.Request(RuntimeAssetRefreshScope.Cards);
        }

        /// <summary>
        ///     Removes a frame material provider by key.
        ///     按键移除a 边框材质 提供器。
        /// </summary>
        public static bool UnregisterFrameProvider(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            bool removed;
            lock (SyncRoot)
            {
                removed = FrameProviders.Remove(key);
            }

            if (removed)
                RuntimeAssetRefreshCoordinator.Request(RuntimeAssetRefreshScope.Cards);
            return removed;
        }

        /// <summary>
        ///     Removes a portrait material provider by key.
        ///     按键移除卡图材质提供器。
        /// </summary>
        public static bool UnregisterPortraitProvider(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            bool removed;
            lock (SyncRoot)
            {
                removed = PortraitProviders.Remove(key);
            }

            if (removed)
                RuntimeAssetRefreshCoordinator.Request(RuntimeAssetRefreshScope.Cards);
            return removed;
        }

        /// <summary>
        ///     Removes a banner material provider by key.
        ///     按键移除a 横幅材质 提供器。
        /// </summary>
        public static bool UnregisterBannerProvider(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            bool removed;
            lock (SyncRoot)
            {
                removed = BannerProviders.Remove(key);
            }

            if (removed)
                RuntimeAssetRefreshCoordinator.Request(RuntimeAssetRefreshScope.Cards);
            return removed;
        }

        /// <summary>
        ///     Registers or replaces a pool-frame material provider.
        ///     注册或替换pool-边框材质提供器。
        /// </summary>
        public static void RegisterPoolFrameProvider(string key, Func<CardPoolModel, Material?> provider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(provider);
            lock (SyncRoot)
            {
                PoolFrameProviders[key] = provider;
            }

            RuntimeAssetRefreshCoordinator.Request(RuntimeAssetRefreshScope.Cards);
        }

        /// <summary>
        ///     Removes a pool-frame material provider by key.
        ///     按键移除a pool-边框材质 提供器。
        /// </summary>
        public static bool UnregisterPoolFrameProvider(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            bool removed;
            lock (SyncRoot)
            {
                removed = PoolFrameProviders.Remove(key);
            }

            if (removed)
                RuntimeAssetRefreshCoordinator.Request(RuntimeAssetRefreshScope.Cards);
            return removed;
        }

        /// <summary>
        ///     Clears all frame, portrait, and banner providers.
        ///     清除所有边框、卡图和横幅提供器。
        /// </summary>
        public static void Clear()
        {
            lock (SyncRoot)
            {
                FrameProviders.Clear();
                PortraitProviders.Clear();
                BannerProviders.Clear();
                PoolFrameProviders.Clear();
            }

            RuntimeAssetRefreshCoordinator.Request(RuntimeAssetRefreshScope.Cards);
        }

        internal static bool TryGetFrameMaterial(CardModel card, out Material material)
        {
            foreach (var provider in Snapshot(FrameProviders))
            {
                Material? value;
                try
                {
                    value = provider(card);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Assets] External frame material provider failed for '{card.GetType().Name}': {ex.Message}");
                    continue;
                }

                if (value == null)
                    continue;

                material = value;
                return true;
            }

            material = null!;
            return false;
        }

        internal static bool TryGetPortraitMaterial(CardModel card, out Material material)
        {
            foreach (var provider in Snapshot(PortraitProviders))
            {
                Material? value;
                try
                {
                    value = provider(card);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Assets] External portrait material provider failed for '{card.GetType().Name}': {ex.Message}");
                    continue;
                }

                if (value == null)
                    continue;

                material = value;
                return true;
            }

            material = null!;
            return false;
        }

        internal static bool TryGetBannerMaterial(CardModel card, out Material material)
        {
            foreach (var provider in Snapshot(BannerProviders))
            {
                Material? value;
                try
                {
                    value = provider(card);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Assets] External banner material provider failed for '{card.GetType().Name}': {ex.Message}");
                    continue;
                }

                if (value == null)
                    continue;

                material = value;
                return true;
            }

            material = null!;
            return false;
        }

        internal static bool TryGetPoolFrameMaterial(CardPoolModel pool, out Material material)
        {
            foreach (var provider in Snapshot(PoolFrameProviders))
            {
                Material? value;
                try
                {
                    value = provider(pool);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Assets] External pool frame material provider failed for '{pool.GetType().Name}': {ex.Message}");
                    continue;
                }

                if (value == null)
                    continue;

                material = value;
                return true;
            }

            material = null!;
            return false;
        }

        private static Func<CardModel, Material?>[] Snapshot(Dictionary<string, Func<CardModel, Material?>> providers)
        {
            lock (SyncRoot)
            {
                return [.. providers.Values];
            }
        }

        private static Func<CardPoolModel, Material?>[] Snapshot(
            Dictionary<string, Func<CardPoolModel, Material?>> providers)
        {
            lock (SyncRoot)
            {
                return [.. providers.Values];
            }
        }
    }
}
