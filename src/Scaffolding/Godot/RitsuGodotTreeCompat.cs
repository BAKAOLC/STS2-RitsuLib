using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides tree mutations matching the base game's <c>GodotTreeExtensions</c> behavior on versions that expose
    ///         those helpers. Game version 0.103.2 lacks <c>MoveChildSafely</c>, so this compatibility API allows the same
    ///         layout code to compile against every supported version.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供与已公开对应方法的游戏版本中 <c>GodotTreeExtensions</c> 行为一致的节点树操作。
    ///         游戏 0.103.2 没有 <c>MoveChildSafely</c>，因此此兼容 API 可让同一套布局代码针对所有受支持版本编译。
    ///     </para>
    /// </summary>
    public static class RitsuGodotTreeCompat
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a child immediately or defers the call using the same conditions as the base game's
        ///         <c>MegaCrit.Sts2.Core.Helpers.GodotTreeExtensions.AddChildSafely</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按照游戏 <c>MegaCrit.Sts2.Core.Helpers.GodotTreeExtensions.AddChildSafely</c> 的相同条件，
        ///         立即添加子节点或延迟调用。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Moves a child immediately or defers the call using the same conditions as the base game's
        ///         <c>MegaCrit.Sts2.Core.Helpers.GodotTreeExtensions.MoveChildSafely</c>, while remaining compatible with
        ///         the 0.103.2 reference assemblies.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按照游戏 <c>MegaCrit.Sts2.Core.Helpers.GodotTreeExtensions.MoveChildSafely</c> 的相同条件，
        ///         立即移动子节点或延迟调用，同时保持与 0.103.2 引用程序集兼容。
        ///     </para>
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
