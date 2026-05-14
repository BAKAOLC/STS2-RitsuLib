using Godot;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     Godot node helpers for packed-scene conversion and procedural roots.
    ///     用于 packed scene 转换和程序化根节点的 Godot 节点辅助方法。
    /// </summary>
    public static class RitsuGodotNodeExtensions
    {
        /// <summary>
        ///     Adds <paramref name="child" /> with <see cref="Node.UniqueNameInOwner" /> so it resolves via
        ///     <c>GetNode("%Name")</c>.
        ///     添加 <paramref name="child" /> 并设置 <see cref="Node.UniqueNameInOwner" />，使其可通过
        ///     <c>GetNode("%Name")</c> 解析。
        /// </summary>
        public static void AddUniqueChild(this Node owner, Node child, string? name = null)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(child);

            if (name != null)
                child.Name = name;

            child.UniqueNameInOwner = true;
            owner.AddChild(child);
            child.Owner = owner;
        }
    }
}
