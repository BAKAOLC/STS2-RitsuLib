using Godot;

namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     Internal: siblings of the compendium filter parent captured before RitsuLib inserts mod pool filters.
    ///     Resolves snapshot indices for vanilla anchors; mod authors should use
    ///     <see cref="CardLibraryCompendiumVanillaFilterNames" /> in placement rules, not this type.
    ///     内部：RitsuLib 插入 mod 池筛选器之前捕获的概要筛选器父节点同级项。
    ///     为原版锚点解析快照索引；mod 作者应在放置规则中使用
    ///     <see cref="CardLibraryCompendiumVanillaFilterNames" />，而不是此类型。
    /// </summary>
    internal sealed class CardLibraryCompendiumStripSnapshot
    {
        private CardLibraryCompendiumStripSnapshot(IReadOnlyList<Node> siblingsInOrder)
        {
            OriginalSiblingsInOrder = siblingsInOrder;
        }

        /// <summary>
        ///     Original child sequence under the filter parent (0 = leftmost in the compendium strip as built by
        ///     the base game, before RitsuLib inserts any mod filter nodes).
        ///     筛选器父节点下的原始子节点序列（0 = 基础游戏构建的概要条中最左侧，
        ///     在 RitsuLib 插入任何 mod 筛选器节点之前）。
        /// </summary>
        public IReadOnlyList<Node> OriginalSiblingsInOrder { get; }

        /// <summary>
        ///     Sibling count at snapshot time. Valid insertion indices for “into this list” simulation range from
        ///     0 to <c>Count</c> (inclusive of appending as <c>Count</c>).
        ///     快照时的同级数量。“插入此列表”模拟的有效插入索引范围为
        ///     0 到 <c>Count</c>（包括以 <c>Count</c> 追加）。
        /// </summary>
        public int Count => OriginalSiblingsInOrder.Count;

        /// <summary>
        ///     Captures the current <paramref name="filterParent" /> children as an ordered list.
        ///     将当前 <paramref name="filterParent" /> 子节点捕获为有序列表。
        /// </summary>
        public static CardLibraryCompendiumStripSnapshot Capture(Node filterParent)
        {
            ArgumentNullException.ThrowIfNull(filterParent);
            var n = filterParent.GetChildCount();
            var list = new List<Node>(n);
            for (var i = 0; i < n; i++)
                list.Add(filterParent.GetChild(i));
            return new(list);
        }

        /// <summary>
        ///     Returns the sibling index of <paramref name="node" /> if it is a direct child in this snapshot
        ///     (reference match).
        ///     如果 <paramref name="node" /> 是此快照中的直接子节点，则返回其同级索引
        ///     （引用匹配）。
        /// </summary>
        public bool TryGetIndexOfNode(Node? node, out int index)
        {
            if (node is null)
            {
                index = -1;
                return false;
            }

            for (var i = 0; i < OriginalSiblingsInOrder.Count; i++)
                if (ReferenceEquals(OriginalSiblingsInOrder[i], node))
                {
                    index = i;
                    return true;
                }

            index = -1;
            return false;
        }
    }
}
