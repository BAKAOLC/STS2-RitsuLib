using Godot;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    /// <summary>
    ///     <para xml:lang="en">Defines an attribute-auto-registration factory for nodes attached during <c>_Ready</c>.</para>
    ///     <para xml:lang="zh-CN">定义通过特性自动注册、在 <c>_Ready</c> 阶段附加节点的工厂。</para>
    /// </summary>
    public interface INodeAttachmentFactory
    {
        /// <summary>
        ///     <para xml:lang="en">Creates the child node for <paramref name="parent" />.</para>
        ///     <para xml:lang="zh-CN">为 <paramref name="parent" /> 创建子节点。</para>
        /// </summary>
        Node CreateNode(Node parent);
    }

    /// <summary>
    ///     <para xml:lang="en">Defines attribute-auto-registered setup logic for nodes attached during <c>_Ready</c>.</para>
    ///     <para xml:lang="zh-CN">定义通过特性自动注册、用于配置 <c>_Ready</c> 阶段附加节点的逻辑。</para>
    /// </summary>
    public interface INodeAttachmentSetup
    {
        /// <summary>
        ///     <para xml:lang="en">Configures <paramref name="node" /> after it is attached to <paramref name="parent" />.</para>
        ///     <para xml:lang="zh-CN">在 <paramref name="node" /> 附加到 <paramref name="parent" /> 后对其进行配置。</para>
        /// </summary>
        void Setup(Node parent, Node node);
    }
}
