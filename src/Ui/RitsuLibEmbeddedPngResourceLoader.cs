using Godot;

namespace STS2RitsuLib
{
    internal sealed partial class RitsuLibEmbeddedPngResourceLoader : ResourceFormatLoader
    {
        private static readonly Lock RegistrationLock = new();
        private static readonly StringName Texture2DType = new("Texture2D");
        private static readonly StringName ResourceType = new("Resource");
        private static RitsuLibEmbeddedPngResourceLoader? _registeredLoader;

        internal static void EnsureRegistered()
        {
            lock (RegistrationLock)
            {
                if (_registeredLoader != null)
                    return;

                var loader = new RitsuLibEmbeddedPngResourceLoader();
                ResourceLoader.AddResourceFormatLoader(loader, true);
                _registeredLoader = loader;
            }
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
            return RitsuLibEmbeddedPngAssets.Contains(path) ? "Texture2D" : string.Empty;
        }

        public override bool _RecognizePath(string path, StringName type)
        {
            return RitsuLibEmbeddedPngAssets.Contains(path);
        }

        public override bool _Exists(string path)
        {
            return RitsuLibEmbeddedPngAssets.Contains(path);
        }

        public override Variant _Load(string path, string originalPath, bool useSubThreads, int cacheMode)
        {
            if (!RitsuLibEmbeddedPngAssets.TryGet(path, out var asset))
                return default;

            var resourceName = asset.ResolveEmbeddedResourceName();
            try
            {
                using var stream = typeof(RitsuLibEmbeddedPngResourceLoader)
                    .Assembly
                    .GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    RitsuLibFramework.Logger.Warn($"[EmbeddedPng] Resource not found: {resourceName}");
                    return default;
                }

                using var memory = new MemoryStream();
                stream.CopyTo(memory);

                var image = new Image();
                var error = image.LoadPngFromBuffer(memory.ToArray());
                if (error == Error.Ok)
                    return ImageTexture.CreateFromImage(image);

                RitsuLibFramework.Logger.Warn($"[EmbeddedPng] Failed to decode '{resourceName}': {error}");
                return default;
            }
            catch (Exception exception) when (RitsuLibExceptionPolicy.IsRecoverable(exception))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[EmbeddedPng] Failed to load '{resourceName}': {exception.Message}");
                return default;
            }
        }
    }
}
