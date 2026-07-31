using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Rooms;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Creates <see cref="BackgroundAssets" /> from explicit paths without invoking the base constructor that
    ///         scans a fixed <c>res://scenes/backgrounds/&lt;id&gt;/layers</c> directory.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使用显式路径创建 <see cref="BackgroundAssets" />，而不调用会扫描固定
    ///         <c>res://scenes/backgrounds/&lt;id&gt;/layers</c> 目录的游戏本体构造函数。
    ///     </para>
    /// </summary>
    public static class CombatBackgroundAssetsFactory
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates combat background assets from a main scene path, an ordered list of parallax background
        ///         layers, and an optional foreground layer.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用主场景路径、有序的视差背景图层列表和可选前景图层创建战斗背景资源。
        ///     </para>
        /// </summary>
        public static BackgroundAssets Create(string backgroundScenePath, IReadOnlyList<string> bgLayers,
            string? fgLayer = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(backgroundScenePath);
            ArgumentNullException.ThrowIfNull(bgLayers);

            var layers = bgLayers as List<string> ?? [.. bgLayers];
            return Construct(backgroundScenePath, layers, fgLayer);
        }

        internal static BackgroundAssets Construct(string backgroundScenePath, List<string> bgLayers,
            string? fgLayer)
        {
            var instance = (BackgroundAssets)RuntimeHelpers.GetUninitializedObject(typeof(BackgroundAssets));
            SetReadOnlyAutoProperty(instance, nameof(BackgroundAssets.BackgroundScenePath), backgroundScenePath);
            SetReadOnlyAutoProperty(instance, nameof(BackgroundAssets.BgLayers), bgLayers);
            SetReadOnlyAutoProperty(instance, nameof(BackgroundAssets.FgLayer), fgLayer);
            return instance;
        }

        private static void SetReadOnlyAutoProperty<T>(BackgroundAssets target, string propertyName, T value)
        {
            var field = typeof(BackgroundAssets).GetField(
                            $"<{propertyName}>k__BackingField",
                            BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? throw new MissingFieldException(typeof(BackgroundAssets).FullName,
                            $"<{propertyName}>k__BackingField");

            field.SetValue(target, value);
        }
    }
}
