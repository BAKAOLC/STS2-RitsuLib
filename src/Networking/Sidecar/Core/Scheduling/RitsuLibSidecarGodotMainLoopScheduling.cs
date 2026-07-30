using Godot;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defers work to the Godot scene-tree main loop through <see cref="Callable.CallDeferred" /> and provides
    ///         continuation helpers for Sidecar <see cref="Task" /> results.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过 <see cref="Callable.CallDeferred" /> 将处理延后到 Godot 场景树主循环，
    ///         并为 Sidecar <see cref="Task" /> 结果提供延续方法。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarGodotMainLoopScheduling
    {
        /// <summary>
        ///     <para xml:lang="en">Queues <paramref name="action" /> on the Godot main loop when a <see cref="SceneTree" /> is available.</para>
        ///     <para xml:lang="zh-CN"><see cref="SceneTree" /> 可用时，将 <paramref name="action" /> 排入 Godot 主循环。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when the delegate was queued; <see langword="false" /> when the main loop is unavailable.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         委托已排队时为 <see langword="true" />；主循环不可用时为 <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryPostToMainLoop(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            if (Engine.GetMainLoop() is not SceneTree)
                return false;

            Callable.From(action).CallDeferred();
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         After <paramref name="task" /> completes, completes the returned task on the Godot main loop when
        ///         possible; otherwise completes it on the thread-pool continuation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <paramref name="task" /> 完成后，尽可能在 Godot 主循环上完成返回的任务；
        ///         否则在线程池延续中完成。
        ///     </para>
        /// </summary>
        public static Task ContinueOnGodotMainLoopAsync(this Task task)
        {
            return task.ContinueWith(
                t =>
                {
                    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                    if (!TryPostToMainLoop(Complete))
                        Complete();

                    return tcs.Task;

                    void Complete()
                    {
                        if (t.IsFaulted)
                        {
                            var ex = t.Exception?.GetBaseException() ?? t.Exception;
                            if (ex != null)
                                tcs.TrySetException(ex);
                            else
                                tcs.TrySetException(new InvalidOperationException("Sidecar task faulted."));
                        }
                        else if (t.IsCanceled)
                        {
                            tcs.TrySetCanceled();
                        }
                        else
                        {
                            tcs.TrySetResult();
                        }
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default).Unwrap();
        }

        /// <inheritdoc cref="ContinueOnGodotMainLoopAsync(Task)" />
        public static Task<T> ContinueOnGodotMainLoopAsync<T>(this Task<T> task)
        {
            return task.ContinueWith(
                t =>
                {
                    var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

                    if (!TryPostToMainLoop(Complete))
                        Complete();

                    return tcs.Task;

                    void Complete()
                    {
                        if (t.IsFaulted)
                        {
                            var ex = t.Exception?.GetBaseException() ?? t.Exception;
                            if (ex != null)
                                tcs.TrySetException(ex);
                            else
                                tcs.TrySetException(new InvalidOperationException("Sidecar task faulted."));
                        }
                        else if (t.IsCanceled)
                        {
                            tcs.TrySetCanceled();
                        }
                        else
                        {
                            tcs.TrySetResult(t.Result);
                        }
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default).Unwrap();
        }
    }
}
