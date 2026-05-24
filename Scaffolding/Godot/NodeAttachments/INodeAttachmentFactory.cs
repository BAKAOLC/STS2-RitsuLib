using Godot;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    /// <summary>
    ///     Attribute auto-registration factory hook for ready-time node attachments.
    /// </summary>
    public interface INodeAttachmentFactory
    {
        /// <summary>
        ///     Creates the child node for <paramref name="parent" />.
        /// </summary>
        Node CreateNode(Node parent);
    }

    /// <summary>
    ///     Attribute auto-registration setup hook for ready-time node attachments.
    /// </summary>
    public interface INodeAttachmentSetup
    {
        /// <summary>
        ///     Runs attachment setup for <paramref name="node" /> and its <paramref name="parent" />.
        /// </summary>
        void Setup(Node parent, Node node);
    }
}
