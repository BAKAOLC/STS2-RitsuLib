using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     External override registry for card-pool deck-view styles.
    ///     Intended for card pools that cannot implement RitsuLib interfaces directly, including vanilla pools.
    ///     卡池牌组查看界面样式的外部覆盖注册表。
    ///     用于无法直接实现 RitsuLib 接口的卡池，包括原版卡池。
    /// </summary>
    public static class CardPoolDeckViewStyleRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, Func<CardPoolModel, CardPoolDeckViewStyle?>> Providers =
            new(StringComparer.Ordinal);

        /// <summary>
        ///     Registers or replaces a deck-view style provider.
        ///     注册或替换牌组查看界面样式 provider。
        /// </summary>
        public static void RegisterProvider(string key, Func<CardPoolModel, CardPoolDeckViewStyle?> provider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(provider);
            lock (SyncRoot)
            {
                Providers[key] = provider;
            }
        }

        /// <summary>
        ///     Removes a deck-view style provider by key.
        ///     按 key 移除牌组查看界面样式 provider。
        /// </summary>
        public static bool UnregisterProvider(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            lock (SyncRoot)
            {
                return Providers.Remove(key);
            }
        }

        /// <summary>
        ///     Clears all registered deck-view style providers.
        ///     清除所有已注册的牌组查看界面样式 provider。
        /// </summary>
        public static void Clear()
        {
            lock (SyncRoot)
            {
                Providers.Clear();
            }
        }

        internal static bool TryGetStyle(CardPoolModel pool, out CardPoolDeckViewStyle style)
        {
            if (ModContentRegistry.TryGetEffectiveCardPoolAssetReplacement(pool.Id.Entry, out var registered) &&
                registered.DeckViewStyle != null)
            {
                style = registered.DeckViewStyle;
                return true;
            }

            switch (pool)
            {
                case IModCardPoolDeckViewStyle { DeckViewStyle: not null } direct:
                    style = direct.DeckViewStyle;
                    return true;
                case IModCardPoolAssetOverrides profileSource when
                    profileSource.AssetProfile.DeckViewStyle != null:
                    style = profileSource.AssetProfile.DeckViewStyle;
                    return true;
            }

            foreach (var provider in Snapshot())
            {
                CardPoolDeckViewStyle? value;
                try
                {
                    value = provider(pool);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Assets] Card-pool deck-view style provider failed for '{pool.GetType().Name}': {ex.Message}");
                    continue;
                }

                if (value == null)
                    continue;

                style = value;
                return true;
            }

            style = null!;
            return false;
        }

        private static Func<CardPoolModel, CardPoolDeckViewStyle?>[] Snapshot()
        {
            lock (SyncRoot)
            {
                return [.. Providers.Values];
            }
        }
    }
}
