using Godot;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a per-mod registry for attaching child nodes when a Godot parent enters <c>_Ready</c>.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供按模组划分的注册表，用于在 Godot 父节点进入 <c>_Ready</c> 时附加子节点。
    ///     </para>
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
        ///     <para xml:lang="en">Gets the singleton registry for <paramref name="modId" />.</para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="modId" /> 对应的单例注册表。</para>
        /// </summary>
        public static ModNodeAttachmentRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            var normalizedModId = modId.Trim();

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(normalizedModId, out var existing))
                    return existing;

                var created = new ModNodeAttachmentRegistry(normalizedModId);
                Registries[normalizedModId] = created;
                return created;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a factory-created child for <typeparamref name="TParent" /> <c>_Ready</c> callbacks.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TParent" /> 的 <c>_Ready</c> 回调注册由工厂创建的子节点。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Registers a factory-created child and optional setup callback for
        ///         <typeparamref name="TParent" /> <c>_Ready</c> callbacks.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TParent" /> 的 <c>_Ready</c> 回调注册由工厂创建的子节点及可选配置回调。
        ///     </para>
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
        ///     <para xml:lang="en">Registers a child instantiated directly from a <see cref="PackedScene" /> path.</para>
        ///     <para xml:lang="zh-CN">注册直接从 <see cref="PackedScene" /> 路径实例化的子节点。</para>
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
        ///     <para xml:lang="en">
        ///         Registers a child created by converting a scene through
        ///         <see cref="RitsuGodotNodeFactories.CreateFromScenePath{TNode}(string)" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册通过 <see cref="RitsuGodotNodeFactories.CreateFromScenePath{TNode}(string)" /> 转换场景后创建的子节点。
        ///     </para>
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
        ///     <para xml:lang="en">Gets an attached node by this registry's local ID without creating it.</para>
        ///     <para xml:lang="zh-CN">按此注册表中的本地 ID 获取已附加节点，不会创建节点。</para>
        /// </summary>
        public bool TryGetAttached<TParent, TNode>(TParent parent, string localId, out TNode node)
            where TParent : Node
            where TNode : Node
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localId);
            return TryGetAttachedById(parent, GetQualifiedNodeAttachmentId(_modId, localId), out node);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets an attached node by its fully qualified attachment ID without creating it.</para>
        ///     <para xml:lang="zh-CN">按完全限定的附加项 ID 获取已附加节点，不会创建节点。</para>
        /// </summary>
        public static bool TryGetAttachedById<TParent, TNode>(TParent parent, string id, out TNode node)
            where TParent : Node
            where TNode : Node
        {
            return NodeAttachmentRuntime.TryGetAttached(parent, id, out node);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Ensures that all <c>_Ready</c>-time attachments registered for <paramref name="parent" /> are applied.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         确保为 <paramref name="parent" /> 注册的所有 <c>_Ready</c> 阶段附加项均已应用。
        ///     </para>
        /// </summary>
        public static void EnsureReadyAttachments(Node parent)
        {
            ArgumentNullException.ThrowIfNull(parent);
            NodeAttachmentRuntime.AttachReadyChildren(parent);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a snapshot of all registered node attachments for diagnostics and inspection UIs.</para>
        ///     <para xml:lang="zh-CN">获取所有已注册节点附加项的快照，供诊断和检查界面使用。</para>
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
        ///     <para xml:lang="en">Builds the stable public ID for a mod-scoped node attachment.</para>
        ///     <para xml:lang="zh-CN">构建模组作用域内节点附加项的稳定公开 ID。</para>
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

            if (parentType.ContainsGenericParameters || !typeof(Node).IsAssignableFrom(parentType))
                throw new ArgumentException(
                    $"Parent type '{parentType.FullName}' must be closed and derive from {typeof(Node).FullName}.",
                    nameof(parentType));

            if (nodeType.ContainsGenericParameters || !typeof(Node).IsAssignableFrom(nodeType))
                throw new ArgumentException(
                    $"Node type '{nodeType.FullName}' must be closed and derive from {typeof(Node).FullName}.",
                    nameof(nodeType));

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

            var actualNodeType = node.GetType();
            node.Free();
            throw new InvalidOperationException(
                $"Scene '{scenePath}' instantiated {actualNodeType.FullName}, expected {typeof(TNode).FullName}. " +
                $"Use {nameof(RegisterReadyChildFromConvertedScene)} when the scene root must be converted by RitsuLib factories.");
        }
    }
}
