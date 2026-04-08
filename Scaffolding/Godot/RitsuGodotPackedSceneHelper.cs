using Godot;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     Packs a live node tree into a <see cref="PackedScene" /> when an API requires a scene resource (for example event
    ///     layout).
    /// </summary>
    public static class RitsuGodotPackedSceneHelper
    {
        /// <summary>
        ///     Packs <paramref name="root" /> into a new <see cref="PackedScene" />, or returns <c>null</c> if packing fails.
        /// </summary>
        public static PackedScene? PackRootOrNull(Node root)
        {
            ArgumentNullException.ThrowIfNull(root);
            var packed = new PackedScene();
            return packed.Pack(root) == Error.Ok ? packed : null;
        }
    }
}
