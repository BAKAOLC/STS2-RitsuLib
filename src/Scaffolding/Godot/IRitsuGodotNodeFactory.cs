using Godot;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     Public typed factory contract used by <see cref="RitsuGodotNodeFactories" />.
    ///     <see cref="RitsuGodotNodeFactories" /> 使用的公开强类型工厂契约。
    /// </summary>
    public interface IRitsuGodotNodeFactory<out TNode> where TNode : Node
    {
        /// <summary>
        ///     Converts an instantiated Godot scene root into <typeparamref name="TNode" />.
        ///     将已实例化的 Godot 场景根节点转换为 <typeparamref name="TNode" />。
        /// </summary>
        TNode CreateFromNode(Node source, VisualNodeStyle? style);

        /// <summary>
        ///     Creates <typeparamref name="TNode" /> from a loaded resource or resource path.
        ///     从已加载资源或资源路径创建 <typeparamref name="TNode" />。
        /// </summary>
        TNode CreateFromResource(object resource, VisualNodeStyle? style);
    }
}
