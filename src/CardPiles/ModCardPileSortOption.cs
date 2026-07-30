using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">Specifies a sort category offered by the optional mod pile view.</para>
    ///     <para xml:lang="zh-CN">指定模组牌堆可选查看界面提供的排序类别。</para>
    /// </summary>
    public enum ModCardPileSortOption
    {
        /// <summary>
        ///     <para xml:lang="en">Sorts by the cards' order in the pile.</para>
        ///     <para xml:lang="zh-CN">按卡牌在牌堆中的顺序排序。</para>
        /// </summary>
        Obtained = 0,

        /// <summary>
        ///     <para xml:lang="en">Sorts by card type.</para>
        ///     <para xml:lang="zh-CN">按卡牌类型排序。</para>
        /// </summary>
        Type = 1,

        /// <summary>
        ///     <para xml:lang="en">Sorts by energy cost.</para>
        ///     <para xml:lang="zh-CN">按能量费用排序。</para>
        /// </summary>
        Cost = 2,

        /// <summary>
        ///     <para xml:lang="en">Sorts alphabetically by localized card title.</para>
        ///     <para xml:lang="zh-CN">按本地化卡牌标题的字母顺序排序。</para>
        /// </summary>
        Alphabetical = 3,

        /// <summary>
        ///     <para xml:lang="en">Sorts by card rarity.</para>
        ///     <para xml:lang="zh-CN">按卡牌稀有度排序。</para>
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
