using Godot;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     <para xml:lang="en">Provides Godot node helpers for packed-scene conversion and procedural roots.</para>
    ///     <para xml:lang="zh-CN">提供用于场景打包转换和程序化根节点的 Godot 节点方法。</para>
    /// </summary>
    public static class RitsuGodotNodeExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds <paramref name="child" />, assigns <paramref name="owner" /> as its owner, and enables
        ///         <see cref="Node.UniqueNameInOwner" /> so it can be resolved through <c>GetNode("%Name")</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         添加 <paramref name="child" />，将 <paramref name="owner" /> 指定为其所有者，并启用
        ///         <see cref="Node.UniqueNameInOwner" />，使其可通过 <c>GetNode("%Name")</c> 解析。
        ///     </para>
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
