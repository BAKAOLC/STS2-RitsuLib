using Godot;
using MegaCrit.Sts2.Core.Nodes;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    internal static class NodeAttachmentRuntime
    {
        private static readonly AttachedState<Node, Dictionary<string, Node>> AttachedNodes =
            new(() => new(StringComparer.OrdinalIgnoreCase));

        private static readonly AttachedState<Node, HashSet<string>> PendingAttachmentIds =
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
                    LogAttachmentFailure(definition, parent.GetType(), ex);
                }
        }

        private static void Attach(Node parent, NodeAttachmentDefinition definition)
        {
            var attachParent = ResolveAttachParent(parent, definition);
            var attached = AttachedNodes.GetOrCreate(parent);
            var pending = PendingAttachmentIds.GetOrCreate(parent);
            if (pending.Contains(definition.Id))
                return;

            if (attached.TryGetValue(definition.Id, out var tracked))
            {
                if (GodotObject.IsInstanceValid(tracked))
                {
                    if (EnsureAttached(attachParent, tracked, definition))
                        return;

                    pending.Add(definition.Id);
                    ScheduleDeferredAttachment(
                        parent,
                        attachParent,
                        tracked,
                        definition,
                        attached,
                        pending,
                        false);
                    return;
                }

                attached.Remove(definition.Id);
            }

            if (!string.IsNullOrWhiteSpace(definition.Name) &&
                TryFindDirectChildByName(attachParent, definition.Name, out var existing))
                switch (definition.Options.DuplicatePolicy)
                {
                    case NodeAttachmentDuplicatePolicy.AllowDuplicateName:
                        break;
                    case NodeAttachmentDuplicatePolicy.ReuseExistingByName:
                        if (!definition.NodeType.IsInstanceOfType(existing))
                            throw new InvalidOperationException(
                                $"Existing child '{definition.Name}' is {existing.GetType().FullName}, expected {definition.NodeType.FullName}.");
                        attached[definition.Id] = existing;
                        ApplyNodeOptions(existing, definition);
                        if (definition.Options.UniqueNameInOwner)
                            existing.Owner = attachParent;
                        ApplyInsertion(attachParent, existing, definition);
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
#pragma warning disable CA2208
                        throw new ArgumentOutOfRangeException(nameof(definition.Options.DuplicatePolicy));
#pragma warning restore CA2208
                }

            var child = definition.CreateNode(parent);
            try
            {
                ApplyNodeOptions(child, definition);

                if (definition.Options.SetupTiming == NodeAttachmentSetupTiming.BeforeAdd)
                    definition.RunSetup(parent, child);

                if (!EnsureAttached(attachParent, child, definition))
                {
                    pending.Add(definition.Id);
                    ScheduleDeferredAttachment(parent, attachParent, child, definition, attached, pending, true);
                    return;
                }

                if (definition.Options.SetupTiming == NodeAttachmentSetupTiming.AfterAdd)
                    definition.RunSetup(parent, child);

                attached[definition.Id] = child;
            }
            catch
            {
                pending.Remove(definition.Id);
                FreeFailedChild(child, attachParent);
                throw;
            }
        }

        private static Node ResolveAttachParent(Node lifecycleParent, NodeAttachmentDefinition definition)
        {
            var attachParent = definition.Options.AttachParentSelector?.Invoke(lifecycleParent) ?? lifecycleParent;
            if (!GodotObject.IsInstanceValid(attachParent))
                throw new InvalidOperationException(
                    $"Node attachment '{definition.Id}' resolved an invalid attach parent.");

            return attachParent;
        }

        private static bool EnsureAttached(Node attachParent, Node child, NodeAttachmentDefinition definition)
        {
            if (!GodotObject.IsInstanceValid(child))
                throw new InvalidOperationException(
                    $"Node attachment '{definition.Id}' produced an invalid node instance.");

            var currentParent = child.GetParent();
            if (currentParent == attachParent)
            {
                ApplyInsertion(attachParent, child, definition);
                return true;
            }

            if (currentParent != null)
                throw new InvalidOperationException(
                    $"Node attachment '{definition.Id}' child already belongs to {currentParent.GetType().FullName}.");

            switch (definition.Options.AddMode)
            {
                case NodeAttachmentAddMode.AddChildSafely:
                    if (!NGame.IsMainThread() || attachParent.IsInsideTree() && !attachParent.IsNodeReady())
                        return false;

                    attachParent.AddChild(child);
                    break;
                case NodeAttachmentAddMode.AddChildDirect:
                    attachParent.AddChild(child);
                    break;
                default:
#pragma warning disable CA2208
                    throw new ArgumentOutOfRangeException(nameof(definition.Options.AddMode));
#pragma warning restore CA2208
            }

            if (definition.Options.UniqueNameInOwner)
                child.Owner = attachParent;

            ApplyInsertion(attachParent, child, definition);
            return true;
        }

        private static void ScheduleDeferredAttachment(
            Node lifecycleParent,
            Node attachParent,
            Node child,
            NodeAttachmentDefinition definition,
            Dictionary<string, Node> attached,
            HashSet<string> pending,
            bool runAfterAddSetup)
        {
            Callable.From(() =>
            {
                pending.Remove(definition.Id);
                try
                {
                    if (!GodotObject.IsInstanceValid(lifecycleParent))
                        throw new InvalidOperationException(
                            $"Node attachment '{definition.Id}' lifecycle parent is no longer valid.");

                    if (!GodotObject.IsInstanceValid(attachParent))
                        throw new InvalidOperationException(
                            $"Node attachment '{definition.Id}' attach parent is no longer valid.");

                    if (!GodotObject.IsInstanceValid(child))
                        throw new InvalidOperationException(
                            $"Node attachment '{definition.Id}' child is no longer valid.");

                    var currentParent = child.GetParent();
                    if (currentParent == null)
                        attachParent.AddChild(child);
                    else if (currentParent != attachParent)
                        throw new InvalidOperationException(
                            $"Node attachment '{definition.Id}' child already belongs to {currentParent.GetType().FullName}.");

                    if (definition.Options.UniqueNameInOwner)
                        child.Owner = attachParent;

                    ApplyInsertion(attachParent, child, definition);

                    if (runAfterAddSetup &&
                        definition.Options.SetupTiming == NodeAttachmentSetupTiming.AfterAdd)
                        definition.RunSetup(lifecycleParent, child);

                    attached[definition.Id] = child;
                }
                catch (Exception ex)
                {
                    FreeFailedChild(child, attachParent);
                    LogAttachmentFailure(definition, definition.ParentType, ex);
                }
            }).CallDeferred();
        }

        private static void FreeFailedChild(Node child, Node attachParent)
        {
            if (!GodotObject.IsInstanceValid(child))
                return;

            var currentParent = child.GetParent();
            if (currentParent != null && currentParent != attachParent)
                return;

            currentParent?.RemoveChild(child);
            child.Free();
        }

        private static void LogAttachmentFailure(
            NodeAttachmentDefinition definition,
            Type parentType,
            Exception exception)
        {
            RitsuLibFramework.Logger.ErrorNoTrace(
                $"[NodeAttachment] Failed to attach '{definition.Id}' to {parentType.FullName}: {exception.Message}");
            RitsuLibFramework.Logger.Debug(exception.ToString());
        }

        private static void ApplyNodeOptions(Node child, NodeAttachmentDefinition definition)
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
