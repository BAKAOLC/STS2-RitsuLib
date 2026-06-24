using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace STS2RitsuLib.Utils
{
    internal static class RitsuMainThread
    {
        internal static Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (NGame.IsMainThread())
            {
                action();
                return Task.CompletedTask;
            }

            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree?.Root == null)
            {
                action();
                return Task.CompletedTask;
            }

            var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Callable.From(() =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        source.TrySetCanceled(cancellationToken);
                        return;
                    }

                    action();
                    source.TrySetResult();
                }
                catch (Exception ex)
                {
                    source.TrySetException(ex);
                }
            }).CallDeferred();

            return cancellationToken.CanBeCanceled
                ? source.Task.WaitAsync(cancellationToken)
                : source.Task;
        }

        internal static Task<T> InvokeAsync<T>(Func<T> action, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (NGame.IsMainThread())
                return Task.FromResult(action());

            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree?.Root == null)
                return Task.FromResult(action());

            var source = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            Callable.From(() =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        source.TrySetCanceled(cancellationToken);
                        return;
                    }

                    source.TrySetResult(action());
                }
                catch (Exception ex)
                {
                    source.TrySetException(ex);
                }
            }).CallDeferred();

            return cancellationToken.CanBeCanceled
                ? source.Task.WaitAsync(cancellationToken)
                : source.Task;
        }
    }
}
