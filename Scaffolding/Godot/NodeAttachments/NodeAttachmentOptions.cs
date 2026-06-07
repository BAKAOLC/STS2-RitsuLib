using Godot;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    /// <summary>
    ///     Options for ready-time child node attachment.
    ///     ready 阶段子节点挂载选项。
    /// </summary>
    public sealed class NodeAttachmentOptions
    {
        /// <summary>
        ///     Optional direct-child name assigned before the node is added.
        ///     节点加入前分配的可选直接子节点名称。
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        ///     Stable ordering among attachments on the same parent; lower values run first.
        ///     同一父节点上各挂载项的稳定顺序；值越小越先执行。
        /// </summary>
        public int Order { get; init; }

        /// <summary>
        ///     Sets <c>UniqueNameInOwner</c> and assigns the parent as owner after the node is added.
        ///     设置 <c>UniqueNameInOwner</c>，并在节点加入后将父节点设为 owner。
        /// </summary>
        public bool UniqueNameInOwner { get; init; }

        /// <summary>
        ///     When true, an attachment registered for a base parent type also applies to derived node instances.
        ///     为 true 时，注册到父基类的挂载项也会应用到派生节点实例。
        /// </summary>
        public bool IncludeDerivedParentTypes { get; init; } = true;

        /// <summary>
        ///     Policy for an existing direct child with <see cref="Name" />.
        ///     处理已有 <see cref="Name" /> 直接子节点的策略。
        /// </summary>
        public NodeAttachmentDuplicatePolicy DuplicatePolicy { get; init; } =
            NodeAttachmentDuplicatePolicy.AllowDuplicateName;

        /// <summary>
        ///     Method used to add the child to the parent.
        ///     将子节点加入父节点时使用的方法。
        /// </summary>
        public NodeAttachmentAddMode AddMode { get; init; } = NodeAttachmentAddMode.AddChildSafely;

        /// <summary>
        ///     Optional selector for the concrete node that receives the child. The lifecycle parent is passed in.
        ///     可选的实际子节点接收者选择器；参数为生命周期父节点。
        /// </summary>
        public Func<Node, Node?>? AttachParentSelector { get; init; }

        /// <summary>
        ///     Whether setup runs before or after the child is added to the tree.
        ///     setup 在子节点加入树前还是加入树后运行。
        /// </summary>
        public NodeAttachmentSetupTiming SetupTiming { get; init; } = NodeAttachmentSetupTiming.BeforeAdd;

        /// <summary>
        ///     Optional final direct-child index after attachment.
        ///     挂载后的可选最终直接子节点索引。
        /// </summary>
        public int? ChildIndex { get; init; }

        /// <summary>
        ///     Optional sibling name to insert before.
        ///     可选：插入到该同级节点名称之前。
        /// </summary>
        public string? InsertBeforeName { get; init; }

        /// <summary>
        ///     Optional sibling name to insert after.
        ///     可选：插入到该同级节点名称之后。
        /// </summary>
        public string? InsertAfterName { get; init; }

        /// <summary>
        ///     Queues a replaced existing child for freeing when <see cref="DuplicatePolicy" /> is ReplaceExistingByName.
        ///     当 <see cref="DuplicatePolicy" /> 为 ReplaceExistingByName 时，对被替换的已有子节点调用 QueueFree。
        /// </summary>
        public bool QueueFreeReplacedNode { get; init; } = true;

        internal static NodeAttachmentOptions Default { get; } = new();

        internal void Validate(string attachmentId)
        {
            var insertionTargets = 0;
            if (ChildIndex.HasValue)
                insertionTargets++;
            if (!string.IsNullOrWhiteSpace(InsertBeforeName))
                insertionTargets++;
            if (!string.IsNullOrWhiteSpace(InsertAfterName))
                insertionTargets++;

            if (insertionTargets > 1)
                throw new InvalidOperationException(
                    $"Node attachment '{attachmentId}' can specify only one insertion option.");

            if (ChildIndex is < 0)
                throw new InvalidOperationException(
                    $"Node attachment '{attachmentId}' cannot use a negative child index.");

            if (DuplicatePolicy != NodeAttachmentDuplicatePolicy.AllowDuplicateName && string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException(
                    $"Node attachment '{attachmentId}' must set {nameof(Name)} when using duplicate policy {DuplicatePolicy}.");
        }
    }
}
