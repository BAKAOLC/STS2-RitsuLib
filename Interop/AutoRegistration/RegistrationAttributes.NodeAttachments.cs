using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     Base metadata for declarative ready-time node attachments.
    /// </summary>
    public abstract class RegisterNodeAttachmentAttributeBase(Type parentType, string localId)
        : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Parent node type whose ready lifecycle receives the child.
        /// </summary>
        public Type ParentType { get; } = parentType;

        /// <summary>
        ///     Local, mod-scoped attachment id.
        /// </summary>
        public string LocalId { get; } = localId;

        /// <summary>
        ///     Direct child name assigned to the attached node.
        /// </summary>
        public string? NodeName { get; set; }

        /// <summary>
        ///     Sets <c>UniqueNameInOwner</c> and assigns the parent as owner after add.
        /// </summary>
        public bool UniqueNameInOwner { get; set; }

        /// <summary>
        ///     Applies the registration to derived parent node types.
        /// </summary>
        public bool IncludeDerivedParentTypes { get; set; } = true;

        /// <summary>
        ///     Existing-child duplicate policy.
        /// </summary>
        public NodeAttachmentDuplicatePolicy DuplicatePolicy { get; set; } =
            NodeAttachmentDuplicatePolicy.AllowDuplicateName;

        /// <summary>
        ///     Node add mode.
        /// </summary>
        public NodeAttachmentAddMode AddMode { get; set; } = NodeAttachmentAddMode.AddChildSafely;

        /// <summary>
        ///     Setup timing for <see cref="INodeAttachmentSetup" />.
        /// </summary>
        public NodeAttachmentSetupTiming SetupTiming { get; set; } = NodeAttachmentSetupTiming.BeforeAdd;

        /// <summary>
        ///     Optional final direct-child index.
        /// </summary>
        public int ChildIndex { get; set; } = -1;

        /// <summary>
        ///     Optional sibling name to insert before.
        /// </summary>
        public string? InsertBeforeName { get; set; }

        /// <summary>
        ///     Optional sibling name to insert after.
        /// </summary>
        public string? InsertAfterName { get; set; }

        /// <summary>
        ///     Queues a replaced existing node for freeing.
        /// </summary>
        public bool QueueFreeReplacedNode { get; set; } = true;
    }

    /// <summary>
    ///     Declaratively registers a factory-created ready-time node attachment.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterNodeAttachmentAttribute(Type parentType, string localId)
        : RegisterNodeAttachmentAttributeBase(parentType, localId)
    {
        /// <summary>
        ///     Optional child node type. When omitted, the annotated type must be the child node type.
        /// </summary>
        public Type? NodeType { get; set; }
    }

    /// <summary>
    ///     Declaratively registers a ready-time node attachment instantiated directly from a scene path.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterNodeAttachmentFromSceneAttribute(Type parentType, string localId, string scenePath)
        : RegisterNodeAttachmentAttributeBase(parentType, localId)
    {
        /// <summary>
        ///     Expected child node type. When omitted, the annotated type must be the child node type.
        /// </summary>
        public Type? NodeType { get; set; }

        /// <summary>
        ///     Godot scene path to instantiate.
        /// </summary>
        public string ScenePath { get; } = scenePath;
    }

    /// <summary>
    ///     Declaratively registers a ready-time node attachment created from a scene converted by RitsuLib node factories.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterNodeAttachmentFromConvertedSceneAttribute(
        Type parentType,
        string localId,
        string scenePath)
        : RegisterNodeAttachmentAttributeBase(parentType, localId)
    {
        /// <summary>
        ///     Expected child node type. When omitted, the annotated type must be the child node type.
        /// </summary>
        public Type? NodeType { get; set; }

        /// <summary>
        ///     Godot scene path loaded and converted through RitsuLib node factories.
        /// </summary>
        public string ScenePath { get; } = scenePath;
    }
}
