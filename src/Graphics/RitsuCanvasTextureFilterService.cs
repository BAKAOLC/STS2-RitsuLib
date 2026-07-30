using Godot;
using STS2RitsuLib.Data;

namespace STS2RitsuLib.Graphics
{
    /// <summary>
    ///     <para xml:lang="en">Applies the configured default 2D canvas texture filter to the current Godot viewport.</para>
    ///     <para xml:lang="zh-CN">将配置的默认二维画布纹理过滤模式应用到当前 Godot 视口。</para>
    /// </summary>
    internal static class RitsuCanvasTextureFilterService
    {
        private static readonly Lock InitializeGate = new();
        private static IDisposable? _lifecycleSubscription;
        private static bool _initialized;
        private static bool _initializing;

        /// <summary>
        ///     <para xml:lang="en">Applies the current setting and subscribes to reapply it when the game becomes ready.</para>
        ///     <para xml:lang="zh-CN">应用当前设置，并订阅游戏就绪事件以重新应用该设置。</para>
        /// </summary>
        internal static void Initialize()
        {
            lock (InitializeGate)
            {
                if (_initialized || _initializing)
                    return;

                _initializing = true;
            }

            try
            {
                ApplyConfiguredMode();
                _lifecycleSubscription ??= RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt =>
                {
                    ApplyMode(evt.Game.GetViewport(), RitsuLibSettingsStore.GetCanvasTextureFilterMode());
                });
                lock (InitializeGate)
                {
                    _initialized = true;
                }
            }
            finally
            {
                lock (InitializeGate)
                {
                    _initializing = false;
                }
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Applies <paramref name="mode" /> to the root viewport when it is available.</para>
        ///     <para xml:lang="zh-CN">在根视口可用时将 <paramref name="mode" /> 应用于它。</para>
        /// </summary>
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

        /// <summary>
        ///     <para xml:lang="en">
        ///         Normalizes a configured filter name to <c>nearest</c>, <c>linear</c>, <c>nearest_mipmap</c>, or
        ///         <c>linear_mipmap</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">将配置的过滤名称规范化为 <c>nearest</c>、<c>linear</c>、<c>nearest_mipmap</c> 或 <c>linear_mipmap</c>。</para>
        /// </summary>
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
