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

            var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var registration = ct.CanBeCanceled
                ? ct.Register(static state =>
                    ((TaskCompletionSource)state!).TrySetCanceled(), source)
                : default;

            Callable.From(() =>
            {
                try
                {
                    if (ct.IsCancellationRequested || (owner != null && !GodotObject.IsInstanceValid(owner)))
                    {
                        source.TrySetCanceled(ct);
                        return;
                    }

                    source.TrySetResult();
                }
                catch (Exception ex)
                {
                    source.TrySetException(ex);
                }
            }).CallDeferred();

            try
            {
                await source.Task;
            }
            finally
            {
                await registration.DisposeAsync();
            }

            ct.ThrowIfCancellationRequested();
            ThrowIfInvalid(owner, ct);
            if (!GodotObject.IsInstanceValid(tree))
                throw new OperationCanceledException("Scene tree was deleted while awaiting a process frame.", ct);
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
