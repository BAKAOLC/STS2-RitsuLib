using Godot;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides external card frame, portrait, and title-banner material overrides for models that cannot
    ///         implement RitsuLib interfaces directly, including base-game cards.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为无法直接实现 RitsuLib 接口的模型（包括原版卡牌）提供外部卡牌边框、卡图和标题横幅材质覆盖。
    ///     </para>
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
        ///     <para xml:lang="en">Registers or replaces a card-frame material provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换卡牌边框材质提供器。</para>
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
        ///     <para xml:lang="en">Registers or replaces a card-portrait material provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换卡图材质提供器。</para>
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
        ///     <para xml:lang="en">Registers or replaces a card title-banner material provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换卡牌标题横幅材质提供器。</para>
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
        ///     <para xml:lang="en">Removes a card-frame material provider by key.</para>
        ///     <para xml:lang="zh-CN">按键移除卡牌边框材质提供器。</para>
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
        ///     <para xml:lang="en">Removes a card-portrait material provider by key.</para>
        ///     <para xml:lang="zh-CN">按键移除卡图材质提供器。</para>
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
        ///     <para xml:lang="en">Removes a card title-banner material provider by key.</para>
        ///     <para xml:lang="zh-CN">按键移除卡牌标题横幅材质提供器。</para>
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
        ///     <para xml:lang="en">Registers or replaces a card-pool frame-material provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换卡池边框材质提供器。</para>
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
        ///     <para xml:lang="en">Removes a card-pool frame-material provider by key.</para>
        ///     <para xml:lang="zh-CN">按键移除卡池边框材质提供器。</para>
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
        ///     <para xml:lang="en">Removes all card and card-pool material providers.</para>
        ///     <para xml:lang="zh-CN">移除所有卡牌和卡池材质提供器。</para>
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

                if (!GodotObject.IsInstanceValid(value))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Assets] External frame material provider returned an invalid Material for '{card.GetType().Name}'. Ignoring it.");
                    continue;
                }

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

                if (!GodotObject.IsInstanceValid(value))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Assets] External portrait material provider returned an invalid Material for '{card.GetType().Name}'. Ignoring it.");
                    continue;
                }

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

                if (!GodotObject.IsInstanceValid(value))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Assets] External banner material provider returned an invalid Material for '{card.GetType().Name}'. Ignoring it.");
                    continue;
                }

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

                if (!GodotObject.IsInstanceValid(value))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Assets] External pool frame material provider returned an invalid Material for '{pool.GetType().Name}'. Ignoring it.");
                    continue;
                }

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
