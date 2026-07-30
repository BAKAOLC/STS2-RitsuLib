using Godot;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    /// <summary>
    ///     <para xml:lang="en">Provides options for attaching child nodes during <c>_Ready</c>.</para>
    ///     <para xml:lang="zh-CN">提供在 <c>_Ready</c> 阶段附加子节点的选项。</para>
    /// </summary>
    public sealed class NodeAttachmentOptions
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the optional direct-child name assigned before the node is added.</para>
        ///     <para xml:lang="zh-CN">获取在添加节点之前为其分配的可选直接子节点名称。</para>
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the stable order among attachments on the same parent; lower values run first.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取同一父节点上各附加项的稳定顺序；值越小越先执行。</para>
        /// </summary>
        public int Order { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether to set <c>UniqueNameInOwner</c> and assign the parent as owner after adding the node.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取是否设置 <c>UniqueNameInOwner</c>，并在添加节点后将父节点指定为其所有者。
        ///     </para>
        /// </summary>
        public bool UniqueNameInOwner { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether an attachment registered for a base parent type also applies to derived node instances.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取注册到父节点基类的附加项是否也应用于派生节点实例。</para>
        /// </summary>
        public bool IncludeDerivedParentTypes { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets the policy for an existing direct child named <see cref="Name" />.</para>
        ///     <para xml:lang="zh-CN">获取用于处理名为 <see cref="Name" /> 的现有直接子节点的策略。</para>
        /// </summary>
        public NodeAttachmentDuplicatePolicy DuplicatePolicy { get; init; } =
            NodeAttachmentDuplicatePolicy.AllowDuplicateName;

        /// <summary>
        ///     <para xml:lang="en">Gets the method used to add the child to the parent.</para>
        ///     <para xml:lang="zh-CN">获取将子节点添加到父节点时使用的方法。</para>
        /// </summary>
        public NodeAttachmentAddMode AddMode { get; init; } = NodeAttachmentAddMode.AddChildSafely;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets an optional selector for the node that receives the child. The lifecycle parent is passed to the selector.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取可选的子节点接收者选择器；选择器的参数是参与生命周期回调的父节点。
        ///     </para>
        /// </summary>
        public Func<Node, Node?>? AttachParentSelector { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether setup runs before or after the child is added to the tree.</para>
        ///     <para xml:lang="zh-CN">获取在子节点添加到场景树之前还是之后执行配置。</para>
        /// </summary>
        public NodeAttachmentSetupTiming SetupTiming { get; init; } = NodeAttachmentSetupTiming.BeforeAdd;

        /// <summary>
        ///     <para xml:lang="en">Gets the optional final direct-child index after attachment.</para>
        ///     <para xml:lang="zh-CN">获取附加完成后可选的直接子节点最终索引。</para>
        /// </summary>
        public int? ChildIndex { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional name of the sibling before which the child is inserted.</para>
        ///     <para xml:lang="zh-CN">获取可选的同级节点名称；子节点将插入到该节点之前。</para>
        /// </summary>
        public string? InsertBeforeName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional name of the sibling after which the child is inserted.</para>
        ///     <para xml:lang="zh-CN">获取可选的同级节点名称；子节点将插入到该节点之后。</para>
        /// </summary>
        public string? InsertAfterName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether a replaced child is queued for deletion when <see cref="DuplicatePolicy" /> is
        ///         <see cref="NodeAttachmentDuplicatePolicy.ReplaceExistingByName" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取当 <see cref="DuplicatePolicy" /> 为
        ///         <see cref="NodeAttachmentDuplicatePolicy.ReplaceExistingByName" /> 时，是否将被替换的子节点排队释放。
        ///     </para>
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
