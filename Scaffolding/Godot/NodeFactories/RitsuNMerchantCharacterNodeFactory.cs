using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace STS2RitsuLib.Scaffolding.Godot.NodeFactories
{
    /// <summary>
    ///     Builds <see cref="NMerchantCharacter" /> from <see cref="Texture2D" /> or converted mod scenes.
    ///     Merchant <see cref="Characters.Visuals.ModCreatureVisualPlayback" /> handles non-Spine <c>PlayAnimation</c>.
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

        protected override void GenerateNode(NMerchantCharacter target, IRitsuGodotNodeSlot required)
        {
        }
    }
}
