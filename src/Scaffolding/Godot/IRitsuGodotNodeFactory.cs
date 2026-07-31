using Godot;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     <para xml:lang="en">Defines the public typed factory contract used by <see cref="RitsuGodotNodeFactories" />.</para>
    ///     <para xml:lang="zh-CN">定义 <see cref="RitsuGodotNodeFactories" /> 使用的公开强类型工厂契约。</para>
    /// </summary>
    public interface IRitsuGodotNodeFactory<out TNode> where TNode : Node
    {
        /// <summary>
        ///     <para xml:lang="en">Converts an instantiated Godot scene root into <typeparamref name="TNode" />.</para>
        ///     <para xml:lang="zh-CN">将已实例化的 Godot 场景根节点转换为 <typeparamref name="TNode" />。</para>
        /// </summary>
        TNode CreateFromNode(Node source, VisualNodeStyle? style);

        /// <summary>
        ///     <para xml:lang="en">Creates <typeparamref name="TNode" /> from a loaded resource or resource path.</para>
        ///     <para xml:lang="zh-CN">从已加载的资源或资源路径创建 <typeparamref name="TNode" />。</para>
        /// </summary>
        TNode CreateFromResource(object resource, VisualNodeStyle? style);
    }
}
