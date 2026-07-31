using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Godot.NodeFactories
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Builds <see cref="NCreatureVisuals" /> from base-game-style scenes or a <see cref="Texture2D" /> used as a
    ///         <see cref="Sprite2D" /> body. Non-Spine combat playback remains handled by
    ///         <see cref="Characters.Visuals.ModCreatureVisualPlayback" />. Named slots match <c>NCreatureVisualsFactory</c>;
    ///         as in BaseLib, missing <c>%OrbPos</c> and <c>%TalkPos</c> slots are not synthesized, so
    ///         <see cref="NCreatureVisuals" /> falls back to <c>IntentPos</c> and <see langword="null" />, respectively.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         从游戏风格的场景或作为 <see cref="Sprite2D" /> 主体的 <see cref="Texture2D" /> 构建
    ///         <see cref="NCreatureVisuals" />。非 Spine 战斗动画仍由
    ///         <see cref="Characters.Visuals.ModCreatureVisualPlayback" /> 播放。命名槽位与
    ///         <c>NCreatureVisualsFactory</c> 一致；与 BaseLib 相同，缺少的 <c>%OrbPos</c> 和 <c>%TalkPos</c>
    ///         槽位不会被自动创建，<see cref="NCreatureVisuals" /> 会分别回退到 <c>IntentPos</c> 和
    ///         <see langword="null" />。
    ///     </para>
    /// </summary>
    internal sealed class RitsuNCreatureVisualsNodeFactory() : RitsuGodotNodeFactory<NCreatureVisuals>([
        new RitsuGodotNodeSlot<Node2D>("%Visuals"),
        new RitsuGodotNodeSlot<Node2D>("%PhobiaModeVisuals"),
#if STS2_AT_LEAST_0_110_0
        new RitsuGodotNodeSlot<Control>("%FormVfx"),
#endif
        new RitsuGodotNodeSlot<Control>("Bounds"),
        new RitsuGodotNodeSlot<Marker2D>("%CenterPos"),
        new RitsuGodotNodeSlot<Marker2D>("IntentPos"),
        new RitsuGodotNodeSlot<Marker2D>("%OrbPos"),
        new RitsuGodotNodeSlot<Marker2D>("%TalkPos"),
    ])
    {
        public override Node CreateFromNode(Node source)
        {
            var created = (NCreatureVisuals)base.CreateFromNode(source);
            EnsureFormVfxHolder(created);
            return created;
        }

        public override Node CreateFromNode(Node source, VisualNodeStyle? style)
        {
            var created = (NCreatureVisuals)base.CreateFromNode(source, style);
            EnsureFormVfxHolder(created);
            return created;
        }

        internal static void EnsureFormVfxHolder(NCreatureVisuals visuals)
        {
#if STS2_AT_LEAST_0_110_0
            if (visuals.GetNodeOrNull<Control>("%FormVfx") != null)
                return;

            var namedChild = visuals.GetNodeOrNull("FormVfx");
            if (namedChild is Control existingHolder)
            {
                existingHolder.UniqueNameInOwner = true;
                existingHolder.Owner = visuals;
                return;
            }

            if (namedChild != null)
                throw new InvalidOperationException(
                    $"NCreatureVisuals child 'FormVfx' must be a {nameof(Control)}, but was {namedChild.GetType().Name}.");

            var formVfxHolder = new Control
            {
                Position = Vector2.Zero,
            };
            visuals.AddUniqueChild(formVfxHolder, "FormVfx");
            visuals.MoveChild(formVfxHolder, 0);
#endif
        }

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
#if STS2_AT_LEAST_0_110_0
                case "%FormVfx":
                    EnsureFormVfxHolder(target);
                    break;
#endif
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
