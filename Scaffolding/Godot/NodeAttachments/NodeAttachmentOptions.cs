namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    /// <summary>
    ///     Options for ready-time child node attachment.
    /// </summary>
    public sealed class NodeAttachmentOptions
    {
        /// <summary>
        ///     Optional direct-child name assigned before the node is added.
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        ///     Stable ordering among attachments on the same parent; lower values run first.
        /// </summary>
        public int Order { get; init; }

        /// <summary>
        ///     Sets <c>UniqueNameInOwner</c> and assigns the parent as owner after the node is added.
        /// </summary>
        public bool UniqueNameInOwner { get; init; }

        /// <summary>
        ///     When true, an attachment registered for a base parent type also applies to derived node instances.
        /// </summary>
        public bool IncludeDerivedParentTypes { get; init; } = true;

        /// <summary>
        ///     Policy for an existing direct child with <see cref="Name" />.
        /// </summary>
        public NodeAttachmentDuplicatePolicy DuplicatePolicy { get; init; } =
            NodeAttachmentDuplicatePolicy.AllowDuplicateName;

        /// <summary>
        ///     Method used to add the child to the parent.
        /// </summary>
        public NodeAttachmentAddMode AddMode { get; init; } = NodeAttachmentAddMode.AddChildSafely;

        /// <summary>
        ///     Whether setup runs before or after the child is added to the tree.
        /// </summary>
        public NodeAttachmentSetupTiming SetupTiming { get; init; } = NodeAttachmentSetupTiming.BeforeAdd;

        /// <summary>
        ///     Optional final direct-child index after attachment.
        /// </summary>
        public int? ChildIndex { get; init; }

        /// <summary>
        ///     Optional sibling name to insert before.
        /// </summary>
        public string? InsertBeforeName { get; init; }

        /// <summary>
        ///     Optional sibling name to insert after.
        /// </summary>
        public string? InsertAfterName { get; init; }

        /// <summary>
        ///     Queues a replaced existing child for freeing when <see cref="DuplicatePolicy" /> is ReplaceExistingByName.
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
