using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     Base metadata for declarative ready-time node attachments.
    ///     声明式 ready 阶段节点挂载的基础元数据。
    /// </summary>
    public abstract class RegisterNodeAttachmentAttributeBase(Type parentType, string localId)
        : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Parent node type whose ready lifecycle receives the child.
        ///     ready 生命周期会接收子节点的父节点类型。
        /// </summary>
        public Type ParentType { get; } = parentType;

        /// <summary>
        ///     Local, mod-scoped attachment id.
        ///     mod 作用域内的本地挂载 id。
        /// </summary>
        public string LocalId { get; } = localId;

        /// <summary>
        ///     Direct child name assigned to the attached node.
        ///     分配给挂载节点的直接子节点名称。
        /// </summary>
        public string? NodeName { get; set; }

        /// <summary>
        ///     Sets <c>UniqueNameInOwner</c> and assigns the parent as owner after add.
        ///     设置 <c>UniqueNameInOwner</c>，并在加入后将父节点设为 owner。
        /// </summary>
        public bool UniqueNameInOwner { get; set; }

        /// <summary>
        ///     Applies the registration to derived parent node types.
        ///     将注册应用到派生父节点类型。
        /// </summary>
        public bool IncludeDerivedParentTypes { get; set; } = true;

        /// <summary>
        ///     Existing-child duplicate policy.
        ///     已有子节点的重复处理策略。
        /// </summary>
        public NodeAttachmentDuplicatePolicy DuplicatePolicy { get; set; } =
            NodeAttachmentDuplicatePolicy.AllowDuplicateName;

        /// <summary>
        ///     Node add mode.
        ///     节点加入模式。
        /// </summary>
        public NodeAttachmentAddMode AddMode { get; set; } = NodeAttachmentAddMode.AddChildSafely;

        /// <summary>
        ///     Setup timing for <see cref="INodeAttachmentSetup" />.
        ///     <see cref="INodeAttachmentSetup" /> 的 setup 时机。
        /// </summary>
        public NodeAttachmentSetupTiming SetupTiming { get; set; } = NodeAttachmentSetupTiming.BeforeAdd;

        /// <summary>
        ///     Optional final direct-child index.
        ///     可选最终直接子节点索引。
        /// </summary>
        public int ChildIndex { get; set; } = -1;

        /// <summary>
        ///     Optional sibling name to insert before.
        ///     可选：插入到该同级节点名称之前。
        /// </summary>
        public string? InsertBeforeName { get; set; }

        /// <summary>
        ///     Optional sibling name to insert after.
        ///     可选：插入到该同级节点名称之后。
        /// </summary>
        public string? InsertAfterName { get; set; }

        /// <summary>
        ///     Queues a replaced existing node for freeing.
        ///     对被替换的已有节点调用 QueueFree。
        /// </summary>
        public bool QueueFreeReplacedNode { get; set; } = true;
    }

    /// <summary>
    ///     Declaratively registers a factory-created ready-time node attachment.
    ///     声明式注册由工厂创建的 ready 阶段节点挂载。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterNodeAttachmentAttribute(Type parentType, string localId)
        : RegisterNodeAttachmentAttributeBase(parentType, localId)
    {
        /// <summary>
        ///     Optional child node type. When omitted, the annotated type must be the child node type.
        ///     可选子节点类型。省略时，标注类型本身必须是子节点类型。
        /// </summary>
        public Type? NodeType { get; set; }
    }

    /// <summary>
    ///     Declaratively registers a ready-time node attachment instantiated directly from a scene path.
    ///     声明式注册直接从 scene 路径实例化的 ready 阶段节点挂载。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterNodeAttachmentFromSceneAttribute(Type parentType, string localId, string scenePath)
        : RegisterNodeAttachmentAttributeBase(parentType, localId)
    {
        /// <summary>
        ///     Expected child node type. When omitted, the annotated type must be the child node type.
        ///     期望的子节点类型。省略时，标注类型本身必须是子节点类型。
        /// </summary>
        public Type? NodeType { get; set; }

        /// <summary>
        ///     Godot scene path to instantiate.
        ///     要实例化的 Godot scene 路径。
        /// </summary>
        public string ScenePath { get; } = scenePath;
    }

    /// <summary>
    ///     Declaratively registers a ready-time node attachment created from a scene converted by RitsuLib node factories.
    ///     声明式注册由 RitsuLib 节点工厂转换 scene 后创建的 ready 阶段节点挂载。
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
        ///     期望的子节点类型。省略时，标注类型本身必须是子节点类型。
        /// </summary>
        public Type? NodeType { get; set; }

        /// <summary>
        ///     Godot scene path loaded and converted through RitsuLib node factories.
        ///     通过 RitsuLib 节点工厂加载并转换的 Godot scene 路径。
        /// </summary>
        public string ScenePath { get; } = scenePath;
    }
}
