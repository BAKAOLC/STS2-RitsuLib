namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Dispatches Sidecar payloads and one-shot waiters by 64-bit opcode.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         按 64 位操作码分发 Sidecar 载荷和一次性等待器。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarBus
    {
        private static readonly Lock Gate = new();

        private static readonly Dictionary<ulong, Action<RitsuLibSidecarDispatchContext>> Handlers = [];
        private static readonly List<PendingWaiter> Waiters = [];
        private static readonly TimeSpan MaximumSupportedWaitTimeout = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers or replaces the handler for <paramref name="opcode" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册或替换 <paramref name="opcode" /> 对应的处理器。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Removes the handler for <paramref name="opcode" />, if present.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         移除 <paramref name="opcode" /> 对应的处理器（如存在）。
        ///     </para>
        /// </summary>
        public static void UnregisterHandler(ulong opcode)
        {
            lock (Gate)
            {
                Handlers.Remove(opcode);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Removes all registered opcode handlers.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         移除所有已注册的操作码处理器。
        ///     </para>
        /// </summary>
        public static void ClearHandlers()
        {
            lock (Gate)
            {
                Handlers.Clear();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets a snapshot of the number of active <see cref="WaitForNextAsync" /> waiters.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取活动 <see cref="WaitForNextAsync" /> 等待器数量的快照。
        ///     </para>
        /// </summary>
        public static int GetPendingWaiterCount()
        {
            lock (Gate)
            {
                return Waiters.Count;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Removes every pending <see cref="WaitForNextAsync" /> waiter and completes its task as canceled.
        ///         Registered opcode handlers are retained.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         移除所有待处理的 <see cref="WaitForNextAsync" /> 等待器，并将其任务完成为已取消。
        ///         已注册的操作码处理器会保留。
        ///     </para>
        /// </summary>
        public static void CancelAllPendingWaits()
        {
            List<PendingWaiter> pending;
            lock (Gate)
            {
                pending = [.. Waiters];
                Waiters.Clear();
            }

            foreach (var w in pending)
                w.Tcs.TrySetCanceled();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Waits for the next packet matching an opcode and optional predicate.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         等待下一个与操作码及可选谓词匹配的数据包。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         A timeout may be <c>Timeout.InfiniteTimeSpan</c> or from <c>TimeSpan.Zero</c> through
        ///         the maximum supported <see cref="Task.Delay(TimeSpan, CancellationToken)" /> value; zero and infinite
        ///         timeouts do not schedule a timeout. Caller cancellation is tracked separately. Task continuations are
        ///         not marshaled to the Godot main loop; use
        ///         <see cref="RitsuLibSidecarGodotMainLoopScheduling.ContinueOnGodotMainLoopAsync{T}(Task{T})" /> when
        ///         main-loop affinity is required. The predicate executes synchronously outside the bus lock against an
        ///         opcode-matching waiter snapshot. If a candidate completes or is canceled concurrently, dispatch
        ///         continues to the next still-pending matching candidate.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         超时可以是 <c>Timeout.InfiniteTimeSpan</c>，或从 <c>TimeSpan.Zero</c> 到
        ///         <see cref="Task.Delay(TimeSpan, CancellationToken)" /> 支持的最大值；零超时和无限超时均不会安排超时。
        ///         调用方取消会单独处理。任务的延续不会自动切换到 Godot 主循环；需要主循环线程关联时，请使用
        ///         <see cref="RitsuLibSidecarGodotMainLoopScheduling.ContinueOnGodotMainLoopAsync{T}(Task{T})" />。
        ///         谓词会针对操作码匹配等待器的快照在总线锁外同步执行。若候选等待器在并发情况下已完成或被取消，
        ///         分发会继续检查下一个仍待处理的匹配候选。
        ///     </para>
        /// </remarks>
        public static Task<RitsuLibSidecarDispatchContext> WaitForNextAsync(
            ulong opcode,
            TimeSpan timeout,
            Func<RitsuLibSidecarDispatchContext, bool>? predicate = null,
            bool consumeOnMatch = true,
            CancellationToken cancellationToken = default)
        {
            ValidateTimeout(timeout);

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
        ///     <para xml:lang="en">
        ///         Removes a still-pending waiter and faults it with <paramref name="exception" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         移除仍在等待的等待器，并使用 <paramref name="exception" /> 将其任务置为失败。
        ///     </para>
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
            var dispatchContext = context;
            Action<RitsuLibSidecarDispatchContext>? handler;
            PendingWaiter[] candidates;
            PendingWaiter? matchedWaiter = null;
            var consumeByWaiter = false;
            lock (Gate)
            {
                Handlers.TryGetValue(dispatchContext.Opcode, out handler);
                candidates = [.. Waiters.Where(w => w.Opcode == dispatchContext.Opcode)];
            }

            foreach (var candidate in candidates)
            {
                if (candidate.Predicate != null && !candidate.Predicate(dispatchContext))
                    continue;

                lock (Gate)
                {
                    if (!Waiters.Remove(candidate))
                        continue;

                    matchedWaiter = candidate;
                    consumeByWaiter = candidate.ConsumeOnMatch;
                    break;
                }
            }

            matchedWaiter?.Tcs.TrySetResult(dispatchContext);
            if (consumeByWaiter)
                return;

            handler?.Invoke(dispatchContext);
        }

        private static void ValidateTimeout(TimeSpan timeout)
        {
            if ((timeout == Timeout.InfiniteTimeSpan || timeout >= TimeSpan.Zero) &&
                timeout <= MaximumSupportedWaitTimeout)
                return;

            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                timeout,
                $"Timeout must be {Timeout.InfiniteTimeSpan} or between {TimeSpan.Zero} and {MaximumSupportedWaitTimeout}.");
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
