using Godot;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    /// <summary>
    ///     Per-mod registration surface for attaching child nodes when a Godot parent becomes ready.
    ///     当 Godot 父节点进入 ready 时挂载子节点的逐 mod 注册入口。
    /// </summary>
    public sealed class ModNodeAttachmentRegistry
    {
        private const string IdTypeStem = "NODEATTACHMENT";
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModNodeAttachmentRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, NodeAttachmentDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly string _modId;

        private ModNodeAttachmentRegistry(string modId)
        {
            _modId = modId;
        }

        /// <summary>
        ///     Returns the singleton registry for <paramref name="modId" />.
        ///     返回 <paramref name="modId" /> 对应的单例注册表。
        /// </summary>
        public static ModNodeAttachmentRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var existing))
                    return existing;

                var created = new ModNodeAttachmentRegistry(modId);
                Registries[modId] = created;
                return created;
            }
        }

        /// <summary>
        ///     Registers a factory-created child for <typeparamref name="TParent" /> ready events.
        ///     为 <typeparamref name="TParent" /> 的 ready 事件注册由工厂创建的子节点。
        /// </summary>
        public NodeAttachmentDefinition RegisterReadyChild<TParent, TNode>(
            string localId,
            Func<TParent, TNode> factory,
            NodeAttachmentOptions? options = null)
            where TParent : Node
            where TNode : Node
        {
            return RegisterReadyChild(localId, factory, null, options);
        }

        /// <summary>
        ///     Registers a factory-created child with setup for <typeparamref name="TParent" /> ready events.
        ///     为 <typeparamref name="TParent" /> 的 ready 事件注册带 setup 的工厂创建子节点。
        /// </summary>
        public NodeAttachmentDefinition RegisterReadyChild<TParent, TNode>(
            string localId,
            Func<TParent, TNode> factory,
            Action<TParent, TNode>? setup,
            NodeAttachmentOptions? options = null)
            where TParent : Node
            where TNode : Node
        {
            ArgumentNullException.ThrowIfNull(factory);
            return RegisterCore(
                localId,
                factory,
                setup,
                options,
                "factory",
                null);
        }

        /// <summary>
        ///     Registers a child instantiated directly from a <see cref="PackedScene" /> path.
        ///     注册直接从 <see cref="PackedScene" /> 路径实例化的子节点。
        /// </summary>
        public NodeAttachmentDefinition RegisterReadyChildFromScene<TParent, TNode>(
            string localId,
            string scenePath,
            Action<TParent, TNode>? setup = null,
            NodeAttachmentOptions? options = null)
            where TParent : Node
            where TNode : Node
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
            return RegisterCore<TParent, TNode>(
                localId,
                _ => InstantiateScene<TNode>(scenePath),
                setup,
                options,
                "scene",
                scenePath);
        }

        /// <summary>
        ///     Registers a child created from a scene converted by
        ///     <see cref="RitsuGodotNodeFactories.CreateFromScenePath{TNode}(string)" />.
        ///     注册由 <see cref="RitsuGodotNodeFactories.CreateFromScenePath{TNode}(string)" />
        ///     转换 scene 后创建的子节点。
        /// </summary>
        public NodeAttachmentDefinition RegisterReadyChildFromConvertedScene<TParent, TNode>(
            string localId,
            string scenePath,
            Action<TParent, TNode>? setup = null,
            NodeAttachmentOptions? options = null)
            where TParent : Node
            where TNode : Node, new()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
            return RegisterCore<TParent, TNode>(
                localId,
                _ => RitsuGodotNodeFactories.CreateFromScenePath<TNode>(scenePath),
                setup,
                options,
                "converted-scene",
                scenePath);
        }

        /// <summary>
        ///     Reads an attached node by this registry's local id without creating it.
        ///     通过该注册表的本地 id 读取已挂载节点，不会创建节点。
        /// </summary>
        public bool TryGetAttached<TParent, TNode>(TParent parent, string localId, out TNode node)
            where TParent : Node
            where TNode : Node
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localId);
            return TryGetAttachedById(parent, GetQualifiedNodeAttachmentId(_modId, localId), out node);
        }

        /// <summary>
        ///     Reads an attached node by fully qualified attachment id without creating it.
        ///     通过完整限定挂载 id 读取已挂载节点，不会创建节点。
        /// </summary>
        public static bool TryGetAttachedById<TParent, TNode>(TParent parent, string id, out TNode node)
            where TParent : Node
            where TNode : Node
        {
            return NodeAttachmentRuntime.TryGetAttached(parent, id, out node);
        }

        /// <summary>
        ///     Ensures all ready-time attachments registered for <paramref name="parent" /> have been applied.
        ///     确保已应用为 <paramref name="parent" /> 注册的所有 ready 阶段挂载项。
        /// </summary>
        public static void EnsureReadyAttachments(Node parent)
        {
            ArgumentNullException.ThrowIfNull(parent);
            NodeAttachmentRuntime.AttachReadyChildren(parent);
        }

        /// <summary>
        ///     Returns every registered node attachment for diagnostics and audit UIs.
        ///     返回所有已注册节点挂载项，供诊断和审计 UI 使用。
        /// </summary>
        public static NodeAttachmentDefinition[] GetDefinitionsSnapshot()
        {
            lock (SyncRoot)
            {
                return
                [
                    .. Definitions.Values
                        .OrderBy(def => def.ParentType.FullName, StringComparer.Ordinal)
                        .ThenBy(def => def.Order)
                        .ThenBy(def => def.Id, StringComparer.Ordinal),
                ];
            }
        }

        /// <summary>
        ///     Builds the stable public id for a mod-scoped node attachment.
        ///     构建 mod 作用域节点挂载项的稳定公开 id。
        /// </summary>
        public static string GetQualifiedNodeAttachmentId(string modId, string localId)
        {
            return ModContentRegistry.GetCompoundId(modId, IdTypeStem, localId);
        }

        internal static NodeAttachmentDefinition[] GetDefinitionsForParent(Node parent)
        {
            lock (SyncRoot)
            {
                return
                [
                    .. Definitions.Values
                        .Where(definition => definition.AppliesTo(parent))
                        .OrderBy(definition => definition.Order)
                        .ThenBy(definition => definition.Id, StringComparer.Ordinal),
                ];
            }
        }

        internal NodeAttachmentDefinition RegisterReadyChildUntyped(
            string localId,
            Type parentType,
            Type nodeType,
            Func<Node, Node> factory,
            Action<Node, Node>? setup,
            NodeAttachmentOptions? options,
            string sourceKind,
            string? scenePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localId);
            ArgumentNullException.ThrowIfNull(parentType);
            ArgumentNullException.ThrowIfNull(nodeType);
            ArgumentNullException.ThrowIfNull(factory);

            if (!typeof(Node).IsAssignableFrom(parentType))
                throw new ArgumentException(
                    $"Parent type '{parentType.FullName}' must derive from {typeof(Node).FullName}.",
                    nameof(parentType));

            if (!typeof(Node).IsAssignableFrom(nodeType))
                throw new ArgumentException(
                    $"Node type '{nodeType.FullName}' must derive from {typeof(Node).FullName}.", nameof(nodeType));

            var normalizedLocalId = localId.Trim();
            var id = GetQualifiedNodeAttachmentId(_modId, normalizedLocalId);
            var attachmentOptions = options ?? NodeAttachmentOptions.Default;
            attachmentOptions.Validate(id);

            var definition = new NodeAttachmentDefinition(
                _modId,
                id,
                normalizedLocalId,
                parentType,
                nodeType,
                factory,
                setup,
                attachmentOptions,
                sourceKind,
                scenePath);

            NodeAttachmentPatchInstaller.EnsureReadyPatched(parentType);

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(id, out var existing))
                {
                    if (!StringComparer.OrdinalIgnoreCase.Equals(existing.ModId, _modId))
                        throw new InvalidOperationException(
                            $"Node attachment '{id}' is already registered by mod '{existing.ModId}'.");

                    if (existing.ParentType != definition.ParentType || existing.NodeType != definition.NodeType)
                        throw new InvalidOperationException(
                            $"Node attachment '{id}' is already registered for {existing.ParentType.FullName} -> {existing.NodeType.FullName}.");

                    return existing;
                }

                Definitions[id] = definition;
            }

            RitsuLibFramework.Logger.Info(
                $"[NodeAttachment] Registered {id}: {parentType.FullName} -> {nodeType.FullName} (Order={definition.Order}, Source={sourceKind})");
            return definition;
        }

        private NodeAttachmentDefinition RegisterCore<TParent, TNode>(
            string localId,
            Func<TParent, TNode> factory,
            Action<TParent, TNode>? setup,
            NodeAttachmentOptions? options,
            string sourceKind,
            string? scenePath)
            where TParent : Node
            where TNode : Node
        {
            ArgumentNullException.ThrowIfNull(factory);
            return RegisterReadyChildUntyped(
                localId,
                typeof(TParent),
                typeof(TNode),
                parent => factory((TParent)parent),
                setup == null ? null : (parent, node) => setup((TParent)parent, (TNode)node),
                options,
                sourceKind,
                scenePath);
        }

        private static TNode InstantiateScene<TNode>(string scenePath) where TNode : Node
        {
            var scene = ResourceLoader.Load<PackedScene>(scenePath)
                        ?? throw new InvalidOperationException($"Failed to load PackedScene: {scenePath}");
            var node = scene.Instantiate()
                       ?? throw new InvalidOperationException($"PackedScene.Instantiate returned null: {scenePath}");

            if (node is TNode typed)
                return typed;

            throw new InvalidOperationException(
                $"Scene '{scenePath}' instantiated {node.GetType().FullName}, expected {typeof(TNode).FullName}. " +
                $"Use {nameof(RegisterReadyChildFromConvertedScene)} when the scene root must be converted by RitsuLib factories.");
        }
    }
}
