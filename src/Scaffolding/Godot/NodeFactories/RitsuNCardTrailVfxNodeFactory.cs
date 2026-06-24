using Godot;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace STS2RitsuLib.Scaffolding.Godot.NodeFactories
{
    /// <summary>
    ///     Converts ordinary trail scenes into <see cref="NCardTrailVfx" /> roots. Image resources are intentionally not
    ///     supported; card trails are scene-only because vanilla expects a <c>Sprites</c> node with particle children.
    ///     将普通 trail 场景转换为 <see cref="NCardTrailVfx" /> 根节点。这里有意不支持图片资源；卡牌 trail 仅支持场景，
    ///     因为原版期望存在包含粒子子节点的 <c>Sprites</c> 节点。
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
