using Godot;
using STS2RitsuLib.Data;

namespace STS2RitsuLib.Graphics
{
    internal static class RitsuCanvasTextureFilterService
    {
        private static IDisposable? _lifecycleSubscription;
        private static bool _initialized;

        internal static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            ApplyConfiguredMode();
            _lifecycleSubscription ??= RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt =>
            {
                ApplyMode(evt.Game.GetViewport(), RitsuLibSettingsStore.GetCanvasTextureFilterMode());
            });
        }

        internal static void ApplyMode(string? mode)
        {
            if (Engine.GetMainLoop() is not SceneTree { Root: { } root })
            {
                RitsuLibFramework.Logger.Warn(
                    "[Graphics] Cannot apply the 2D texture filter because the root viewport is unavailable.");
                return;
            }

            ApplyMode(root, mode);
        }

        internal static string NormalizeMode(string? mode)
        {
            return mode?.Trim().ToLowerInvariant() switch
            {
                "nearest" => "nearest",
                "linear" => "linear",
                "nearest_mipmap" or "nearest_with_mipmaps" => "nearest_mipmap",
                _ => "linear_mipmap",
            };
        }

        private static void ApplyConfiguredMode()
        {
            ApplyMode(RitsuLibSettingsStore.GetCanvasTextureFilterMode());
        }

        private static void ApplyMode(Viewport viewport, string? mode)
        {
            var normalized = NormalizeMode(mode);
            viewport.CanvasItemDefaultTextureFilter = normalized switch
            {
                "nearest" => Viewport.DefaultCanvasItemTextureFilter.Nearest,
                "linear" => Viewport.DefaultCanvasItemTextureFilter.Linear,
                "nearest_mipmap" => Viewport.DefaultCanvasItemTextureFilter.NearestWithMipmaps,
                _ => Viewport.DefaultCanvasItemTextureFilter.LinearWithMipmaps,
            };
            RitsuLibFramework.Logger.Info($"[Graphics] 2D texture filter applied: {normalized}.");
        }
    }
}
