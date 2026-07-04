using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Sort option shown by the optional mod card-pile view sort bar.
    ///     可选 mod 牌堆查看界面排序栏显示的排序项。
    /// </summary>
    public enum ModCardPileSortOption
    {
        /// <summary>
        ///     Original pile order.
        ///     原始牌堆顺序。
        /// </summary>
        Obtained = 0,

        /// <summary>
        ///     Card type.
        ///     卡牌类型。
        /// </summary>
        Type = 1,

        /// <summary>
        ///     Energy cost.
        ///     能量费用。
        /// </summary>
        Cost = 2,

        /// <summary>
        ///     Localized card title.
        ///     本地化卡牌标题。
        /// </summary>
        Alphabetical = 3,

        /// <summary>
        ///     Card rarity.
        ///     卡牌稀有度。
        /// </summary>
        Rarity = 4,
    }

    internal static class ModCardPileSortOptionExtensions
    {
        public static SortingOrders Ascending(this ModCardPileSortOption option)
        {
            return option switch
            {
                ModCardPileSortOption.Obtained => SortingOrders.Ascending,
                ModCardPileSortOption.Type => SortingOrders.TypeAscending,
                ModCardPileSortOption.Cost => SortingOrders.CostAscending,
                ModCardPileSortOption.Alphabetical => SortingOrders.AlphabetAscending,
                ModCardPileSortOption.Rarity => SortingOrders.RarityAscending,
                _ => SortingOrders.Ascending,
            };
        }

        public static SortingOrders Descending(this ModCardPileSortOption option)
        {
            return option switch
            {
                ModCardPileSortOption.Obtained => SortingOrders.Descending,
                ModCardPileSortOption.Type => SortingOrders.TypeDescending,
                ModCardPileSortOption.Cost => SortingOrders.CostDescending,
                ModCardPileSortOption.Alphabetical => SortingOrders.AlphabetDescending,
                ModCardPileSortOption.Rarity => SortingOrders.RarityDescending,
                _ => SortingOrders.Descending,
            };
        }
    }
}
