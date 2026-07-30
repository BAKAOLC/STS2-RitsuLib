using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Base metadata for declarative node attachments created during a parent's <c>_Ready</c> lifecycle.
    ///     </para>
    ///     <para xml:lang="zh-CN">在父节点 <c>_Ready</c> 生命周期中声明式挂载子节点的基础元数据。</para>
    /// </summary>
    public abstract class RegisterNodeAttachmentAttributeBase(Type parentType, string localId)
        : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Parent node type whose <c>_Ready</c> lifecycle receives the attachment.
        ///     </para>
        ///     <para xml:lang="zh-CN">在其 <c>_Ready</c> 生命周期中执行挂载的父节点类型。</para>
        /// </summary>
        public Type ParentType { get; } = parentType;

        /// <summary>
        ///     <para xml:lang="en">Local attachment ID within the owning mod's namespace.</para>
        ///     <para xml:lang="zh-CN">归属模组命名空间内的本地挂载 ID。</para>
        /// </summary>
        public string LocalId { get; } = localId;

        /// <summary>
        ///     <para xml:lang="en">Optional direct-child name assigned before the node is added.</para>
        ///     <para xml:lang="zh-CN">节点加入前为其指定的可选直接子节点名称。</para>
        /// </summary>
        public string? NodeName { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets <see cref="Godot.Node.UniqueNameInOwner" /> and assigns the attachment parent as
        ///         <see cref="Godot.Node.Owner" /> after the node is added.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         设置 <see cref="Godot.Node.UniqueNameInOwner" />，并在节点加入后将挂载父节点设为
        ///         <see cref="Godot.Node.Owner" />。
        ///     </para>
        /// </summary>
        public bool UniqueNameInOwner { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Whether a registration for a base parent type also applies to derived node instances.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册到父基类的挂载项是否也应用于派生节点实例。</para>
        /// </summary>
        public bool IncludeDerivedParentTypes { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Policy for an existing direct child with <see cref="NodeName" />.</para>
        ///     <para xml:lang="zh-CN">处理已有同名直接子节点的策略。</para>
        /// </summary>
        public NodeAttachmentDuplicatePolicy DuplicatePolicy { get; set; } =
            NodeAttachmentDuplicatePolicy.AllowDuplicateName;

        /// <summary>
        ///     <para xml:lang="en">Method used to add the child to its attachment parent.</para>
        ///     <para xml:lang="zh-CN">将子节点加入挂载父节点时使用的方法。</para>
        /// </summary>
        public NodeAttachmentAddMode AddMode { get; set; } = NodeAttachmentAddMode.AddChildSafely;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Whether <see cref="INodeAttachmentSetup" /> runs before or after the child is added.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <see cref="INodeAttachmentSetup" /> 在子节点加入前还是加入后运行。
        ///     </para>
        /// </summary>
        public NodeAttachmentSetupTiming SetupTiming { get; set; } = NodeAttachmentSetupTiming.BeforeAdd;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional final direct-child index. Negative values leave the index unspecified.
        ///     </para>
        ///     <para xml:lang="zh-CN">可选的最终直接子节点索引；负数表示不指定索引。</para>
        /// </summary>
        public int ChildIndex { get; set; } = -1;

        /// <summary>
        ///     <para xml:lang="en">Optional direct-child name before which the new node is inserted.</para>
        ///     <para xml:lang="zh-CN">可选：将新节点插入到该直接子节点之前。</para>
        /// </summary>
        public string? InsertBeforeName { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Optional direct-child name after which the new node is inserted.</para>
        ///     <para xml:lang="zh-CN">可选：将新节点插入到该直接子节点之后。</para>
        /// </summary>
        public string? InsertAfterName { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Whether a replaced existing child is released through <see cref="Godot.Node.QueueFree" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         是否通过 <see cref="Godot.Node.QueueFree" /> 释放被替换的已有子节点。
        ///     </para>
        /// </summary>
        public bool QueueFreeReplacedNode { get; set; } = true;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Declaratively registers a node attachment created by a factory or node constructor.
    ///     </para>
    ///     <para xml:lang="zh-CN">声明式注册由工厂或节点构造函数创建的节点挂载项。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterNodeAttachmentAttribute(Type parentType, string localId)
        : RegisterNodeAttachmentAttributeBase(parentType, localId)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional child node type. When omitted, the annotated type must be the child node type.
        ///     </para>
        ///     <para xml:lang="zh-CN">可选的子节点类型；省略时，标注类型本身必须是子节点类型。</para>
        /// </summary>
        public Type? NodeType { get; set; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Declaratively registers a node attachment instantiated directly from a Godot scene.
    ///     </para>
    ///     <para xml:lang="zh-CN">声明式注册直接从 Godot 场景实例化的节点挂载项。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterNodeAttachmentFromSceneAttribute(Type parentType, string localId, string scenePath)
        : RegisterNodeAttachmentAttributeBase(parentType, localId)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Expected child node type. When omitted, the annotated type must be the child node type.
        ///     </para>
        ///     <para xml:lang="zh-CN">预期的子节点类型；省略时，标注类型本身必须是子节点类型。</para>
        /// </summary>
        public Type? NodeType { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Godot scene resource path to instantiate.</para>
        ///     <para xml:lang="zh-CN">要实例化的 Godot 场景资源路径。</para>
        /// </summary>
        public string ScenePath { get; } = scenePath;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Declaratively registers a node attachment created from a scene converted by RitsuLib node factories.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         声明式注册由 RitsuLib 节点工厂转换场景后创建的节点挂载项。
    ///     </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterNodeAttachmentFromConvertedSceneAttribute(
        Type parentType,
        string localId,
        string scenePath)
        : RegisterNodeAttachmentAttributeBase(parentType, localId)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Expected child node type. When omitted, the annotated type must be the child node type.
        ///     </para>
        ///     <para xml:lang="zh-CN">预期的子节点类型；省略时，标注类型本身必须是子节点类型。</para>
        /// </summary>
        public Type? NodeType { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Godot scene resource path loaded and converted through RitsuLib node factories.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过 RitsuLib 节点工厂加载并转换的 Godot 场景资源路径。
        ///     </para>
        /// </summary>
        public string ScenePath { get; } = scenePath;
    }
}
