using System.Diagnostics.CodeAnalysis;

namespace STS2RitsuLib
{
    internal static class RitsuLibEmbeddedPngAssets
    {
        private static readonly Dictionary<string, RitsuLibEmbeddedPngAsset> AssetsByPath =
            new(StringComparer.Ordinal);

        internal static readonly RitsuLibEmbeddedPngAsset CardArtPlaceholder = Add(
            "res://STS2-RitsuLib/card_art_placeholder.png",
            static () => "STS2RitsuLib.Assets.card_art_placeholder.png");

        internal static readonly RitsuLibEmbeddedPngAsset ModImage = Add(
            "res://STS2-RitsuLib/mod_image.png",
            static () => RitsuLibEasterEggPolicy.IsJuneTwentySeventhInBeijing()
                ? "STS2RitsuLib.Assets.mod_image_ex.png"
                : "STS2RitsuLib.Assets.mod_image.png");

        internal static bool Contains(string resourcePath)
        {
            return AssetsByPath.ContainsKey(resourcePath);
        }

        internal static bool TryGet(
            string resourcePath,
            [NotNullWhen(true)] out RitsuLibEmbeddedPngAsset? asset)
        {
            return AssetsByPath.TryGetValue(resourcePath, out asset);
        }

        private static RitsuLibEmbeddedPngAsset Add(
            string resourcePath,
            Func<string> embeddedResourceNameResolver)
        {
            var asset = new RitsuLibEmbeddedPngAsset(resourcePath, embeddedResourceNameResolver);
            AssetsByPath.Add(resourcePath, asset);
            return asset;
        }
    }

    internal sealed record RitsuLibEmbeddedPngAsset(
        string ResourcePath,
        Func<string> EmbeddedResourceNameResolver)
    {
        internal string ResolveEmbeddedResourceName()
        {
            return EmbeddedResourceNameResolver();
        }
    }
}
