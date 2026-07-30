using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace STS2RitsuLib.Scaffolding.Godot.NodeFactories
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Builds <see cref="NMerchantCharacter" /> from a <see cref="Texture2D" /> or a converted mod scene.
    ///         <see cref="Characters.Visuals.ModCreatureVisualPlayback" /> handles non-Spine merchant animations.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         从 <see cref="Texture2D" /> 或转换后的模组场景构建 <see cref="NMerchantCharacter" />。
    ///         非 Spine 商人动画由 <see cref="Characters.Visuals.ModCreatureVisualPlayback" /> 播放。
    ///     </para>
    /// </summary>
    internal sealed class RitsuNMerchantCharacterNodeFactory() : RitsuGodotNodeFactory<NMerchantCharacter>([])
    {
        protected override NMerchantCharacter CreateBareFromResourceImpl(object resource)
        {
            return resource switch
            {
                Texture2D img => FromTexture(img),
                _ => throw new NotSupportedException(
                    $"RitsuNMerchantCharacterNodeFactory does not support {resource.GetType().Name}."),
            };
        }

        private static NMerchantCharacter FromTexture(Texture2D img)
        {
            var imgSize = img.GetSize();
            var node = new NMerchantCharacter();

            var visuals = new Sprite2D();
            node.AddUniqueChild(visuals, "Visuals");
            visuals.Texture = img;
            visuals.Position = new(0, -imgSize.Y * 0.5f);

            return node;
        }

        protected override Node? ResolveDefaultStyleTarget(NMerchantCharacter root, bool fromResource)
        {
            return root.GetNodeOrNull("%Visuals") ??
                   root.GetNodeOrNull("Visuals") ?? base.ResolveDefaultStyleTarget(root, fromResource);
        }

        protected override void GenerateNode(NMerchantCharacter target, IRitsuGodotNodeSlot required)
        {
        }
    }
}
