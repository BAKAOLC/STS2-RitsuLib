using Godot;

namespace STS2RitsuLib
{
    internal sealed partial class RitsuLibModImageResourceLoader : ResourceFormatLoader
    {
        private const string ModImagePath = "res://STS2-RitsuLib/mod_image.png";
        private const string EmbeddedResourceName = "STS2RitsuLib.Assets.mod_image.png";

        private static readonly StringName Texture2DType = new("Texture2D");
        private static readonly StringName ResourceType = new("Resource");
        private static RitsuLibModImageResourceLoader? _registeredLoader;

        public static void EnsureRegistered()
        {
            if (_registeredLoader != null)
                return;

            var loader = new RitsuLibModImageResourceLoader();
            ResourceLoader.AddResourceFormatLoader(loader, true);
            _registeredLoader = loader;
        }

        public override string[] _GetRecognizedExtensions()
        {
            return ["png"];
        }

        public override bool _HandlesType(StringName type)
        {
            return type == Texture2DType || type == ResourceType;
        }

        public override string _GetResourceType(string path)
        {
            return IsModImagePath(path) ? "Texture2D" : string.Empty;
        }

        public override bool _RecognizePath(string path, StringName type)
        {
            return IsModImagePath(path);
        }

        public override bool _Exists(string path)
        {
            return IsModImagePath(path);
        }

        public override Variant _Load(string path, string originalPath, bool useSubThreads, int cacheMode)
        {
            if (!IsModImagePath(path))
                return default;

            try
            {
                using var stream = typeof(RitsuLibModImageResourceLoader)
                    .Assembly
                    .GetManifestResourceStream(EmbeddedResourceName);
                if (stream == null)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[ModImage] Embedded resource not found: {EmbeddedResourceName}");
                    return default;
                }

                using var memory = new MemoryStream();
                stream.CopyTo(memory);

                var image = new Image();
                var error = image.LoadPngFromBuffer(memory.ToArray());
                if (error == Error.Ok) return ImageTexture.CreateFromImage(image);
                RitsuLibFramework.Logger.Warn($"[ModImage] Failed to decode embedded PNG: {error}");
                return default;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModImage] Failed to load embedded mod image: {ex.Message}");
                return default;
            }
        }

        private static bool IsModImagePath(string path)
        {
            return string.Equals(path, ModImagePath, StringComparison.Ordinal);
        }
    }
}
