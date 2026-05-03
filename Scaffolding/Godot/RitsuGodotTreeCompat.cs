using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     Tree mutations aligned with vanilla <c>GodotTreeExtensions</c> (0.104.x). Game 0.103.2 omits
    ///     <c>MoveChildSafely</c>; referencing it from mods breaks multi-version builds. Use these helpers instead of
    ///     calling game extension methods for merchant-booth style layout.
    /// </summary>
    public static class RitsuGodotTreeCompat
    {
        /// <summary>
        ///     Same branching as <c>MegaCrit.Sts2.Core.Helpers.GodotTreeExtensions.AddChildSafely</c> on current game
        ///     branches that ship it.
        /// </summary>
        public static void AddChildSafely(Node parent, Node? child)
        {
            if (child == null || !GodotObject.IsInstanceValid(parent))
                return;

            if (NGame.IsMainThread() && (parent.IsNodeReady() || !parent.IsInsideTree()))
            {
                parent.AddChild(child);
                return;
            }

            parent.CallDeferred(Node.MethodName.AddChild, child);
        }

        /// <summary>
        ///     Same branching as <c>MegaCrit.Sts2.Core.Helpers.GodotTreeExtensions.MoveChildSafely</c> where the game
        ///     provides it (0.104+); self-contained for 0.103.2 reference assemblies.
        /// </summary>
        public static void MoveChildSafely(Node parent, Node? child, int index)
        {
            if (child == null || !GodotObject.IsInstanceValid(parent))
                return;

            if (NGame.IsMainThread() && (parent.IsNodeReady() || !parent.IsInsideTree()))
            {
                parent.MoveChild(child, index);
                return;
            }

            parent.CallDeferred(Node.MethodName.MoveChild, child, index);
        }
    }
}
