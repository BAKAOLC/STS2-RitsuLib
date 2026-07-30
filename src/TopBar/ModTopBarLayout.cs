using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.CardPiles.Nodes;
using NVec2 = System.Numerics.Vector2;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Places mod card-pile and action buttons in the vanilla top bar relative to the deck slot.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         相对于原版牌组槽位放置模组牌堆按钮和操作按钮。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         The right-side controls are children of an <see cref="HBoxContainer" /> rather than direct
    ///         children of <see cref="NTopBar" />. This helper reparents each button and adjusts its child
    ///         index; the container then determines its screen position.
    ///     </para>
    ///     <para xml:lang="en">
    ///         Repeated calls that place a button before the same anchor put the most recently placed
    ///         button closest to that anchor. Callers should therefore place ordered items in reverse.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         顶部栏右侧控件是 <see cref="HBoxContainer" /> 的子节点，并非
    ///         <see cref="NTopBar" /> 的直接子节点。此辅助类会重新设置按钮的父节点并调整子节点索引，
    ///         再由容器决定实际屏幕位置。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         连续将按钮放在同一锚点之前时，最后放置的按钮最靠近锚点。因此调用方应按目标顺序的
    ///         逆序放置。
    ///     </para>
    /// </remarks>
    public static class ModTopBarLayout
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Finds the nearest <see cref="HBoxContainer" /> containing <see cref="NTopBar.Deck" />,
        ///         or the deck button's immediate parent when no such container exists.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         查找包含 <see cref="NTopBar.Deck" /> 的最近 <see cref="HBoxContainer" />；
        ///         不存在时返回牌组按钮的直接父控件。
        ///     </para>
        /// </summary>
        public static Control? GetRightAlignedContainer(NTopBar topBar)
        {
            ArgumentNullException.ThrowIfNull(topBar);
            var deck = topBar.Deck;
            if (deck == null)
                return null;
            var cursor = deck.GetParent();
            while (cursor is { } node)
            {
                if (node is HBoxContainer hbox)
                    return hbox;
                cursor = node.GetParent();
            }

            return deck.GetParent() as Control;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the direct child of the right-aligned container that contains
        ///         <see cref="NTopBar.Deck" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回右对齐容器中包含 <see cref="NTopBar.Deck" /> 的直接子节点。
        ///     </para>
        /// </summary>
        public static Node? GetDeckSlotAnchor(NTopBar topBar)
        {
            var container = GetRightAlignedContainer(topBar);
            var deck = topBar.Deck;
            if (container == null || deck == null)
                return null;
            Node cursor = deck;
            while (cursor.GetParent() is { } parent && parent != container)
                cursor = parent;
            return cursor.GetParent() == container ? cursor : null;
        }

        /// <summary>
        ///     <para xml:lang="en">Places <paramref name="button" /> immediately before the deck slot.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="button" /> 放在牌组槽位之前的相邻位置。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="false" /> when the deck container is unavailable; otherwise
        ///         <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         牌组容器不可用时为 <see langword="false" />；否则为 <see langword="true" />。
        ///     </para>
        /// </returns>
        public static bool Place(NTopBar topBar, NModCardPileButton button, Vector2 offset = default)
        {
            ArgumentNullException.ThrowIfNull(topBar);
            ArgumentNullException.ThrowIfNull(button);

            var container = GetRightAlignedContainer(topBar);
            var anchor = GetDeckSlotAnchor(topBar);
            if (container == null || anchor == null)
                return false;

            return PlaceBeforeAnchor(container, anchor, button, offset);
        }

        /// <summary>
        ///     <para xml:lang="en">Places <paramref name="button" /> immediately after the deck slot.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="button" /> 放在牌组槽位之后的相邻位置。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="false" /> when the deck container is unavailable; otherwise
        ///         <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         牌组容器不可用时为 <see langword="false" />；否则为 <see langword="true" />。
        ///     </para>
        /// </returns>
        public static bool PlaceAfterDeck(NTopBar topBar, NModCardPileButton button, Vector2 offset = default)
        {
            ArgumentNullException.ThrowIfNull(topBar);
            ArgumentNullException.ThrowIfNull(button);

            var container = GetRightAlignedContainer(topBar);
            var anchor = GetDeckSlotAnchor(topBar);
            if (container == null || anchor == null)
                return false;

            return PlaceAfterAnchor(container, anchor, button, offset);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Places <paramref name="button" /> immediately before the modifiers slot, falling back to
        ///         the position before the deck slot.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <paramref name="button" /> 放在特效槽位之前；该槽位不可用时回退到牌组槽位之前。
        ///     </para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="false" /> when neither anchor is available; otherwise
        ///         <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         两个锚点都不可用时为 <see langword="false" />；否则为 <see langword="true" />。
        ///     </para>
        /// </returns>
        public static bool PlaceBeforeModifiers(NTopBar topBar, NModCardPileButton button, Vector2 offset = default)
        {
            ArgumentNullException.ThrowIfNull(topBar);
            ArgumentNullException.ThrowIfNull(button);

            var container = GetRightAlignedContainer(topBar);
            if (container == null)
                return false;

            var modifiers = topBar.GetNodeOrNull<Control>("%Modifiers");
            if (modifiers == null)
                return Place(topBar, button, offset);

            Node anchor = modifiers;
            while (anchor.GetParent() is { } parent && parent != container)
                anchor = parent;
            return anchor.GetParent() != container
                ? Place(topBar, button, offset)
                : PlaceBeforeAnchor(container, anchor, button, offset);
        }

        private static bool PlaceBeforeAnchor(Control container, Node anchor, NModCardPileButton button, Vector2 offset)
        {
            AttachToContainer(container, button);

            var anchorIndex = anchor.GetIndex();
            var currentIndex = button.GetIndex();
            var targetIndex = currentIndex < anchorIndex ? anchorIndex - 1 : anchorIndex;
            if (currentIndex != targetIndex)
                container.MoveChild(button, targetIndex);

            button.ApplyVisualOffset(offset);
            return true;
        }

        private static bool PlaceAfterAnchor(Control container, Node anchor, NModCardPileButton button, Vector2 offset)
        {
            AttachToContainer(container, button);

            var anchorIndex = anchor.GetIndex();
            var currentIndex = button.GetIndex();
            var targetIndex = currentIndex < anchorIndex ? anchorIndex : anchorIndex + 1;
            if (currentIndex != targetIndex)
                container.MoveChild(button, targetIndex);

            button.ApplyVisualOffset(offset);
            return true;
        }

        private static void AttachToContainer(Control container, NModCardPileButton button)
        {
            if (button.GetParent() != container)
            {
                button.GetParent()?.RemoveChild(button);
                container.AddChildSafely(button);
            }

            button.Position = Vector2.Zero;
            button.Scale = Vector2.One;
        }

        /// <summary>
        ///     <para xml:lang="en">Places a button using a <see cref="NVec2" /> visual offset.</para>
        ///     <para xml:lang="zh-CN">使用 <see cref="NVec2" /> 类型的视觉偏移放置按钮。</para>
        /// </summary>
        public static bool Place(NTopBar topBar, NModCardPileButton button, NVec2 offset)
        {
            return Place(topBar, button, new Vector2(offset.X, offset.Y));
        }
    }
}
