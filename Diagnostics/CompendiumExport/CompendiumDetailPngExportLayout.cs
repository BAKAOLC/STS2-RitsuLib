using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace STS2RitsuLib.Diagnostics.CompendiumExport
{
    internal static class CompendiumDetailPngExportLayout
    {
        internal const float HoverTipTargetWidth = 360f;
        private const int RefCardHoverTipVerticalGap = 4;
        private const float CardHoverTipSceneCardScale = 0.75f;
        private const string HoverTipScenePath = "res://scenes/ui/hover_tip.tscn";
        private const string CardHoverTipScenePath = "res://scenes/ui/card_hover_tip.tscn";
        private const string HoverTipDebuffMaterialPath = "res://materials/ui/hover_tip_debuff.tres";
        private const float CardExportHalfExtentX = 190f;
        private const float CardExportHalfExtentY = 240f;

        /// <summary>
        ///     Relic inspect <c>Popup</c> is often anchor-stretched; off-tree that yields 0×0 min size. Reset to
        ///     top-left with margin 0 so <see cref="Control.GetCombinedMinimumSize" /> reflects child content.
        /// </summary>
        internal static void StripToTopLeftUnstretched(Control? c)
        {
            if (c == null || !GodotObject.IsInstanceValid(c))
                return;
            c.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        }

        internal static void SetRelicInspectRarityFrame(RelicRarity rarity, ShaderMaterial frameHsv)
        {
            var h = new StringName("h");
            var s = new StringName("s");
            var v = new StringName("v");
            Vector3 vector = rarity switch
            {
                RelicRarity.None or RelicRarity.Starter or RelicRarity.Common => new(0.95f, 0.25f, 0.9f),
                RelicRarity.Uncommon => new(0.426f, 0.8f, 1.1f),
                RelicRarity.Rare => new(1f, 0.8f, 1.15f),
                RelicRarity.Shop => new(0.525f, 2.5f, 0.85f),
                RelicRarity.Event => new(0.23f, 0.75f, 0.9f),
                RelicRarity.Ancient => new(0.875f, 3f, 0.9f),
                _ => new(0.95f, 0.25f, 0.9f),
            };

            frameHsv.SetShaderParameter(h, vector.X);
            frameHsv.SetShaderParameter(s, vector.Y);
            frameHsv.SetShaderParameter(v, vector.Z);
        }

        internal static void SetRelicInspectRarityLabelColor(RelicRarity rarity, MegaLabel rarityLabel)
        {
            rarityLabel.Modulate = rarity switch
            {
                RelicRarity.None or RelicRarity.Starter or RelicRarity.Common => StsColors.cream,
                RelicRarity.Uncommon or RelicRarity.Shop => StsColors.blue,
                RelicRarity.Rare => StsColors.gold,
                RelicRarity.Event => StsColors.green,
                RelicRarity.Ancient => StsColors.red,
                _ => StsColors.cream,
            };
        }

        internal static void ApplyLabPotionVisibleStyle(NPotion nPotion, PotionModel model)
        {
            var outline = nPotion.IsNodeReady() ? nPotion.Outline : null;
            outline ??= nPotion.GetNodeOrNull<TextureRect>("%Outline");
            if (outline == null)
                return;

            foreach (var pool in ModelDb.AllCharacterPotionPools)
                if (pool.AllPotionIds.Contains(model.Id))
                {
                    var c = pool.LabOutlineColor;
                    c.A = 0.66f;
                    outline.Modulate = c;
                    return;
                }
        }

        internal static void PopulateHoverRow(VBoxContainer column, VBoxContainer? refColumn,
            IEnumerable<IHoverTip> tips, List<NCard> refHoverTipCards)
        {
            foreach (var tip in IHoverTip.RemoveDupes(tips))
                switch (tip)
                {
                    case HoverTip hoverTip:
                        AddTextHoverRow(column, hoverTip);
                        break;
                    case CardHoverTip refTip:
                        if (refColumn == null)
                            break;
                        AddGameCardHoverTip(refColumn, refTip, refHoverTipCards);
                        break;
                }
        }

        private static void AddTextHoverRow(VBoxContainer column, HoverTip hoverTip)
        {
            var control = PreloadManager.Cache.GetScene(HoverTipScenePath)
                .Instantiate<Control>();
            column.AddChild(control);

            var title = control.GetNode<MegaLabel>("%Title");
            if (hoverTip.Title == null)
                title.Visible = false;
            else
                title.SetTextAutoSize(hoverTip.Title);

            var desc = control.GetNode<MegaRichTextLabel>("%Description");
            desc.Text = hoverTip.Description;
            desc.AutowrapMode = hoverTip.ShouldOverrideTextOverflow
                ? TextServer.AutowrapMode.Off
                : TextServer.AutowrapMode.WordSmart;

            control.GetNode<TextureRect>("%Icon").Texture = hoverTip.Icon;
            if (hoverTip.IsDebuff)
                control.GetNode<CanvasItem>("%Bg").Material =
                    PreloadManager.Cache.GetMaterial(HoverTipDebuffMaterialPath);

            control.CustomMinimumSize = new(HoverTipTargetWidth, 0f);
            control.ResetSize();
        }

        private static void AddGameCardHoverTip(VBoxContainer refCardsColumn, CardHoverTip refTip,
            List<NCard> refHoverTipCardNodes)
        {
            var control = PreloadManager.Cache.GetScene(CardHoverTipScenePath).Instantiate<Control>();
            refCardsColumn.AddChild(control);

            var padded = ComputePaddedCardViewportSize(CardHoverTipSceneCardScale);
            control.CustomMinimumSize = new(padded.X, padded.Y);
            control.ResetSize();

            var node = control.GetNode<NCard>("%Card");
            node.Model = refTip.Card;
            node.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
            node.Scale = Vector2.One * CardHoverTipSceneCardScale;
            var minLocal = new Vector2(-CardExportHalfExtentX, -CardExportHalfExtentY);
            node.Position = new(
                Mathf.Round(-minLocal.X * CardHoverTipSceneCardScale),
                Mathf.Round(-minLocal.Y * CardHoverTipSceneCardScale));
            refHoverTipCardNodes.Add(node);
        }

        private static Vector2I ComputePaddedCardViewportSize(float scale)
        {
            var w = Mathf.CeilToInt(2f * CardExportHalfExtentX * scale);
            var h = Mathf.CeilToInt(2f * CardExportHalfExtentY * scale);
            return new(w, h);
        }

        internal static void ApplyRefCardExportVisuals(NCard nCard)
        {
            nCard.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
            if (nCard.Model is { IsUpgraded: true })
                nCard.ShowUpgradePreview();
        }

        internal static HBoxContainer CreateHoverRowForPotionExport()
        {
            var row = new HBoxContainer { Name = "PotionDetailHoverRow" };
            return row;
        }

        internal static (VBoxContainer text, VBoxContainer? refs) CreatePotionHoverColumns()
        {
            var textTips = new VBoxContainer { Name = "PotionTextTips" };
            textTips.AddThemeConstantOverride("separation", Mathf.RoundToInt(5f));
            var refCol = new VBoxContainer { Name = "PotionRefCardHoverTips" };
            refCol.AddThemeConstantOverride("separation", RefCardHoverTipVerticalGap);
            return (textTips, refCol);
        }
    }
}
