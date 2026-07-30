using Godot;

namespace STS2RitsuLib.Utils
{
    internal static class RitsuGodotAwaitSafety
    {
        internal static async Task AwaitProcessFrameAsync(SceneTree? tree,
            GodotObject? owner = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            ThrowIfInvalid(owner, ct);

            if (tree == null || !GodotObject.IsInstanceValid(tree))
            {
                await Task.Yield();
                ct.ThrowIfCancellationRequested();
                ThrowIfInvalid(owner, ct);
                return;
            }

            await AwaitProcessFrameSignalAsync(tree).WaitAsync(ct);

            ct.ThrowIfCancellationRequested();
            ThrowIfInvalid(owner, ct);
            if (!GodotObject.IsInstanceValid(tree))
                throw new OperationCanceledException("Scene tree was deleted while awaiting a process frame.", ct);
        }

        private static async Task AwaitProcessFrameSignalAsync(SceneTree tree)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }

        internal static async Task AwaitProcessFramesAsync(SceneTree? tree, int count,
            GodotObject? owner = null, CancellationToken ct = default)
        {
            for (var i = 0; i < count; i++)
                await AwaitProcessFrameAsync(tree, owner, ct);
        }

        private static void ThrowIfInvalid(GodotObject? owner, CancellationToken ct)
        {
            if (owner != null && !GodotObject.IsInstanceValid(owner))
                throw new OperationCanceledException("Godot owner was deleted while awaiting a callback.", ct);
        }
    }
}
