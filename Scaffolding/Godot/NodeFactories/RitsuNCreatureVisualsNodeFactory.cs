using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace STS2RitsuLib.Scaffolding.Godot.NodeFactories
{
    /// <summary>
    ///     Builds <see cref="NCreatureVisuals" /> from vanilla-style scenes or a <see cref="Texture2D" /> (Sprite2D body).
    ///     Non-Spine combat playback remains handled by <see cref="Characters.Visuals.ModCreatureVisualPlayback" />.
    ///     Named slots match <c>NCreatureVisualsFactory</c>; missing <c>%OrbPos</c> / <c>%TalkPos</c> are not synthesized
    ///     (same as baselib): <see cref="NCreatureVisuals" /> falls back to <c>IntentPos</c> / <c>null</c>.
    ///     从原版风格场景或 <see cref="Texture2D" />（Sprite2D 主体）构建 <see cref="NCreatureVisuals" />。
    ///     非 Spine 战斗播放仍由 <see cref="Characters.Visuals.ModCreatureVisualPlayback" /> 处理。
    ///     命名槽位与 <c>NCreatureVisualsFactory</c> 匹配；缺失的 <c>%OrbPos</c> / <c>%TalkPos</c> 不会被合成
    ///     （与 baselib 相同）：<see cref="NCreatureVisuals" /> 会回退到 <c>IntentPos</c> / <c>null</c>。
    /// </summary>
    internal sealed class RitsuNCreatureVisualsNodeFactory() : RitsuGodotNodeFactory<NCreatureVisuals>([
        new RitsuGodotNodeSlot<Node2D>("%Visuals"),
        new RitsuGodotNodeSlot<Node2D>("%PhobiaModeVisuals"),
        new RitsuGodotNodeSlot<Control>("Bounds"),
        new RitsuGodotNodeSlot<Marker2D>("%CenterPos"),
        new RitsuGodotNodeSlot<Marker2D>("IntentPos"),
        new RitsuGodotNodeSlot<Marker2D>("%OrbPos"),
        new RitsuGodotNodeSlot<Marker2D>("%TalkPos"),
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

        protected override Node? ResolveDefaultStyleTarget(NCreatureVisuals root, bool fromResource)
        {
            return root.GetNodeOrNull("%Visuals") ??
                   root.GetNodeOrNull("Visuals") ?? base.ResolveDefaultStyleTarget(root, fromResource);
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
                case "%Visuals":
                    RitsuLibFramework.Logger.Warn(
                        "[Godot] NCreatureVisuals '%Visuals' must be supplied for non-texture sources.");
                    break;
            }
        }
    }
}
