using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     <para xml:lang="en">Defines the non-generic factory contract used by <see cref="RitsuGodotNodeFactoryRegistry" />.</para>
    ///     <para xml:lang="zh-CN">定义 <see cref="RitsuGodotNodeFactoryRegistry" /> 使用的非泛型工厂契约。</para>
    /// </summary>
    internal abstract class RitsuGodotNodeFactory
    {
        public abstract Node CreateFromNode(Node source);

        public virtual Node CreateFromNode(Node source, VisualNodeStyle? style)
        {
            var node = CreateFromNode(source);
            style.ApplyTo(node);
            return node;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a root node without running <see cref="CompleteBareRoot(Node)" />, for example when creating
        ///         visuals from a <see cref="Texture2D" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         构建根节点，但不运行 <see cref="CompleteBareRoot(Node)" />；例如，从 <see cref="Texture2D" />
        ///         创建视觉节点时会使用此方法。
        ///     </para>
        /// </summary>
        public abstract Node CreateBareFromResource(object resource);

        public virtual Node CreateFromResource(object resource, VisualNodeStyle? style)
        {
            var bare = CreateBareFromResource(resource);
            try
            {
                CompleteBareRoot(bare, style);
                return bare;
            }
            catch
            {
                if (GodotObject.IsInstanceValid(bare))
                    bare.Free();
                throw;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Fills the required named slots and children of a bare root, equivalent to
        ///         <c>ConvertScene(target, null)</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         填充裸根节点所需的具名槽位和子节点，等同于调用 <c>ConvertScene(target, null)</c>。
        ///     </para>
        /// </summary>
        public abstract void CompleteBareRoot(Node bare);

        public virtual void CompleteBareRoot(Node bare, VisualNodeStyle? style)
        {
            CompleteBareRoot(bare);
            style.ApplyTo(bare);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes a named child expected beneath a converted Godot scene root, resolved by a unique
    ///         <c>%Name</c> or a node path.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述转换后的 Godot 场景根节点下所需的具名子节点，可通过唯一名称 <c>%Name</c> 或节点路径解析。
    ///     </para>
    /// </summary>
    internal interface IRitsuGodotNodeSlot
    {
        string Path { get; }
        bool UniqueName { get; }
        bool MakeNameUnique { get; }
        Type ExpectedNodeType { get; }
        bool IsValidName(Node node);
        bool IsValidType(Node node);
        bool IsValidUnique(Node node);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Stores named-slot metadata for <see cref="RitsuGodotNodeFactory{T}" />, corresponding to BaseLib's
    ///         <c>NodeInfo&lt;T&gt;</c>.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         存储 <see cref="RitsuGodotNodeFactory{T}" /> 的具名槽位元数据，对应 BaseLib 的
    ///         <c>NodeInfo&lt;T&gt;</c>。
    ///     </para>
    /// </summary>
    internal sealed record RitsuGodotNodeSlot<TExpected>(string Path, bool MakeNameUnique = true) : IRitsuGodotNodeSlot
        where TExpected : Node
    {
        public StringName StringName { get; } = new(Path.StartsWith('%') ? Path[1..] : Path);
        public bool UniqueName { get; } = Path.StartsWith('%');

        public bool IsValidType(Node node)
        {
            return node is TExpected;
        }

        public bool IsValidName(Node node)
        {
            return node.Name.Equals(StringName);
        }

        public bool IsValidUnique(Node node)
        {
            return UniqueName && node is TExpected && node.Name.Equals(StringName);
        }

        public Type ExpectedNodeType => typeof(TExpected);
    }

    /// <summary>
    ///     <para xml:lang="en">Provides a base class for typed procedural-node and scene-conversion factories.</para>
    ///     <para xml:lang="zh-CN">提供强类型程序化节点及场景转换工厂的基类。</para>
    /// </summary>
    internal abstract class RitsuGodotNodeFactory<T> : RitsuGodotNodeFactory where T : Node, new()
    {
        protected readonly bool FlexibleStructure;
        protected readonly List<IRitsuGodotNodeSlot> NamedNodes;

        protected RitsuGodotNodeFactory(IEnumerable<IRitsuGodotNodeSlot> namedNodes)
        {
            NamedNodes = [.. namedNodes];
            FlexibleStructure = NamedNodes.Count == 0 || NamedNodes.All(static s => s.UniqueName);
            RitsuGodotNodeFactoryRegistry.RegisterFactory<T>(this);
        }

        public override Node CreateFromNode(Node source)
        {
            if (source is T typed)
            {
                CompleteBareRoot(typed);
                return typed;
            }

            var target = new T();
            try
            {
                ConvertScene(target, source);
                return target;
            }
            catch
            {
                FreeFailedConversion(target, source);
                throw;
            }
        }

        public override Node CreateFromNode(Node source, VisualNodeStyle? style)
        {
            if (source is T typed)
            {
                CompleteBareRoot(typed);
                ApplyStyle(typed, false, style);
                return typed;
            }

            var target = new T();
            try
            {
                ConvertScene(target, source);
                ApplyStyle(target, false, style);
                return target;
            }
            catch
            {
                FreeFailedConversion(target, source);
                throw;
            }
        }

        public override Node CreateBareFromResource(object resource)
        {
            return CreateBareFromResourceImpl(resource);
        }

        public override void CompleteBareRoot(Node bare)
        {
            ConvertScene((T)bare, null);
        }

        public override void CompleteBareRoot(Node bare, VisualNodeStyle? style)
        {
            CompleteBareRoot(bare);
            ApplyStyle((T)bare, true, style);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a bare root from <paramref name="resource" />. Implementations should throw a descriptive exception
        ///         for unsupported resource types.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从 <paramref name="resource" /> 创建裸根节点。实现应在资源类型不受支持时抛出说明明确的异常。
        ///     </para>
        /// </summary>
        protected abstract T CreateBareFromResourceImpl(object resource);

        protected virtual Node? ResolveDefaultStyleTarget(T root, bool fromResource)
        {
            return root;
        }

        private void ApplyStyle(T root, bool fromResource, VisualNodeStyle? style)
        {
            style?.ApplyTo(ResolveDefaultStyleTarget(root, fromResource) ?? root);
        }

        protected virtual void ConvertScene(T target, Node? source)
        {
            if (source != null)
            {
                target.Name = source.Name;
                switch (target)
                {
                    case Control targetControl when source is Control sourceControl:
                        CopyControlProperties(targetControl, sourceControl);
                        break;
                    case CanvasItem targetItem when source is CanvasItem sourceItem:
                        CopyCanvasItemProperties(targetItem, sourceItem);
                        break;
                }
            }

            TransferAndCreateNodes(target, source);
        }

        protected virtual void TransferAndCreateNodes(T target, Node? source)
        {
            if (source != null)
            {
                if (FlexibleStructure)
                {
                    target.AddChild(source);
                    source.Owner = target;
                    SetChildrenOwner(target, source);
                }
                else
                {
                    foreach (var child in source.GetChildren())
                    {
                        source.RemoveChild(child);
                        ClearSubtreeOwnersForReparent(child);
                        target.AddChild(child);
                        child.Owner = target;
                        SetChildrenOwner(target, child);
                    }

                    source.QueueFree();
                }
            }

            var uniqueNames = new List<IRitsuGodotNodeSlot>();
            var placeholder = new Node();
            foreach (var named in NamedNodes)
            {
                if (named.UniqueName)
                {
                    uniqueNames.Add(named);
                    continue;
                }

                var node = target.GetNodeOrNull(named.Path);
                if (node != null)
                {
                    if (!named.IsValidType(node))
                    {
                        node.ReplaceBy(placeholder);
                        node = ConvertNodeTypeAndFreeUnusedSource(node, named.ExpectedNodeType);
                        placeholder.ReplaceBy(node);
                    }

                    if (!named.MakeNameUnique) continue;
                    node.UniqueNameInOwner = true;
                    node.Owner = target;
                }
                else
                {
                    GenerateNode(target, named);
                }
            }

            Dictionary<IRitsuGodotNodeSlot, Node> backupUniqueNodes = [];
            foreach (var child in target.GetChildrenRecursive<Node>())
                for (var index = 0; index < uniqueNames.Count; index++)
                {
                    var unique = uniqueNames[index];
                    if (unique.IsValidName(child))
                        backupUniqueNodes[unique] = child;
                    if (!unique.IsValidUnique(child))
                        continue;

                    child.UniqueNameInOwner = true;
                    child.Owner = target;
                    uniqueNames.RemoveAt(index);
                    break;
                }

            foreach (var missing in uniqueNames)
                if (backupUniqueNodes.TryGetValue(missing, out var node))
                {
                    if (!missing.IsValidType(node))
                    {
                        node.ReplaceBy(placeholder);
                        node = ConvertNodeTypeAndFreeUnusedSource(node, missing.ExpectedNodeType);
                        placeholder.ReplaceBy(node);
                    }

                    node.UniqueNameInOwner = true;
                    node.Owner = target;
                }
                else
                {
                    GenerateNode(target, missing);
                }

            placeholder.QueueFree();
        }

        private Node ConvertNodeTypeAndFreeUnusedSource(Node source, Type targetType)
        {
            var sourceType = source.GetType();
            var sourceName = source.Name;
            var converted = ConvertNodeType(source, targetType);
            if (!GodotObject.IsInstanceValid(converted))
                throw new InvalidOperationException(
                    $"Factory for {typeof(T).Name} returned a null or invalid {targetType.Name} replacement for " +
                    $"{sourceType.Name} '{sourceName}'.");

            if (!ReferenceEquals(converted, source) &&
                GodotObject.IsInstanceValid(source) &&
                source.GetParent() == null)
                source.Free();

            return converted;
        }

        protected virtual Node ConvertNodeType(Node node, Type targetType)
        {
            throw new InvalidOperationException(
                $"Factory for {typeof(T).Name} cannot convert {node.GetType().Name} '{node.Name}' to {targetType.Name}.");
        }

        protected abstract void GenerateNode(T target, IRitsuGodotNodeSlot required);

        private static void FreeFailedConversion(T target, Node source)
        {
            if (GodotObject.IsInstanceValid(target))
                target.Free();
            if (GodotObject.IsInstanceValid(source))
                source.Free();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Clears owners before reparenting because packed-scene children can retain the old root as
        ///         <see cref="Node.Owner" /> after <c>RemoveChild</c>. Leaving those owners in place can produce Godot
        ///         “inconsistent owner” warnings and break unique-name resolution beneath the new root.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在重新设置父节点之前清除所有者，因为打包场景中的子节点在调用 <c>RemoveChild</c> 后仍可能以旧根节点
        ///         作为 <see cref="Node.Owner" />。保留这些所有者会产生 Godot“所有者不一致”警告，并可能破坏新根节点下
        ///         的唯一名称解析。
        ///     </para>
        /// </summary>
        private static void ClearSubtreeOwnersForReparent(Node node)
        {
            foreach (var descendant in node.GetChildren())
                ClearSubtreeOwnersForReparent(descendant);

            node.Owner = null;
        }

        protected static void SetChildrenOwner(Node target, Node child)
        {
            foreach (var grandchild in child.GetChildren())
            {
                grandchild.Owner = target;
                SetChildrenOwner(target, grandchild);
            }
        }

        protected static void CopyControlProperties(Control target, Control source)
        {
            CopyCanvasItemProperties(target, source);
            target.LayoutMode = source.LayoutMode;
            target.AnchorLeft = source.AnchorLeft;
            target.AnchorTop = source.AnchorTop;
            target.AnchorRight = source.AnchorRight;
            target.AnchorBottom = source.AnchorBottom;
            target.OffsetLeft = source.OffsetLeft;
            target.OffsetTop = source.OffsetTop;
            target.OffsetRight = source.OffsetRight;
            target.OffsetBottom = source.OffsetBottom;
            target.GrowHorizontal = source.GrowHorizontal;
            target.GrowVertical = source.GrowVertical;
            target.Size = source.Size;
            target.CustomMinimumSize = source.CustomMinimumSize;
            target.PivotOffset = source.PivotOffset;
            target.MouseFilter = source.MouseFilter;
            target.FocusMode = source.FocusMode;
            target.ClipContents = source.ClipContents;
        }

        protected static void CopyCanvasItemProperties(CanvasItem target, CanvasItem source)
        {
            target.Visible = source.Visible;
            target.Modulate = source.Modulate;
            target.SelfModulate = source.SelfModulate;
            target.ShowBehindParent = source.ShowBehindParent;
            target.TopLevel = source.TopLevel;
            target.ZIndex = source.ZIndex;
            target.ZAsRelative = source.ZAsRelative;
            target.YSortEnabled = source.YSortEnabled;
            target.TextureFilter = source.TextureFilter;
            target.TextureRepeat = source.TextureRepeat;
            target.Material = source.Material;
            target.UseParentMaterial = source.UseParentMaterial;

            if (target is not Node2D targetNode2D || source is not Node2D sourceNode2D) return;
            targetNode2D.Position = sourceNode2D.Position;
            targetNode2D.Rotation = sourceNode2D.Rotation;
            targetNode2D.Scale = sourceNode2D.Scale;
            targetNode2D.Skew = sourceNode2D.Skew;
        }
    }
}
