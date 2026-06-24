using Godot;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    /// <summary>
    ///     Attribute auto-registration factory hook for ready-time node attachments.
    ///     ready 阶段节点挂载的 attribute 自动注册工厂钩子。
    /// </summary>
    public interface INodeAttachmentFactory
    {
        /// <summary>
        ///     Creates the child node for <paramref name="parent" />.
        ///     为 <paramref name="parent" /> 创建子节点。
        /// </summary>
        Node CreateNode(Node parent);
    }

    /// <summary>
    ///     Attribute auto-registration setup hook for ready-time node attachments.
    ///     ready 阶段节点挂载的 attribute 自动注册 setup 钩子。
    /// </summary>
    public interface INodeAttachmentSetup
    {
        /// <summary>
        ///     Runs attachment setup for <paramref name="node" /> and its <paramref name="parent" />.
        ///     为 <paramref name="node" /> 及其 <paramref name="parent" /> 运行挂载 setup。
        /// </summary>
        void Setup(Node parent, Node node);
    }
}
