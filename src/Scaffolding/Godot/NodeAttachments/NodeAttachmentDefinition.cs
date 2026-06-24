using Godot;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    /// <summary>
    ///     Immutable audit record for a ready-time node attachment registration.
    ///     ready 阶段节点挂载注册的不可变审计记录。
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
        ///     Mod id that owns this attachment.
        ///     拥有该挂载项的 mod id。
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     Fully qualified attachment id.
        ///     完整限定的挂载 id。
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Local id supplied by the owning mod.
        ///     拥有方 mod 提供的本地 id。
        /// </summary>
        public string LocalId { get; }

        /// <summary>
        ///     Parent node type whose ready lifecycle installs this attachment.
        ///     ready 生命周期会安装该挂载项的父节点类型。
        /// </summary>
        public Type ParentType { get; }

        /// <summary>
        ///     Expected attached child node type.
        ///     期望的被挂载子节点类型。
        /// </summary>
        public Type NodeType { get; }

        /// <summary>
        ///     Options captured at registration time.
        ///     注册时捕获的选项。
        /// </summary>
        public NodeAttachmentOptions Options { get; }

        /// <summary>
        ///     Creation source label such as factory, scene, or converted-scene.
        ///     创建来源标签，例如 factory、scene 或 converted-scene。
        /// </summary>
        public string SourceKind { get; }

        /// <summary>
        ///     Scene path used by scene-backed registrations, if any.
        ///     scene-backed 注册使用的 scene 路径（如果有）。
        /// </summary>
        public string? ScenePath { get; }

        /// <summary>
        ///     Stable ordering among attachments on the same parent.
        ///     同一父节点上各挂载项的稳定顺序。
        /// </summary>
        public int Order => Options.Order;

        /// <summary>
        ///     Optional direct-child name assigned to the attached node.
        ///     分配给挂载节点的可选直接子节点名称。
        /// </summary>
        public string? Name => Options.Name;

        /// <summary>
        ///     Setup delegate adapted to untyped Node parameters for diagnostics.
        ///     适配为非泛型 Node 参数、用于诊断的 setup 委托。
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
            if (node == null)
                throw new InvalidOperationException($"Node attachment '{Id}' factory returned null.");

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
