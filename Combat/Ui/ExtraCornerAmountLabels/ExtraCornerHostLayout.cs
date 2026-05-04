using Godot;
using MegaCrit.Sts2.addons.mega_text;

namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    internal enum ExtraCornerHostKind
    {
        Power,
        Relic,
        Intent,
    }

    internal static class ExtraCornerHostLayout
    {
        internal static void ApplySlotBounds(MegaLabel label, ExtraCornerHostKind host,
            in ExtraIconAmountLabelSlot slot)
        {
            label.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
            var (l, t, r, b) = ResolveRect(host, in slot);
            label.OffsetLeft = l;
            label.OffsetTop = t;
            label.OffsetRight = r;
            label.OffsetBottom = b;
        }

        internal static void ApplySlotAlignment(MegaLabel label, ExtraCornerHostKind host,
            in ExtraIconAmountLabelSlot slot)
        {
            if (slot.Corner == ExtraIconAmountLabelCorner.Custom)
            {
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment = VerticalAlignment.Center;
                return;
            }

            label.HorizontalAlignment = slot.Corner switch
            {
                ExtraIconAmountLabelCorner.TopLeft or ExtraIconAmountLabelCorner.BottomLeft => HorizontalAlignment.Left,
                ExtraIconAmountLabelCorner.TopRight => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Center,
            };

            label.VerticalAlignment = host == ExtraCornerHostKind.Relic &&
                                      slot.Corner == ExtraIconAmountLabelCorner.BottomLeft
                ? VerticalAlignment.Bottom
                : VerticalAlignment.Center;
        }

        private static (float L, float T, float R, float B) ResolveRect(ExtraCornerHostKind host,
            in ExtraIconAmountLabelSlot slot)
        {
            if (slot.Corner != ExtraIconAmountLabelCorner.Custom)
                return (host, slot.Corner) switch
                {
                    (ExtraCornerHostKind.Power, ExtraIconAmountLabelCorner.TopLeft) => (0f, 0f, 22f, 24f),
                    (ExtraCornerHostKind.Power, ExtraIconAmountLabelCorner.TopRight) => (18f, 0f, 44f, 24f),
                    (ExtraCornerHostKind.Power, ExtraIconAmountLabelCorner.BottomLeft) => (0f, 21f, 22f, 40f),
                    (ExtraCornerHostKind.Relic, ExtraIconAmountLabelCorner.TopLeft) => (4f, 4f, 34f, 32f),
                    (ExtraCornerHostKind.Relic, ExtraIconAmountLabelCorner.TopRight) => (34f, 4f, 64f, 32f),
                    (ExtraCornerHostKind.Relic, ExtraIconAmountLabelCorner.BottomLeft) => (4f, 36f, 32f, 67f),
                    (ExtraCornerHostKind.Intent, ExtraIconAmountLabelCorner.TopLeft) => (2f, 2f, 32f, 36f),
                    (ExtraCornerHostKind.Intent, ExtraIconAmountLabelCorner.TopRight) => (32f, 2f, 64f, 36f),
                    (ExtraCornerHostKind.Intent, ExtraIconAmountLabelCorner.BottomLeft) => (2f, 40f, 32f, 63f),
                    _ => throw new ArgumentOutOfRangeException(nameof(slot), slot.Corner,
                        "Unexpected corner for host."),
                };
            var r = slot.CustomRect;
            return (r.Position.X, r.Position.Y, r.Position.X + r.Size.X, r.Position.Y + r.Size.Y);
        }
    }
}
