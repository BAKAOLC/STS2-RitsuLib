namespace STS2RitsuLib.Networking.Sidecar
{
    internal sealed class RitsuLibSidecarEndpointRegistration
    {
        private readonly Lock _dispatchGate = new();
        private readonly Queue<BackgroundAction> _backgroundActions = [];
        private readonly Action<RitsuLibSidecarEndpointMessage>? _handler;
        private readonly RitsuLibSidecarTokenBucket _outboundRateLimit;

        private Action<RitsuLibSidecarEndpointParticipantsChangedEvent>? _participantsChanged;
        private RitsuLibSidecarEndpointRouteDefinition? _route;
        private int _backgroundDispatchBytes;
        private bool _backgroundDispatchRunning;
        private int _mainLoopDispatchBytes;
        private int _mainLoopDispatchMessages;
        private int _disposed;
        private int _disposeStarted;
        private int _sequence;

        internal RitsuLibSidecarEndpointRegistration(
            RitsuLibSidecarEndpointDescriptor descriptor,
            Action<RitsuLibSidecarEndpointMessage> handler)
        {
            Descriptor = descriptor;
            _handler = handler;
            _outboundRateLimit = new(
                descriptor.MaxOutboundPacketsPerSecond,
                descriptor.MaxOutboundBytesPerSecond);
            Handle = new(this);
        }

        internal RitsuLibSidecarEndpointRegistration(
            RitsuLibSidecarEndpointDescriptor descriptor,
            RitsuLibSidecarBulkStreamOptions options,
            Func<RitsuLibSidecarBulkStreamOffer, RitsuLibSidecarBulkReceiveTarget?> handler)
        {
            Descriptor = descriptor;
            BulkOptions = options;
            _outboundRateLimit = new(
                descriptor.MaxOutboundPacketsPerSecond,
                descriptor.MaxOutboundBytesPerSecond);
            BulkTransfers = new(this, handler);
            BulkHandle = new(this);
        }

        internal RitsuLibSidecarEndpointDescriptor Descriptor { get; }
        internal RitsuLibSidecarEndpointHandle? Handle { get; }
        internal RitsuLibSidecarBulkEndpointHandle? BulkHandle { get; }
        internal RitsuLibSidecarBulkStreamOptions? BulkOptions { get; }
        internal RitsuLibSidecarBulkTransferManager? BulkTransfers { get; }
        internal bool IsDisposed => Volatile.Read(ref _disposed) != 0;

        internal event Action<RitsuLibSidecarEndpointParticipantsChangedEvent> ParticipantsChanged
        {
            add
            {
                ArgumentNullException.ThrowIfNull(value);
                lock (_dispatchGate)
                {
                    _participantsChanged += value;
                }
            }
            remove
            {
                lock (_dispatchGate)
                {
                    _participantsChanged -= value;
                }
            }
        }

        internal RitsuLibSidecarEndpointRouteDefinition? GetRoute()
        {
            lock (_dispatchGate)
            {
                return _route;
            }
        }

        internal uint NextSequence()
        {
            return unchecked((uint)Interlocked.Increment(ref _sequence));
        }

        internal bool TryConsumeOutboundRate(int logicalPayloadBytes)
        {
            return _outboundRateLimit.TryConsume(logicalPayloadBytes);
        }

        internal void ApplyRoute(RitsuLibSidecarEndpointRouteDefinition? route)
        {
            RitsuLibSidecarEndpointParticipantsChangedEvent changed;
            Action<RitsuLibSidecarEndpointParticipantsChangedEvent>? handlers;
            lock (_dispatchGate)
            {
                if (RoutesEquivalent(_route, route))
                    return;
                _route = route;
                handlers = _participantsChanged;
                changed = route is { } current
                    ? new(
                        current.ProtocolVersion,
                        Array.AsReadOnly([.. current.ParticipantNetIds]))
                    : new(0, []);
            }

            BulkTransfers?.RouteChanged();
            if (handlers == null)
                return;
            TryScheduleCallback(
                () =>
                {
                    foreach (var handler in handlers.GetInvocationList()
                                 .Cast<Action<RitsuLibSidecarEndpointParticipantsChangedEvent>>())
                        InvokeSafely(() => handler(changed), "participant-change");
                },
                0);
        }

        internal void Dispatch(RitsuLibSidecarEndpointMessage message)
        {
            if (BulkTransfers != null)
            {
                BulkTransfers.HandleMessage(message);
                return;
            }

            TryScheduleCallback(
                () => InvokeSafely(() => _handler!(message), "message"),
                message.Payload.Length);
        }

        internal void Dispose()
        {
            if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
                return;

            BulkTransfers?.Dispose();
            Volatile.Write(ref _disposed, 1);
            RitsuLibSidecarOutboundScheduler.RemoveEndpoint(this);
            RitsuLibSidecarEndpointRegistry.Unregister(this);
            lock (_dispatchGate)
            {
                _route = null;
                _participantsChanged = null;
                _backgroundActions.Clear();
                _backgroundDispatchBytes = 0;
                _mainLoopDispatchBytes = 0;
                _mainLoopDispatchMessages = 0;
            }
        }

        internal bool TryScheduleCallback(Action action, int payloadBytes)
        {
            if (Volatile.Read(ref _disposeStarted) != 0)
                return false;

            switch (Descriptor.DispatchMode)
            {
                case RitsuLibSidecarEndpointDispatchMode.ReceiveThread:
                    action();
                    return true;
                case RitsuLibSidecarEndpointDispatchMode.GodotMainLoop:
                    if (!TryReserveMainLoopDispatch(payloadBytes))
                    {
                        WarnDispatchQueueFull("main-loop");
                        return false;
                    }

                    if (!RitsuLibSidecarGodotMainLoopScheduling.TryPostToMainLoop(() =>
                        {
                            try
                            {
                                if (!IsDisposed)
                                    action();
                            }
                            finally
                            {
                                ReleaseMainLoopDispatch(payloadBytes);
                            }
                        }))
                    {
                        ReleaseMainLoopDispatch(payloadBytes);
                        RitsuLibSidecarRepeatedWarningLog.Warn(
                            $"endpoint-main-loop-unavailable:{Descriptor.OwnerId}/{Descriptor.Name}",
                            $"[SidecarEndpoints] Main loop unavailable; dropped callback for {Descriptor.OwnerId}/{Descriptor.Name}.");
                        return false;
                    }

                    return true;
                case RitsuLibSidecarEndpointDispatchMode.BackgroundSerial:
                    return EnqueueBackground(action, payloadBytes);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool EnqueueBackground(Action action, int payloadBytes)
        {
            lock (_dispatchGate)
            {
                if (Volatile.Read(ref _disposeStarted) != 0)
                    return false;
                if (_backgroundActions.Count >= RitsuLibSidecarEndpointPolicy.MaxDispatchQueueMessages ||
                    _backgroundDispatchBytes + payloadBytes > RitsuLibSidecarEndpointPolicy.MaxDispatchQueueBytes)
                {
                    WarnDispatchQueueFull("background");
                    return false;
                }

                _backgroundDispatchBytes += payloadBytes;
                _backgroundActions.Enqueue(new(action, payloadBytes));
                if (_backgroundDispatchRunning)
                    return true;
                _backgroundDispatchRunning = true;
            }

            _ = Task.Run(ProcessBackgroundQueue);
            return true;
        }

        private bool TryReserveMainLoopDispatch(int payloadBytes)
        {
            lock (_dispatchGate)
            {
                if (IsDisposed ||
                    _mainLoopDispatchMessages >= RitsuLibSidecarEndpointPolicy.MaxDispatchQueueMessages ||
                    _mainLoopDispatchBytes + payloadBytes > RitsuLibSidecarEndpointPolicy.MaxDispatchQueueBytes)
                    return false;
                _mainLoopDispatchMessages++;
                _mainLoopDispatchBytes += payloadBytes;
                return true;
            }
        }

        private void ReleaseMainLoopDispatch(int payloadBytes)
        {
            lock (_dispatchGate)
            {
                _mainLoopDispatchMessages = Math.Max(0, _mainLoopDispatchMessages - 1);
                _mainLoopDispatchBytes = Math.Max(0, _mainLoopDispatchBytes - payloadBytes);
            }
        }

        private void WarnDispatchQueueFull(string queueName)
        {
            RitsuLibSidecarRepeatedWarningLog.Warn(
                $"endpoint-{queueName}-queue-full:{Descriptor.OwnerId}/{Descriptor.Name}",
                $"[SidecarEndpoints] {queueName} callback queue full; dropped callback for {Descriptor.OwnerId}/{Descriptor.Name}.");
        }

        private void ProcessBackgroundQueue()
        {
            while (true)
            {
                BackgroundAction item;
                lock (_dispatchGate)
                {
                    if (IsDisposed || _backgroundActions.Count == 0)
                    {
                        _backgroundActions.Clear();
                        _backgroundDispatchBytes = 0;
                        _backgroundDispatchRunning = false;
                        return;
                    }

                    item = _backgroundActions.Dequeue();
                    _backgroundDispatchBytes -= item.PayloadBytes;
                }

                item.Action();
            }
        }

        private void InvokeSafely(Action action, string operation)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[SidecarEndpoints] Endpoint {operation} callback failed for {Descriptor.OwnerId}/{Descriptor.Name}: {exception}");
            }
        }

        private static bool RoutesEquivalent(
            RitsuLibSidecarEndpointRouteDefinition? left,
            RitsuLibSidecarEndpointRouteDefinition? right)
        {
            if (left is null || right is null)
                return left is null && right is null;
            var a = left.Value;
            var b = right.Value;
            return a.RouteId == b.RouteId &&
                   a.Nonce == b.Nonce &&
                   a.ProtocolVersion == b.ProtocolVersion &&
                   a.MaxPayloadBytes == b.MaxPayloadBytes &&
                   a.ParticipantNetIds.SequenceEqual(b.ParticipantNetIds);
        }

        private readonly record struct BackgroundAction(Action Action, int PayloadBytes);
    }
}
