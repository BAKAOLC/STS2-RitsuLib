using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace STS2RitsuLib
{
    /// <summary>
    ///     <para xml:lang="en">Provides helpers for appending hover tips to active game UI controls.</para>
    ///     <para xml:lang="zh-CN">提供用于向活动游戏界面控件追加悬停提示的辅助方法。</para>
    /// </summary>
    public static class HoverTipHelper
    {
        private const float HoverTipSpacing = 5f;
        private const float HoverTipWidth = 360f;

        /// <summary>
        ///     <para xml:lang="en">Appends a text hover tip to <paramref name="owner" />'s active hover-tip set.</para>
        ///     <para xml:lang="zh-CN">向 <paramref name="owner" /> 的活动悬停提示集合追加文本悬停提示。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="false" /> when no hover-tip set is bound to the control or the set has no text-tip container.</para>
        ///     <para xml:lang="zh-CN">该控件未绑定悬停提示集合，或集合没有文本提示容器时为 <see langword="false" />。</para>
        /// </returns>
        public static bool AddTipToOwner(Control owner, string title, string description)
        {
            return NHoverTipSet._activeHoverTips.TryGetValue(owner, out var hoverTipSet) &&
                   AddTipToSet(hoverTipSet, owner, title, description);
        }

        /// <summary>
        ///     <para xml:lang="en">Appends card-preview hover tips for <paramref name="cards" /> to <paramref name="owner" />.</para>
        ///     <para xml:lang="zh-CN">向 <paramref name="owner" /> 追加 <paramref name="cards" /> 的卡牌预览悬停提示。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="false" /> when no hover-tip set is bound or no tip was added.</para>
        ///     <para xml:lang="zh-CN">未绑定悬停提示集合或未追加任何提示时为 <see langword="false" />。</para>
        /// </returns>
        public static bool AddCardTipsToOwner(Control owner, IEnumerable<CardModel> cards)
        {
            return NHoverTipSet._activeHoverTips.TryGetValue(owner, out var hoverTipSet) &&
                   AddCardTipsToSet(hoverTipSet, owner, cards);
        }

        private static bool AddTipToSet(NHoverTipSet hoverTipSet, Control owner, string title, string description)
        {
            var container = hoverTipSet._textHoverTipContainer;
            if (container == null) return false;

            var tipScene = PreloadManager.Cache.GetScene("res://scenes/ui/hover_tip.tscn");
            var tipControl = tipScene.Instantiate<Control>();

            container.AddChildSafely(tipControl);

            var titleLabel = tipControl.GetNode<MegaLabel>("%Title");
            if (string.IsNullOrEmpty(title))
                titleLabel.Visible = false;
            else
                titleLabel.SetTextAutoSize(title);

            tipControl.GetNode<MegaRichTextLabel>("%Description").Text = description;
            tipControl.GetNode<TextureRect>("%Icon").Texture = null;
            tipControl.ResetSize();

            if (NGame.Instance == null) return true;

            var viewportHeight = NGame.Instance.GetViewportRect().Size.Y;
            if (container.Size.Y + tipControl.Size.Y + HoverTipSpacing < viewportHeight - 50f)
                container.Size = new(HoverTipWidth, container.Size.Y + tipControl.Size.Y + HoverTipSpacing);
            else
                container.Alignment = FlowContainer.AlignmentMode.Center;

            hoverTipSet.SetAlignment(owner, HoverTipAlignment.None);

            return true;
        }

        private static bool AddCardTipsToSet(NHoverTipSet hoverTipSet, Control owner, IEnumerable<CardModel> cards)
        {
            var cardContainer = hoverTipSet._cardHoverTipContainer;
            if (cardContainer == null) return false;

            var seen = new HashSet<string>();
            var added = false;
            foreach (var card in cards)
            {
                var key = card.CurrentUpgradeLevel <= 0
                    ? card.Id.ToString()
                    : card.MaxUpgradeLevel > 1
                        ? $"{card.Id}+{card.CurrentUpgradeLevel}"
                        : $"{card.Id}+";
                if (!seen.Add(key)) continue;

                cardContainer.Add(new(card));
                added = true;
            }

            if (!added) return false;

            hoverTipSet.SetAlignment(owner, HoverTipAlignment.None);
            return true;
        }
    }
}
