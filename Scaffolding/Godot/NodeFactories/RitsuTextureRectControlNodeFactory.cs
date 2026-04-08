using Godot;

namespace STS2RitsuLib.Scaffolding.Godot.NodeFactories
{
    /// <summary>
    ///     Procedural <see cref="Control" /> root from <see cref="Texture2D" /> (full <see cref="TextureRect" />).
    /// </summary>
    internal sealed class RitsuTextureRectControlNodeFactory() : RitsuGodotNodeFactory<Control>([])
    {
        protected override Control CreateBareFromResourceImpl(object resource)
        {
            return resource switch
            {
                Texture2D img => FromTexture(img),
                _ => throw new NotSupportedException(
                    $"RitsuTextureRectControlNodeFactory does not support {resource.GetType().Name}."),
            };
        }

        private static Control FromTexture(Texture2D img)
        {
            var imgSize = img.GetSize();
            return new TextureRect
            {
                Name = StableTextureRectNodeName(img.ResourcePath),
                Size = imgSize,
                Texture = img,
                PivotOffset = imgSize / 2,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            };
        }

        private static string StableTextureRectNodeName(string? resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
                return "TextureRect";

            ReadOnlySpan<char> s = resourcePath.AsSpan();
            var slash = s.LastIndexOf('/');
            if (slash >= 0)
                s = s[(slash + 1)..];

            var dot = s.LastIndexOf('.');
            if (dot > 0)
                s = s[..dot];

            if (s.IsEmpty)
                return "TextureRect";

            Span<char> buf = stackalloc char[s.Length];
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                buf[i] = char.IsAsciiLetterOrDigit(c) || c == '_' ? c : '_';
            }

            return new string(buf);
        }

        protected override void GenerateNode(Control target, IRitsuGodotNodeSlot required)
        {
        }
    }
}
