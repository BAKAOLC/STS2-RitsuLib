using Godot;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     External override registry for card frame/banner materials.
    ///     Intended for models that cannot implement RitsuLib interfaces directly (for example, vanilla cards).
    /// </summary>
    public static class ExternalCardMaterialOverrideRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, Func<CardModel, Material?>> FrameProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<CardModel, Material?>> BannerProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<CardPoolModel, Material?>> PoolFrameProviders =
            new(StringComparer.Ordinal);

        /// <summary>
        ///     Registers or replaces a frame material provider.
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
        ///     Registers or replaces a banner material provider.
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
        ///     Removes a banner material provider by key.
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
        ///     Clears all frame and banner providers.
        /// </summary>
        public static void Clear()
        {
            lock (SyncRoot)
            {
                FrameProviders.Clear();
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
                return providers.Values.ToArray();
            }
        }

        private static Func<CardPoolModel, Material?>[] Snapshot(
            Dictionary<string, Func<CardPoolModel, Material?>> providers)
        {
            lock (SyncRoot)
            {
                return providers.Values.ToArray();
            }
        }
    }
}
