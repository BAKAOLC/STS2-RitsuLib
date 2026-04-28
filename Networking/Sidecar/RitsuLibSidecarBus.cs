namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Opcode dispatch for sidecar payloads. Registration uses the same 64-bit opcodes as
    ///     <see cref="RitsuLibSidecarOpcodes.For" />.
    /// </summary>
    public static class RitsuLibSidecarBus
    {
        private static readonly Lock Gate = new();

        private static readonly Dictionary<ulong, Action<RitsuLibSidecarDispatchContext>> Handlers = [];
        private static readonly List<PendingWaiter> Waiters = [];

        /// <summary>
        ///     Registers or replaces a handler for an opcode. Unregister when leaving multiplayer to avoid leaks.
        /// </summary>
        public static void RegisterHandler(ulong opcode, Action<RitsuLibSidecarDispatchContext> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            lock (Gate)
            {
                Handlers[opcode] = handler;
            }
        }

        /// <summary>Removes a handler for the opcode, if present.</summary>
        public static void UnregisterHandler(ulong opcode)
        {
            lock (Gate)
            {
                Handlers.Remove(opcode);
            }
        }

        /// <summary>Removes all opcode handlers (e.g. when leaving multiplayer).</summary>
        public static void ClearHandlers()
        {
            lock (Gate)
            {
                Handlers.Clear();
            }
        }

        /// <summary>
        ///     Waits once for a matching opcode packet, useful for request/reply control flows.
        /// </summary>
        /// <remarks>
        ///     Timeout uses <see cref="CancellationToken.None" /> on <see cref="Task.Delay(TimeSpan, CancellationToken)" />;
        ///     user cancellation is observed through <paramref name="cancellationToken" /> separately so both paths
        ///     can complete the waiter without linking tokens.
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
