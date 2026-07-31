using Godot;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    /// <summary>
    ///     <para xml:lang="en">Describes an immutable node-attachment registration applied during <c>_Ready</c>.</para>
    ///     <para xml:lang="zh-CN">描述在 <c>_Ready</c> 阶段应用的不可变节点附加注册。</para>
    /// </summary>
    public sealed class NodeAttachmentDefinition
    {
        private readonly Func<Node, Node> _factory;

        internal NodeAttachmentDefinition(
            string modId,
            string id,
            string localId,
            Type parentType,
            Type nodeType,
            Func<Node, Node> factory,
            Action<Node, Node>? setup,
            NodeAttachmentOptions options,
            string sourceKind,
            string? scenePath)
        {
            ModId = modId;
            Id = id;
            LocalId = localId;
            ParentType = parentType;
            NodeType = nodeType;
            _factory = factory;
            Setup = setup;
            Options = options;
            SourceKind = sourceKind;
            ScenePath = scenePath;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the mod ID that owns this attachment.</para>
        ///     <para xml:lang="zh-CN">获取拥有此附加项的模组 ID。</para>
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the fully qualified attachment ID.</para>
        ///     <para xml:lang="zh-CN">获取完全限定的附加项 ID。</para>
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the local ID supplied by the owning mod.</para>
        ///     <para xml:lang="zh-CN">获取所属模组提供的本地 ID。</para>
        /// </summary>
        public string LocalId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the parent node type whose <c>_Ready</c> lifecycle installs this attachment.</para>
        ///     <para xml:lang="zh-CN">获取在 <c>_Ready</c> 生命周期中安装此附加项的父节点类型。</para>
        /// </summary>
        public Type ParentType { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the expected type of the attached child node.</para>
        ///     <para xml:lang="zh-CN">获取所附加子节点的预期类型。</para>
        /// </summary>
        public Type NodeType { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the options captured at registration time.</para>
        ///     <para xml:lang="zh-CN">获取注册时记录的选项。</para>
        /// </summary>
        public NodeAttachmentOptions Options { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the creation-source label, such as <c>factory</c> or <c>scene</c>.</para>
        ///     <para xml:lang="zh-CN">获取创建来源标签，例如 <c>factory</c> 或 <c>scene</c>。</para>
        /// </summary>
        public string SourceKind { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the scene path used by a scene-backed registration, if any.</para>
        ///     <para xml:lang="zh-CN">获取基于场景的注册所使用的场景路径（如果有）。</para>
        /// </summary>
        public string? ScenePath { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the stable order among attachments on the same parent.</para>
        ///     <para xml:lang="zh-CN">获取同一父节点上各附加项的稳定顺序。</para>
        /// </summary>
        public int Order => Options.Order;

        /// <summary>
        ///     <para xml:lang="en">Gets the optional direct-child name assigned to the attached node.</para>
        ///     <para xml:lang="zh-CN">获取分配给所附加节点的可选直接子节点名称。</para>
        /// </summary>
        public string? Name => Options.Name;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the setup delegate adapted to non-generic <see cref="Node" /> parameters for diagnostics.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取为诊断而适配为非泛型 <see cref="Node" /> 参数的配置委托。
        ///     </para>
        /// </summary>
        public Action<Node, Node>? Setup { get; }

        internal bool AppliesTo(Node parent)
        {
            var parentRuntimeType = parent.GetType();
            return Options.IncludeDerivedParentTypes
                ? ParentType.IsAssignableFrom(parentRuntimeType)
                : parentRuntimeType == ParentType;
        }

        internal Node CreateNode(Node parent)
        {
            var node = _factory(parent);
            if (!GodotObject.IsInstanceValid(node))
                throw new InvalidOperationException(
                    $"Node attachment '{Id}' factory returned a null or invalid Godot node.");

            if (!NodeType.IsInstanceOfType(node))
                throw new InvalidOperationException(
                    $"Node attachment '{Id}' factory returned {node.GetType().FullName}, expected {NodeType.FullName}.");

            return node;
        }

        internal void RunSetup(Node parent, Node node)
        {
            Setup?.Invoke(parent, node);
        }
    }
}
