using Godot;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    internal static class NodeAttachmentRuntime
    {
        private static readonly AttachedState<Node, Dictionary<string, Node>> AttachedNodes =
            new(() => new(StringComparer.OrdinalIgnoreCase));

        public static bool TryGetAttached<TParent, TNode>(TParent parent, string id, out TNode node)
            where TParent : Node
            where TNode : Node
        {
            ArgumentNullException.ThrowIfNull(parent);
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            node = null!;
            if (!AttachedNodes.TryGetValue(parent, out var attached) ||
                !attached.TryGetValue(id.Trim(), out var stored) ||
                !GodotObject.IsInstanceValid(stored) ||
                stored is not TNode typed)
                return false;

            node = typed;
            return true;
        }

        internal static void AttachReadyChildren(Node parent)
        {
            if (!GodotObject.IsInstanceValid(parent))
                return;

            var definitions = ModNodeAttachmentRegistry.GetDefinitionsForParent(parent);
            foreach (var definition in definitions)
                try
                {
                    Attach(parent, definition);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[NodeAttachment] Failed to attach '{definition.Id}' to {parent.GetType().FullName}: {ex.Message}");
                    RitsuLibFramework.Logger.Debug(ex.ToString());
                }
        }

        private static void Attach(Node parent, NodeAttachmentDefinition definition)
        {
            var attached = AttachedNodes.GetOrCreate(parent);
            if (attached.TryGetValue(definition.Id, out var tracked))
            {
                if (GodotObject.IsInstanceValid(tracked))
                {
                    EnsureAttached(parent, tracked, definition);
                    return;
                }

                attached.Remove(definition.Id);
            }

            if (!string.IsNullOrWhiteSpace(definition.Name) &&
                TryFindDirectChildByName(parent, definition.Name, out var existing))
                switch (definition.Options.DuplicatePolicy)
                {
                    case NodeAttachmentDuplicatePolicy.AllowDuplicateName:
                        break;
                    case NodeAttachmentDuplicatePolicy.ReuseExistingByName:
                        if (!definition.NodeType.IsInstanceOfType(existing))
                            throw new InvalidOperationException(
                                $"Existing child '{definition.Name}' is {existing.GetType().FullName}, expected {definition.NodeType.FullName}.");
                        attached[definition.Id] = existing;
                        ApplyNodeOptions(parent, existing, definition);
                        ApplyInsertion(parent, existing, definition);
                        return;
                    case NodeAttachmentDuplicatePolicy.SkipIfExistingByName:
                        return;
                    case NodeAttachmentDuplicatePolicy.ReplaceExistingByName:
                        RemoveExistingChild(existing, definition.Options.QueueFreeReplacedNode);
                        break;
                    case NodeAttachmentDuplicatePolicy.ThrowIfExistingByName:
                        throw new InvalidOperationException(
                            $"Parent {parent.GetType().FullName} already has a direct child named '{definition.Name}'.");
                    default:
                        throw new ArgumentOutOfRangeException(nameof(definition.Options.DuplicatePolicy));
                }

            var child = definition.CreateNode(parent);
            ApplyNodeOptions(parent, child, definition);

            if (definition.Options.SetupTiming == NodeAttachmentSetupTiming.BeforeAdd)
                definition.RunSetup(parent, child);

            EnsureAttached(parent, child, definition);

            if (definition.Options.SetupTiming == NodeAttachmentSetupTiming.AfterAdd)
                definition.RunSetup(parent, child);

            attached[definition.Id] = child;
        }

        private static void EnsureAttached(Node parent, Node child, NodeAttachmentDefinition definition)
        {
            if (!GodotObject.IsInstanceValid(child))
                throw new InvalidOperationException(
                    $"Node attachment '{definition.Id}' produced an invalid node instance.");

            var currentParent = child.GetParent();
            if (currentParent == parent)
            {
                ApplyInsertion(parent, child, definition);
                return;
            }

            if (currentParent != null)
                throw new InvalidOperationException(
                    $"Node attachment '{definition.Id}' child already belongs to {currentParent.GetType().FullName}.");

            switch (definition.Options.AddMode)
            {
                case NodeAttachmentAddMode.AddChildSafely:
                    RitsuGodotTreeCompat.AddChildSafely(parent, child);
                    break;
                case NodeAttachmentAddMode.AddChildDirect:
                    parent.AddChild(child);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(definition.Options.AddMode));
            }

            if (definition.Options.UniqueNameInOwner)
                child.Owner = parent;

            ApplyInsertion(parent, child, definition);
        }

        private static void ApplyNodeOptions(Node parent, Node child, NodeAttachmentDefinition definition)
        {
            if (!string.IsNullOrWhiteSpace(definition.Name))
                child.Name = definition.Name;

            if (definition.Options.UniqueNameInOwner)
                child.UniqueNameInOwner = true;
        }

        private static void RemoveExistingChild(Node existing, bool queueFree)
        {
            var parent = existing.GetParent();
            parent?.RemoveChild(existing);
            if (queueFree)
                existing.QueueFree();
        }

        private static void ApplyInsertion(Node parent, Node child, NodeAttachmentDefinition definition)
        {
            if (child.GetParent() != parent)
                return;

            var targetIndex = ResolveTargetIndex(parent, definition);
            if (!targetIndex.HasValue)
                return;

            var clampedIndex = Math.Clamp(targetIndex.Value, 0, Math.Max(0, parent.GetChildCount() - 1));
            if (child.GetIndex() == clampedIndex)
                return;

            RitsuGodotTreeCompat.MoveChildSafely(parent, child, clampedIndex);
        }

        private static int? ResolveTargetIndex(Node parent, NodeAttachmentDefinition definition)
        {
            if (definition.Options.ChildIndex.HasValue)
                return definition.Options.ChildIndex.Value;

            if (!string.IsNullOrWhiteSpace(definition.Options.InsertBeforeName) &&
                TryFindDirectChildByName(parent, definition.Options.InsertBeforeName, out var before))
                return before.GetIndex();

            if (!string.IsNullOrWhiteSpace(definition.Options.InsertAfterName) &&
                TryFindDirectChildByName(parent, definition.Options.InsertAfterName, out var after))
                return after.GetIndex() + 1;

            return null;
        }

        private static bool TryFindDirectChildByName(Node parent, string name, out Node child)
        {
            for (var i = 0; i < parent.GetChildCount(); i++)
            {
                var candidate = parent.GetChild(i);
                if (candidate.Name.ToString() != name) continue;
                child = candidate;
                return true;
            }

            child = null!;
            return false;
        }
    }
}
