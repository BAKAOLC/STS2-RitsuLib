using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.CardPiles.Nodes;
using NVec2 = System.Numerics.Vector2;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     Single source of truth for where mod top-bar buttons land relative to the vanilla
    ///     <c>%Deck</c> button. Both pile-backed
    ///     <see cref="STS2RitsuLib.CardPiles.ModCardPileUiStyle.TopBarDeck" /> buttons and
    ///     action-backed <see cref="ModTopBarButtonRegistry" /> buttons funnel through this helper so
    ///     the two systems cannot disagree about slot ordering / direction — addressing the user
    ///     feedback that having <i>two</i> independent layout algorithms was a "meaningless split".
    ///     mod 顶部栏按钮相对于原版
    ///     <c>%Deck</c> 按钮落位位置的单一事实来源。牌堆支持的
    ///     <see cref="STS2RitsuLib.CardPiles.ModCardPileUiStyle.TopBarDeck" /> 按钮和
    ///     动作支持的 <see cref="ModTopBarButtonRegistry" /> 按钮都会经过此辅助类，使
    ///     两个系统不会在槽顺序 / 方向上产生分歧；这是对用户
    ///     反馈“有<i>两个</i>独立布局算法是无意义拆分”的回应。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The actual layout container for the right-side cluster is
    ///         <c>RootSceneContainer/Run/GlobalUi/TopBar/RightAlignedStuff</c> — and crucially it is
    ///         an <see cref="HBoxContainer" /> (<c>separation=0</c>, <c>alignment=end</c>) that
    ///         auto-lays out its children left-to-right in registration order. The canonical vanilla
    ///         child sequence is <c>[SaveIndicator][Padding][TimerContainer][Map][DeckContainer][PauseButton]</c>
    ///         — so to drop a mod button "just to the left of the deck" we need two things:
    ///         <list type="number">
    ///             <item>the button must be parented under this HBoxContainer, and</item>
    ///             <item>its child index must be immediately before <c>DeckContainer</c>'s index.</item>
    ///         </list>
    ///         Because the container is auto-laid-out, we <b>do not</b> set
    ///         <see cref="Control.Position" /> ourselves — doing so would fight the HBoxContainer and
    ///         produce the "button appears nowhere visible" bug from the previous iteration.
    ///     </para>
    ///     <para>
    ///         Slot ordering: mod buttons are inserted in registration order, each one claiming the
    ///         slot <i>immediately before</i> the deck container / deck button. The newest
    ///         registration ends up closest to the deck; the earliest registration ends up furthest
    ///         left. This keeps every mod button contiguous with the vanilla deck icon and avoids
    ///         splitting them into "pile row" / "action row".
    ///     </para>
    ///     <para>
    ///         右侧集群的实际布局容器是
    ///         <c>RootSceneContainer/Run/GlobalUi/TopBar/RightAlignedStuff</c>；关键是它是
    ///         <see cref="HBoxContainer" />（<c>separation=0</c>、<c>alignment=end</c>），会
    ///         按注册顺序从左到右自动布局子节点。规范的原版
    ///         子节点顺序是 <c>[SaveIndicator][Padding][TimerContainer][Map][DeckContainer][PauseButton]</c>；
    ///         因此，要把 mod 按钮放到“牌组左侧紧邻位置”，需要两点：
    ///         <list type="number">
    ///             <item>按钮必须挂到此 HBoxContainer 下，且</item>
    ///             <item>其子索引必须紧邻 <c>DeckContainer</c> 索引之前。</item>
    ///         </list>
    ///         因为容器会自动布局，我们<b>不会</b>自行设置
    ///         <see cref="Control.Position" />；这样做会与 HBoxContainer 冲突，并
    ///         产生上一轮中“按钮没有出现在可见位置”的 bug。
    ///     </para>
    ///     <para>
    ///         槽顺序：mod 按钮按注册顺序插入，每个按钮占用
    ///         牌组容器/牌组按钮<i>正前方</i>的槽。最新
    ///         注册最终最接近牌组；最早注册最终最靠
    ///         左。这让每个 mod 按钮都与原版牌组图标连续，并避免
    ///         拆成“牌堆行”/“动作行”。
    ///         拆成“牌堆行”/“动作行”。
    ///     </para>
    /// </remarks>
    public static class ModTopBarLayout
    {
        /// <summary>
        ///     Returns the real right-side layout container (<c>RightAlignedStuff</c>, an
        ///     <see cref="HBoxContainer" />) — the parent of either <c>%Deck</c> directly or the
        ///     <c>DeckContainer</c> that wraps it. Returns null when the top bar hasn't resolved
        ///     <see cref="NTopBar.Deck" /> yet.
        ///     返回真实的右侧布局容器（<c>RightAlignedStuff</c>，一个
        ///     <see cref="HBoxContainer" />），它是 <c>%Deck</c> 的直接父节点，或是包裹它的
        ///     <c>DeckContainer</c> 的父节点。当顶部栏尚未解析
        ///     <see cref="NTopBar.Deck" /> 时返回 null。
        /// </summary>
        public static Control? GetRightAlignedContainer(NTopBar topBar)
        {
            ArgumentNullException.ThrowIfNull(topBar);
            var deck = topBar.Deck;
            if (deck == null)
                return null;
            // The deck may sit inside a `DeckContainer` MarginContainer; walk up until we hit
            // the HBoxContainer so the lookup is stable across scene refactors that add / remove the
            // wrapper.
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
        ///     Returns the direct child of the right-aligned container that ultimately contains
        ///     <c>%Deck</c> — often the <c>DeckContainer</c> <see cref="MarginContainer" />
        ///     that wraps the deck button. We insert mod buttons immediately <i>before</i> this node
        ///     so they land "just to the left of the deck", matching the existing vanilla placement
        ///     of Map / DeckContainer / PauseButton.
        ///     of Map / DeckContainer / PauseButton.
        ///     返回右对齐容器的直接子节点，该节点最终包含
        ///     <c>%Deck</c>；通常是包裹牌组按钮的 <c>DeckContainer</c> <see cref="MarginContainer" />。
        ///     我们会把 mod 按钮插到此节点<i>之前</i>，
        ///     让它们落在“牌组左侧紧邻位置”，匹配现有原版
        ///     Map / DeckContainer / PauseButton 的放置方式。
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
        ///     Attaches <paramref name="button" /> to the right-aligned container (re-parenting when
        ///     necessary) and orders it so it sits immediately to the <b>left</b> of the deck-slot
        ///     anchor. The button is sized to match one vanilla top-bar slot
        ///     (<c>80×80</c> minimum) and left at <see cref="Vector2.Zero" /> <see cref="Control.Position" />
        ///     — the enclosing <see cref="HBoxContainer" /> drives the actual screen position.
        ///     Returns true on success, false when the top bar isn't ready yet (caller should retry).
        ///     将 <paramref name="button" /> 附加到右对齐容器（必要时重新挂父），
        ///     并排序，使其立即位于牌组槽锚点的<b>左侧</b>。
        ///     按钮会调整为匹配一个原版顶部栏槽
        ///     （<c>80×80</c> 最小值），并将 <see cref="Vector2.Zero" /> <see cref="Control.Position" /> 保持不变；
        ///     实际屏幕位置由外层 <see cref="HBoxContainer" /> 驱动。
        ///     实际屏幕位置由外层 <c>HBoxContainer</c> 驱动。
        ///     成功时返回 true；顶部栏尚未就绪时返回 false（调用方应重试）。
        /// </summary>
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
        ///     Attaches <paramref name="button" /> to the right-aligned container and places it immediately
        ///     <b>after</b> the deck slot anchor (typically between deck and pause).
        ///     将 <paramref name="button" /> 附加到右对齐容器，并将其立即放在
        ///     牌组槽锚点<b>之后</b>（通常位于牌组和暂停之间）。
        /// </summary>
        /// <returns>
        ///     False when the top bar anchor nodes are not ready yet.
        ///     顶部栏锚点节点尚未就绪时为 false。
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
        ///     Attaches <paramref name="button" /> to the right-aligned container and places it immediately
        ///     <b>before</b> the modifiers slot when available; falls back to the default deck-left placement.
        ///     将 <paramref name="button" /> 附加到右对齐容器，并在可用时将其立即放在
        ///     modifiers 槽<b>之前</b>；否则回退到默认的牌组左侧放置。
        /// </summary>
        /// <returns>
        ///     False only when all candidate anchors are unavailable.
        ///     仅当时为 false 所有候选锚点不可用。
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
        ///     System.Numerics-flavoured overload for callers (e.g. <see cref="ModTopBarButtonSpec" />)
        ///     that carry offsets as <see cref="NVec2" />.
        ///     面向以 <see cref="NVec2" /> 携带偏移的调用方（例如 <see cref="ModTopBarButtonSpec" />）
        ///     的 System.Numerics 风格重载。
        /// </summary>
        public static bool Place(NTopBar topBar, NModCardPileButton button, NVec2 offset)
        {
            return Place(topBar, button, new Vector2(offset.X, offset.Y));
        }
    }
}
