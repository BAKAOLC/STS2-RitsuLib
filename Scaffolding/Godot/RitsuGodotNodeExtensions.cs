using Godot;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     Godot node helpers for packed-scene conversion and procedural roots.
    /// </summary>
    public static class RitsuGodotNodeExtensions
    {
        /// <summary>
        ///     Adds <paramref name="child" /> with <see cref="Node.UniqueNameInOwner" /> so it resolves via
        ///     <c>GetNode("%Name")</c>.
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
