using Godot;

namespace STS2RitsuLib.Scaffolding.Godot.NodeFactories
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Converts ordinary Godot orb and miscellaneous scenes into typed <see cref="Node2D" /> roots for
    ///         <see cref="RitsuGodotNodeFactories" />. The factory mirrors BaseLib's flexible root conversion without
    ///         requiring any named child slots.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将普通的 Godot 充能球及其他场景转换为供 <see cref="RitsuGodotNodeFactories" /> 使用的强类型
    ///         <see cref="Node2D" /> 根节点。此工厂与 BaseLib 的灵活根节点转换方式一致，不要求任何具名子节点槽位。
    ///     </para>
    /// </summary>
    internal sealed class RitsuNode2DSceneRootFactory() : RitsuGodotNodeFactory<Node2D>([])
    {
        protected override Node2D CreateBareFromResourceImpl(object resource)
        {
            throw new NotSupportedException(
                "RitsuNode2DSceneRootFactory only supports scene conversion via RitsuGodotNodeFactories.CreateFromScene / CreateFromScenePath<Node2D>(...).");
        }

        protected override void GenerateNode(Node2D target, IRitsuGodotNodeSlot required)
        {
        }
    }
}
