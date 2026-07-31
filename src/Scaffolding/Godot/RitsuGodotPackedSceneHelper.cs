using Godot;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Packs a live node tree into a <see cref="PackedScene" /> for APIs that require a scene resource,
    ///         such as event layouts.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为需要场景资源的 API（例如事件布局）将现有节点树打包为 <see cref="PackedScene" />。
    ///     </para>
    /// </summary>
    public static class RitsuGodotPackedSceneHelper
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Packs <paramref name="root" /> into a new <see cref="PackedScene" />, or returns
        ///         <see langword="null" /> if packing fails.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <paramref name="root" /> 打包为新的 <see cref="PackedScene" />；打包失败时返回
        ///         <see langword="null" />。
        ///     </para>
        /// </summary>
        public static PackedScene? PackRootOrNull(Node root)
        {
            ArgumentNullException.ThrowIfNull(root);
            var packed = new PackedScene();
            if (packed.Pack(root) == Error.Ok)
                return packed;

            packed.Dispose();
            return null;
        }
    }
}
