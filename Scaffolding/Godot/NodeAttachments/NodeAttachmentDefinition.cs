using Godot;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    /// <summary>
    ///     Immutable audit record for a ready-time node attachment registration.
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
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     Fully qualified attachment id.
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Local id supplied by the owning mod.
        /// </summary>
        public string LocalId { get; }

        /// <summary>
        ///     Parent node type whose ready lifecycle installs this attachment.
        /// </summary>
        public Type ParentType { get; }

        /// <summary>
        ///     Expected attached child node type.
        /// </summary>
        public Type NodeType { get; }

        /// <summary>
        ///     Options captured at registration time.
        /// </summary>
        public NodeAttachmentOptions Options { get; }

        /// <summary>
        ///     Creation source label such as factory, scene, or converted-scene.
        /// </summary>
        public string SourceKind { get; }

        /// <summary>
        ///     Scene path used by scene-backed registrations, if any.
        /// </summary>
        public string? ScenePath { get; }

        /// <summary>
        ///     Stable ordering among attachments on the same parent.
        /// </summary>
        public int Order => Options.Order;

        /// <summary>
        ///     Optional direct-child name assigned to the attached node.
        /// </summary>
        public string? Name => Options.Name;

        /// <summary>
        ///     Setup delegate adapted to untyped Node parameters for diagnostics.
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
