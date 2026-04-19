using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace STS2RitsuLib.Scaffolding.Godot.NodeFactories
{
    /// <summary>
    ///     Builds <see cref="NRestSiteCharacter" /> from <see cref="Texture2D" />, <see cref="Sprite2D" />-root scenes, or
    ///     other mod Godot roots (mirrors baselib <c>NRestSiteCharacterFactory</c> for ritsulib’s explicit factory path).
    /// </summary>
    internal sealed class RitsuNRestSiteCharacterNodeFactory() : RitsuGodotNodeFactory<NRestSiteCharacter>([
        new RitsuGodotNodeSlot<Control>("ControlRoot", false),
        new RitsuGodotNodeSlot<Control>("%Hitbox"),
        new RitsuGodotNodeSlot<NSelectionReticle>("%SelectionReticle"),
        new RitsuGodotNodeSlot<Control>("%ThoughtBubbleRight"),
        new RitsuGodotNodeSlot<Control>("%ThoughtBubbleLeft"),
    ])
    {
        private const string SelectionReticleScenePath = "res://scenes/ui/selection_reticle.tscn";

        protected override NRestSiteCharacter CreateBareFromResourceImpl(object resource)
        {
            return resource switch
            {
                Texture2D img => FromTexture(img),
                _ => throw new NotSupportedException(
                    $"RitsuNRestSiteCharacterNodeFactory does not support {resource.GetType().Name} except Texture2D for CreateFromResource."),
            };
        }

        private static NRestSiteCharacter FromTexture(Texture2D img)
        {
            var imgSize = img.GetSize();
            var boundsSize = img.GetSize() * 1.05f;

            var visualsNode = new NRestSiteCharacter
            {
                Name = $"GeneratedRestSiteChar_{img.ResourcePath.GetFile()}",
            };

            var controlRoot = new Control { Name = "ControlRoot" };
            visualsNode.AddChild(controlRoot);
            controlRoot.Position = Vector2.Zero;
            controlRoot.Size = Vector2.Zero;

            var hitbox = new Control();
            controlRoot.AddUniqueChild(hitbox, "Hitbox");
            hitbox.Position = new(-boundsSize.X * 0.5f, -boundsSize.Y * 0.6f);
            hitbox.Size = boundsSize;

            var visuals = new Sprite2D { Name = "Visuals" };
            controlRoot.AddChild(visuals);
            visuals.Texture = img;
            visuals.Position = new(0, -imgSize.Y * 0.1f);

            return visualsNode;
        }

        protected override void ConvertScene(NRestSiteCharacter target, Node? source)
        {
            if (source is Sprite2D sprite)
            {
                var tex = sprite.Texture;
                if (tex != null)
                {
                    sprite.QueueFreeSafely();
                    source = FromTexture(tex);
                }
            }

            base.ConvertScene(target, source);
        }

        protected override void GenerateNode(NRestSiteCharacter target, IRitsuGodotNodeSlot required)
        {
            switch (required.Path)
            {
                case "ControlRoot":
                case "%Hitbox":
                    RitsuLibFramework.Logger.Warn(
                        $"[Godot] {required.Path} must be defined in NRestSiteCharacter scene for {target.Name}.");
                    break;
                case "%ThoughtBubbleRight":
                {
                    var hitbox = target.GetNode<Control>("%Hitbox");
                    var rightBubble = new Control
                    {
                        Size = Vector2.Zero,
                        Position = hitbox.Position + hitbox.Size * new Vector2(0.8f, 0.2f),
                    };
                    target.AddUniqueChild(rightBubble, "ThoughtBubbleRight");
                    break;
                }
                case "%ThoughtBubbleLeft":
                {
                    var hitbox = target.GetNode<Control>("%Hitbox");
                    var leftBubble = new Control
                    {
                        Size = Vector2.Zero,
                        Position = hitbox.Position + hitbox.Size * new Vector2(0.2f, 0.2f),
                    };
                    target.AddUniqueChild(leftBubble, "ThoughtBubbleLeft");
                    break;
                }
                case "%SelectionReticle":
                {
                    var hitbox = target.GetNode<Control>("%Hitbox");
                    if (!ResourceLoader.Exists(SelectionReticleScenePath))
                    {
                        RitsuLibFramework.Logger.Error(
                            $"[Godot] Missing selection reticle scene '{SelectionReticleScenePath}'; cannot build NRestSiteCharacter reticle.");
                        break;
                    }

                    var reticle =
                        PreloadManager.Cache.GetScene(SelectionReticleScenePath)
                            .Instantiate<NSelectionReticle>();
                    reticle.Name = "SelectionReticle";
                    CopyControlProperties(reticle, hitbox);
                    target.AddUniqueChild(reticle, "SelectionReticle");
                    break;
                }
            }
        }

        protected override Node ConvertNodeType(Node node, Type targetType)
        {
            if (targetType == typeof(NSelectionReticle))
            {
                if (node is not Control control || !ResourceLoader.Exists(SelectionReticleScenePath))
                    return base.ConvertNodeType(node, targetType);

                var reticle =
                    PreloadManager.Cache.GetScene(SelectionReticleScenePath)
                        .Instantiate<NSelectionReticle>();
                reticle.Name = control.Name;
                CopyControlProperties(reticle, control);
                return reticle;
            }

            if (targetType != typeof(Control) || node is not Marker2D marker)
                return base.ConvertNodeType(node, targetType);
            if (marker.Name.Equals("ThoughtBubbleLeft") || marker.Name.Equals("ThoughtBubbleRight") ||
                marker.Name.Equals("ControlRoot"))
                return new Control
                {
                    Name = marker.Name,
                    Size = Vector2.Zero,
                    Position = marker.Position,
                };

            throw new InvalidOperationException(
                $"Marker2D can only be converted to Control for 'ControlRoot', 'ThoughtBubbleLeft', and 'ThoughtBubbleRight' in NRestSiteCharacter, not for '{marker.Name}'.");
        }
    }
}
