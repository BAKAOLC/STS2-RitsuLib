using Godot;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace STS2RitsuLib.Scaffolding.Godot.NodeFactories
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Converts ordinary trail scenes into <see cref="NCardTrailVfx" /> roots. Image resources are intentionally
    ///         unsupported because card trails require a scene containing a <c>Sprites</c> node with particle children.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将普通拖尾场景转换为 <see cref="NCardTrailVfx" /> 根节点。卡牌拖尾需要包含 <c>Sprites</c>
    ///         节点及粒子子节点的场景，因此有意不支持图片资源。
    ///     </para>
    /// </summary>
    internal sealed class RitsuNCardTrailVfxNodeFactory() : RitsuGodotNodeFactory<NCardTrailVfx>([])
    {
        protected override NCardTrailVfx CreateBareFromResourceImpl(object resource)
        {
            throw new NotSupportedException(
                "RitsuNCardTrailVfxNodeFactory only supports scene conversion via RitsuGodotNodeFactories.CreateFromScene / CreateFromScenePath<NCardTrailVfx>(...).");
        }

        protected override void ConvertScene(NCardTrailVfx target, Node? source)
        {
            if (source == null)
            {
                EnsureSpritesNode(target);
                return;
            }

            target.Name = source.Name;
            if (source is CanvasItem sourceItem)
                CopyCanvasItemProperties(target, sourceItem);

            if (source.GetNodeOrNull("Sprites") != null)
            {
                foreach (var child in source.GetChildren())
                {
                    source.RemoveChild(child);
                    target.AddChild(child);
                    child.Owner = target;
                    SetChildrenOwner(target, child);
                }

                source.QueueFree();
                EnsureSpritesNode(target);
                return;
            }

            if (source is Node2D)
            {
                source.Name = "Sprites";
                target.AddChild(source);
                source.Owner = target;
                SetChildrenOwner(target, source);
                return;
            }

            var sprites = new Node2D { Name = "Sprites" };
            target.AddChild(sprites);
            sprites.Owner = target;
            sprites.AddChild(source);
            source.Owner = target;
            SetChildrenOwner(target, source);
        }

        protected override void GenerateNode(NCardTrailVfx target, IRitsuGodotNodeSlot required)
        {
        }

        private static void EnsureSpritesNode(NCardTrailVfx target)
        {
            if (target.GetNodeOrNull("Sprites") != null)
                return;

            var sprites = new Node2D { Name = "Sprites" };
            target.AddChild(sprites);
            sprites.Owner = target;
        }
    }
}
