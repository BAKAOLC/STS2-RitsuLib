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
    ///     Helper class for adding hover tips
    /// </summary>
    public static class HoverTipHelper
    {
        private const float HoverTipSpacing = 5f;
        private const float HoverTipWidth = 360f;

        /// <summary>
        ///     Appends a text hover tip to <paramref name="owner" />'s active hover tip set.
        /// </summary>
        /// <returns>False when no hover tip set is bound to the control.</returns>
        public static bool AddTipToOwner(Control owner, string title, string description)
        {
            return NHoverTipSet._activeHoverTips.TryGetValue(owner, out var hoverTipSet) &&
                   AddTipToSet(hoverTipSet, owner, title, description);
        }

        /// <summary>
        ///     Appends card preview hover tips for <paramref name="cards" /> to <paramref name="owner" />.
        /// </summary>
        /// <returns>False when no hover tip set is bound or no tips were added.</returns>
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
                var key = card.Id + (card.IsUpgraded ? "+" : string.Empty);
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
