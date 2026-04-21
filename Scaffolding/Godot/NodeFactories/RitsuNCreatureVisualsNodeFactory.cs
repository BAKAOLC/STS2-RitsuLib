using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace STS2RitsuLib.Scaffolding.Godot.NodeFactories
{
    /// <summary>
    ///     Builds <see cref="NCreatureVisuals" /> from vanilla-style scenes or a <see cref="Texture2D" /> (Sprite2D body).
    ///     Non-Spine combat playback remains handled by <see cref="Characters.Visuals.ModCreatureVisualPlayback" />.
    ///     Does not synthesize <c>%OrbPos</c> / <c>%TalkPos</c> when absent: <see cref="NCreatureVisuals" /> uses
    ///     <c>IntentPos</c> and <c>null</c> for those, matching baselib <c>NCreatureVisualsFactory</c> and vanilla.
    /// </summary>
    internal sealed class RitsuNCreatureVisualsNodeFactory() : RitsuGodotNodeFactory<NCreatureVisuals>([
        new RitsuGodotNodeSlot<Node2D>("%Visuals"),
        new RitsuGodotNodeSlot<Node2D>("%PhobiaModeVisuals"),
        new RitsuGodotNodeSlot<Control>("Bounds", false),
        new RitsuGodotNodeSlot<Marker2D>("%CenterPos"),
        new RitsuGodotNodeSlot<Marker2D>("IntentPos", false),
    ])
    {
        protected override NCreatureVisuals CreateBareFromResourceImpl(object resource)
        {
            return resource switch
            {
                Texture2D img => FromTexture(img),
                _ => throw new NotSupportedException(
                    $"RitsuNCreatureVisualsNodeFactory does not support resource type {resource.GetType().Name}. Use Texture2D or a registered scene path."),
            };
        }

        private static NCreatureVisuals FromTexture(Texture2D img)
        {
            var imgSize = img.GetSize();
            var boundsSize = img.GetSize() * 1.1f;

            var root = new NCreatureVisuals();

            var bounds = new Control();
            root.AddUniqueChild(bounds, "Bounds");
            bounds.Position = new(-boundsSize.X / 2, -boundsSize.Y);
            bounds.Size = boundsSize;

            var visuals = new Sprite2D();
            root.AddUniqueChild(visuals, "Visuals");
            visuals.Texture = img;
            visuals.Position = new(0, -imgSize.Y * 0.5f);

            return root;
        }

        protected override void GenerateNode(NCreatureVisuals target, IRitsuGodotNodeSlot required)
        {
            switch (required.Path)
            {
                case "Bounds":
                {
                    var bounds = new Control
                    {
                        Size = new(240, 280),
                        Position = new(-120, -280),
                    };
                    target.AddUniqueChild(bounds, "Bounds");
                    break;
                }
                case "IntentPos":
                {
                    var bounds = target.GetNode<Control>("%Bounds");
                    var intent = new Marker2D();
                    target.AddUniqueChild(intent, "IntentPos");
                    intent.Position = bounds.Position + bounds.Size * new Vector2(0.5f, 0f) + new Vector2(0, -70);
                    break;
                }
                case "%CenterPos":
                {
                    var bounds = target.GetNode<Control>("%Bounds");
                    var center = new Marker2D();
                    target.AddUniqueChild(center, "CenterPos");
                    center.Position = bounds.Position + bounds.Size * new Vector2(0.5f, 0.6f);
                    break;
                }
                case "%PhobiaModeVisuals":
                {
                    var phobia = new Node2D { Visible = false };
                    target.AddUniqueChild(phobia, "PhobiaModeVisuals");
                    break;
                }
                case "%Visuals":
                    RitsuLibFramework.Logger.Warn(
                        "[Godot] NCreatureVisuals '%Visuals' must be supplied for non-texture sources.");
                    break;
            }
        }
    }
}
