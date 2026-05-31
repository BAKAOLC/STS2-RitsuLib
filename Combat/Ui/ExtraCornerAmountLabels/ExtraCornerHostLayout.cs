using Godot;

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
        private const float PowerAmountLabelLeft = -56f;
        private const float PowerAmountLabelRight = 44f;
        private const float PowerAmountLabelWidth = PowerAmountLabelRight - PowerAmountLabelLeft;
        private const float PowerAmountLabelBottomRowTop = 21f;
        private const float PowerAmountLabelBottomRowBottom = 44f;
        private const float PowerAmountLabelTopRowTop = 0f;
        private const float PowerAmountLabelTopRowBottom = 23f;
        private const float RelicAmountLabelLeft = 32f;
        private const float RelicAmountLabelTop = 35f;
        private const float RelicAmountLabelRight = 64f;
        private const float RelicAmountLabelBottom = 67f;
        private const float RelicAmountLabelWidth = RelicAmountLabelRight - RelicAmountLabelLeft;
        private const float RelicAmountLabelHeight = RelicAmountLabelBottom - RelicAmountLabelTop;
        private const float RelicAmountLabelMirrorLeft = 4f;
        private const float RelicAmountLabelMirrorTop = 4f;
        private const float IntentValueLabelLeft = 2f;
        private const float IntentValueLabelTop = 40f;
        private const float IntentValueLabelRight = 64f;
        private const float IntentValueLabelBottom = 63f;
        private const float IntentValueLabelHeight = IntentValueLabelBottom - IntentValueLabelTop;
        private const float IntentValueLabelMirrorTop = 1f;

        internal static void ApplySlotBounds(Control label, ExtraCornerHostKind host,
            in ExtraIconAmountLabelSpec slot)
        {
            label.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
            var (l, t, r, b) = ResolveRect(host, in slot);
            label.OffsetLeft = l;
            label.OffsetTop = t;
            label.OffsetRight = r;
            label.OffsetBottom = b;
        }

        internal static void ApplySlotAlignment(Control label, ExtraCornerHostKind host,
            in ExtraIconAmountLabelSpec slot)
        {
            if (label is not Label plainLabel)
                return;

            if (slot.Corner == ExtraIconAmountLabelCorner.Custom)
            {
                plainLabel.HorizontalAlignment = HorizontalAlignment.Center;
                plainLabel.VerticalAlignment = VerticalAlignment.Center;
                return;
            }

            plainLabel.HorizontalAlignment = slot.Corner switch
            {
                ExtraIconAmountLabelCorner.TopLeft or ExtraIconAmountLabelCorner.BottomLeft => HorizontalAlignment.Left,
                ExtraIconAmountLabelCorner.TopRight or ExtraIconAmountLabelCorner.BottomRight => HorizontalAlignment
                    .Right,
                _ => HorizontalAlignment.Center,
            };

            plainLabel.VerticalAlignment = host switch
            {
                ExtraCornerHostKind.Relic => slot.Corner switch
                {
                    ExtraIconAmountLabelCorner.TopLeft or ExtraIconAmountLabelCorner.TopRight =>
                        VerticalAlignment.Top,
                    ExtraIconAmountLabelCorner.BottomLeft or ExtraIconAmountLabelCorner.BottomRight =>
                        VerticalAlignment.Bottom,
                    _ => VerticalAlignment.Center,
                },
                ExtraCornerHostKind.Intent => slot.Corner switch
                {
                    ExtraIconAmountLabelCorner.TopLeft or ExtraIconAmountLabelCorner.TopRight =>
                        VerticalAlignment.Top,
                    ExtraIconAmountLabelCorner.BottomLeft or ExtraIconAmountLabelCorner.BottomRight =>
                        VerticalAlignment.Top,
                    _ => VerticalAlignment.Center,
                },
                _ => VerticalAlignment.Center,
            };
        }

        private static (float L, float T, float R, float B) ResolveRect(ExtraCornerHostKind host,
            in ExtraIconAmountLabelSpec slot)
        {
            if (slot.Corner != ExtraIconAmountLabelCorner.Custom)
                return (host, slot.Corner) switch
                {
                    (ExtraCornerHostKind.Power, ExtraIconAmountLabelCorner.TopLeft) =>
                        (0f, PowerAmountLabelTopRowTop, PowerAmountLabelWidth, PowerAmountLabelTopRowBottom),
                    (ExtraCornerHostKind.Power, ExtraIconAmountLabelCorner.TopRight) =>
                        (PowerAmountLabelLeft, PowerAmountLabelTopRowTop, PowerAmountLabelRight,
                            PowerAmountLabelTopRowBottom),
                    (ExtraCornerHostKind.Power, ExtraIconAmountLabelCorner.BottomLeft) =>
                        (0f, PowerAmountLabelBottomRowTop, PowerAmountLabelWidth, PowerAmountLabelBottomRowBottom),
                    (ExtraCornerHostKind.Power, ExtraIconAmountLabelCorner.BottomRight) =>
                        (PowerAmountLabelLeft, PowerAmountLabelBottomRowTop, PowerAmountLabelRight,
                            PowerAmountLabelBottomRowBottom),
                    (ExtraCornerHostKind.Relic, ExtraIconAmountLabelCorner.TopLeft) =>
                        (RelicAmountLabelMirrorLeft, RelicAmountLabelMirrorTop,
                            RelicAmountLabelMirrorLeft + RelicAmountLabelWidth,
                            RelicAmountLabelMirrorTop + RelicAmountLabelHeight),
                    (ExtraCornerHostKind.Relic, ExtraIconAmountLabelCorner.TopRight) =>
                        (RelicAmountLabelLeft, RelicAmountLabelMirrorTop, RelicAmountLabelRight,
                            RelicAmountLabelMirrorTop + RelicAmountLabelHeight),
                    (ExtraCornerHostKind.Relic, ExtraIconAmountLabelCorner.BottomLeft) =>
                        (RelicAmountLabelMirrorLeft, RelicAmountLabelTop,
                            RelicAmountLabelMirrorLeft + RelicAmountLabelWidth, RelicAmountLabelBottom),
                    (ExtraCornerHostKind.Relic, ExtraIconAmountLabelCorner.BottomRight) =>
                        (RelicAmountLabelLeft, RelicAmountLabelTop, RelicAmountLabelRight,
                            RelicAmountLabelBottom),
                    (ExtraCornerHostKind.Intent, ExtraIconAmountLabelCorner.TopLeft) =>
                        (IntentValueLabelLeft, IntentValueLabelMirrorTop, IntentValueLabelRight,
                            IntentValueLabelMirrorTop + IntentValueLabelHeight),
                    (ExtraCornerHostKind.Intent, ExtraIconAmountLabelCorner.TopRight) =>
                        (IntentValueLabelLeft, IntentValueLabelMirrorTop, IntentValueLabelRight,
                            IntentValueLabelMirrorTop + IntentValueLabelHeight),
                    (ExtraCornerHostKind.Intent, ExtraIconAmountLabelCorner.BottomLeft) =>
                        (IntentValueLabelLeft, IntentValueLabelTop, IntentValueLabelRight, IntentValueLabelBottom),
                    (ExtraCornerHostKind.Intent, ExtraIconAmountLabelCorner.BottomRight) =>
                        (IntentValueLabelLeft, IntentValueLabelTop, IntentValueLabelRight, IntentValueLabelBottom),
                    _ => throw new ArgumentOutOfRangeException(nameof(slot), slot.Corner,
                        "Unexpected corner for host."),
                };
            var r = slot.CustomRect;
            return (r.Position.X, r.Position.Y, r.Position.X + r.Size.X, r.Position.Y + r.Size.Y);
        }
    }
}
