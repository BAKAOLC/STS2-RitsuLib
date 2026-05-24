using Godot;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    /// <summary>
    ///     Controls how a registered attachment handles an existing direct child with the configured node name.
    /// </summary>
    public enum NodeAttachmentDuplicatePolicy
    {
        /// <summary>
        ///     Create and attach the registered node even when another direct child already has the same name.
        /// </summary>
        AllowDuplicateName,

        /// <summary>
        ///     Reuse the existing direct child when its type is compatible with the registered node type.
        /// </summary>
        ReuseExistingByName,

        /// <summary>
        ///     Skip this attachment when a direct child with the configured name already exists.
        /// </summary>
        SkipIfExistingByName,

        /// <summary>
        ///     Remove the existing direct child before creating the registered node.
        /// </summary>
        ReplaceExistingByName,

        /// <summary>
        ///     Throw when a direct child with the configured name already exists.
        /// </summary>
        ThrowIfExistingByName,
    }

    /// <summary>
    ///     Controls how a created attachment node is added to its parent.
    /// </summary>
    public enum NodeAttachmentAddMode
    {
        /// <summary>
        ///     Use <see cref="RitsuGodotTreeCompat.AddChildSafely" />.
        /// </summary>
        AddChildSafely,

        /// <summary>
        ///     Call <see cref="Node.AddChild(Node, bool, Node.InternalMode)" /> immediately.
        /// </summary>
        AddChildDirect,
    }

    /// <summary>
    ///     Controls when <see cref="NodeAttachmentDefinition.Setup" /> is invoked.
    /// </summary>
    public enum NodeAttachmentSetupTiming
    {
        /// <summary>
        ///     Run setup after creation and before adding the child to the parent.
        /// </summary>
        BeforeAdd,

        /// <summary>
        ///     Run setup after the child has been added to the parent.
        /// </summary>
        AfterAdd,
    }
}
