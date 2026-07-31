using Godot;

namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Captures the compendium filter strip before RitsuLib inserts mod filters.
    ///     </para>
    ///     <para xml:lang="zh-CN">捕获 RitsuLib 插入模组筛选器前的图鉴筛选器条。</para>
    /// </summary>
    internal sealed class CardLibraryCompendiumStripSnapshot
    {
        private CardLibraryCompendiumStripSnapshot(IReadOnlyList<Node> siblingsInOrder)
        {
            OriginalSiblingsInOrder = siblingsInOrder;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the original child sequence from left to right.</para>
        ///     <para xml:lang="zh-CN">获取从左到右排列的原始子节点序列。</para>
        /// </summary>
        public IReadOnlyList<Node> OriginalSiblingsInOrder { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the number of children in the snapshot.</para>
        ///     <para xml:lang="zh-CN">获取快照中的子节点数量。</para>
        /// </summary>
        public int Count => OriginalSiblingsInOrder.Count;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Captures the current children of <paramref name="filterParent" /> in sibling order.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按同级顺序捕获 <paramref name="filterParent" /> 的当前子节点。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Tries to get the original sibling index of <paramref name="node" /> by reference.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试按引用获取 <paramref name="node" /> 的原始同级索引。
        ///     </para>
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
