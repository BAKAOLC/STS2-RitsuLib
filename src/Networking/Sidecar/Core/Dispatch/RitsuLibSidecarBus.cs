namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Opcode dispatch for sidecar payloads. Registration uses the same 64-bit opcodes as
    ///     <see cref="RitsuLibSidecarOpcodes.For" />.
    ///     sidecar 载荷的 opcode 分发。注册使用与
    ///     <see cref="RitsuLibSidecarOpcodes.For" /> 相同的 64 位 opcode。
    /// </summary>
    public static class RitsuLibSidecarBus
    {
        private static readonly Lock Gate = new();

        private static readonly Dictionary<ulong, Action<RitsuLibSidecarDispatchContext>> Handlers = [];
        private static readonly List<PendingWaiter> Waiters = [];

        /// <summary>
        ///     Registers or replaces a handler for an opcode. Unregister when leaving multiplayer to avoid leaks.
        ///     注册或替换某个 opcode 的处理器。离开多人游戏时请取消注册，以避免泄漏。
        /// </summary>
        public static void RegisterHandler(ulong opcode, Action<RitsuLibSidecarDispatchContext> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            lock (Gate)
            {
                Handlers[opcode] = handler;
            }
        }

        /// <summary>
        ///     Removes a handler for the opcode, if present.
        ///     移除该 opcode 的处理器（如果存在）。
        /// </summary>
        public static void UnregisterHandler(ulong opcode)
        {
            lock (Gate)
            {
                Handlers.Remove(opcode);
            }
        }

        /// <summary>
        ///     Removes all opcode handlers (e.g. when leaving multiplayer).
        ///     移除所有 opcode 处理器（例如离开多人游戏时）。
        /// </summary>
        public static void ClearHandlers()
        {
            lock (Gate)
            {
                Handlers.Clear();
            }
        }

        /// <summary>
        ///     Number of active <see cref="WaitForNextAsync" /> waiters (snapshot under lock).
        ///     活动 <see cref="WaitForNextAsync" /> 等待者数量（在锁内取得的快照）。
        /// </summary>
        public static int GetPendingWaiterCount()
        {
            lock (Gate)
            {
                return Waiters.Count;
            }
        }

        /// <summary>
        ///     Removes every pending <see cref="WaitForNextAsync" /> waiter and completes each task as canceled. Does
        ///     not remove opcode handlers (including built-in control handlers).
        ///     移除每个挂起的 <see cref="WaitForNextAsync" /> 等待者，并将各任务完成为已取消。不会
        ///     移除 opcode 处理器（包括内置控制处理器）。
        /// </summary>
        public static void CancelAllPendingWaits()
        {
            List<PendingWaiter> pending;
            lock (Gate)
            {
                pending = [..Waiters];
                Waiters.Clear();
            }

            foreach (var w in pending)
                w.Tcs.TrySetCanceled();
        }

        /// <summary>
        ///     Waits once for a matching opcode packet, useful for request/reply control flows.
        ///     等待一次匹配 opcode 的数据包，适用于请求/回复控制流。
        /// </summary>
        /// <remarks>
        ///     Timeout uses <see cref="CancellationToken.None" /> on <see cref="Task.Delay(TimeSpan, CancellationToken)" />;
        ///     user cancellation is observed through <paramref name="cancellationToken" /> separately so both paths
        ///     can complete the waiter without linking tokens. The completed task’s continuations are not marshaled to
        ///     the Godot main loop; use
        ///     <see cref="RitsuLibSidecarGodotMainLoopScheduling.ContinueOnGodotMainLoopAsync{T}(Task{T})" /> when needed.
        ///     超时使用 <see cref="CancellationToken.None" /> 调用 <see cref="Task.Delay(TimeSpan, CancellationToken)" />；
        ///     用户取消通过 <paramref name="cancellationToken" /> 单独观察，因此两条路径
        ///     都可以完成等待者而无需链接 token。已完成任务的 continuation 不会调度到
        ///     Godot 主循环；需要时请使用
        ///     <see cref="RitsuLibSidecarGodotMainLoopScheduling.ContinueOnGodotMainLoopAsync{T}(Task{T})" />。
        /// </remarks>
        public static Task<RitsuLibSidecarDispatchContext> WaitForNextAsync(
            ulong opcode,
            TimeSpan timeout,
            Func<RitsuLibSidecarDispatchContext, bool>? predicate = null,
            bool consumeOnMatch = true,
            CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<RitsuLibSidecarDispatchContext>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var waiter = new PendingWaiter
            {
                Opcode = opcode,
                ConsumeOnMatch = consumeOnMatch,
                Predicate = predicate,
                Tcs = tcs,
            };

            lock (Gate)
            {
                Waiters.Add(waiter);
            }

            if (timeout > TimeSpan.Zero)
                _ = Task.Delay(timeout, CancellationToken.None).ContinueWith(
                    _ => TryTimeoutWaiter(waiter),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);

            if (cancellationToken.CanBeCanceled)
                cancellationToken.Register(() => TryCancelWaiter(waiter, cancellationToken));

            return tcs.Task;
        }

        /// <summary>
        ///     When a waiter was registered but the matching send failed, removes it and completes the task with
        ///     <paramref name="exception" /> so the waiter list does not leak.
        ///     当等待者已注册但匹配的发送失败时，移除该等待者并用
        ///     <paramref name="exception" /> 完成任务，避免等待者列表泄漏。
        /// </summary>
        internal static bool TryFailWaitIfStillPending(Task<RitsuLibSidecarDispatchContext> waitTask,
            Exception exception)
        {
            PendingWaiter? found = null;
            lock (Gate)
            {
                for (var i = 0; i < Waiters.Count; i++)
                {
                    var w = Waiters[i];
                    if (!ReferenceEquals(w.Tcs.Task, waitTask))
                        continue;

                    Waiters.RemoveAt(i);
                    found = w;
                    break;
                }
            }

            return found?.Tcs.TrySetException(exception) ?? false;
        }

        private static void TryTimeoutWaiter(PendingWaiter waiter)
        {
            bool removed;
            lock (Gate)
            {
                removed = Waiters.Remove(waiter);
            }

            if (!removed)
                return;

            waiter.Tcs.TrySetException(new TimeoutException("Sidecar wait timed out"));
        }

        private static void TryCancelWaiter(PendingWaiter waiter, CancellationToken cancellationToken)
        {
            bool removed;
            lock (Gate)
            {
                removed = Waiters.Remove(waiter);
            }

            if (!removed)
                return;

            waiter.Tcs.TrySetCanceled(cancellationToken);
        }

        internal static void Dispatch(in RitsuLibSidecarDispatchContext context)
        {
            Action<RitsuLibSidecarDispatchContext>? handler;
            PendingWaiter? matchedWaiter = null;
            var consumeByWaiter = false;
            lock (Gate)
            {
                Handlers.TryGetValue(context.Opcode, out handler);
                for (var i = 0; i < Waiters.Count; i++)
                {
                    var w = Waiters[i];
                    if (w.Opcode != context.Opcode)
                        continue;

                    if (w.Predicate != null && !w.Predicate(context))
                        continue;

                    Waiters.RemoveAt(i);
                    matchedWaiter = w;
                    consumeByWaiter = w.ConsumeOnMatch;
                    break;
                }
            }

            matchedWaiter?.Tcs.TrySetResult(context);
            if (consumeByWaiter)
                return;

            handler?.Invoke(context);
        }

        private sealed class PendingWaiter
        {
            public required ulong Opcode { get; init; }
            public required bool ConsumeOnMatch { get; init; }
            public required Func<RitsuLibSidecarDispatchContext, bool>? Predicate { get; init; }
            public required TaskCompletionSource<RitsuLibSidecarDispatchContext> Tcs { get; init; }
        }
    }
}
