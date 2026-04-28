using System.Text.Json;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Event payload for one topic state change.
    /// </summary>
    public readonly record struct SidecarConfigTopicChangedEvent(
        string Topic,
        long Revision,
        ulong ChangedByPeer,
        string Reason,
        string StateJson);

    internal readonly record struct ConfigStateSnapshotMessage(string Topic, long Revision, string StateJson);

    internal readonly record struct ConfigChangeRequestMessage(
        string Topic,
        string RequestId,
        string DeltaJson,
        string Reason);

    internal readonly record struct ConfigChangeDecisionMessage(
        string Topic,
        string RequestId,
        bool Approved,
        string Reason,
        long Revision,
        string StateJson);

    /// <summary>
    ///     Host-authoritative sidecar config synchronization service.
    /// </summary>
    public static class RitsuLibSidecarConfigSyncService
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<string, TopicState> Topics = [];

        private static readonly RitsuLibSidecarMessageDescriptor<ConfigStateSnapshotMessage> SnapshotDescriptor = new(
            Const.ModId,
            "cfg_snapshot",
            m => JsonSerializer.SerializeToUtf8Bytes(m),
            payload => JsonSerializer.Deserialize<ConfigStateSnapshotMessage>(payload));

        private static readonly RitsuLibSidecarMessageDescriptor<ConfigChangeRequestMessage> RequestDescriptor = new(
            Const.ModId,
            "cfg_change_req",
            m => JsonSerializer.SerializeToUtf8Bytes(m),
            payload => JsonSerializer.Deserialize<ConfigChangeRequestMessage>(payload));

        private static readonly RitsuLibSidecarMessageDescriptor<ConfigChangeDecisionMessage> DecisionDescriptor = new(
            Const.ModId,
            "cfg_change_decision",
            m => JsonSerializer.SerializeToUtf8Bytes(m),
            payload => JsonSerializer.Deserialize<ConfigChangeDecisionMessage>(payload));

        private static int _bootstrapped;

        /// <summary>
        ///     Raised when a topic state is updated locally or from remote snapshot/decision.
        /// </summary>
        public static event Action<SidecarConfigTopicChangedEvent>? TopicChanged;

        /// <summary>
        ///     Registers a synchronized config topic with request policy and delta apply logic.
        /// </summary>
        public static void RegisterTopic<TState, TDelta>(
            string topic,
            TState initialState,
            Func<ulong, TDelta, bool> canClientRequest,
            Func<TState, TDelta, TState> applyDelta)
        {
            ArgumentException.ThrowIfNullOrEmpty(topic);
            ArgumentNullException.ThrowIfNull(canClientRequest);
            ArgumentNullException.ThrowIfNull(applyDelta);
            EnsureHandlers();
            lock (Gate)
            {
                Topics[topic] = new(
                    1,
                    JsonSerializer.Serialize(initialState),
                    (sender, deltaJson) =>
                    {
                        if (!TryDeserialize(deltaJson, out TDelta delta))
                            return false;
                        try
                        {
                            return canClientRequest(sender, delta);
                        }
                        catch (Exception ex)
                        {
                            RitsuLibFramework.Logger.Warn(
                                $"[Sidecar] Config canClientRequest failed topic={topic}, sender={sender}: {ex.Message}");
                            return false;
                        }
                    },
                    (stateJson, deltaJson) =>
                    {
                        if (!TryDeserialize(stateJson, out TState state) ||
                            !TryDeserialize(deltaJson, out TDelta delta))
                            return stateJson;
                        try
                        {
                            return JsonSerializer.Serialize(applyDelta(state, delta));
                        }
                        catch (Exception ex)
                        {
                            RitsuLibFramework.Logger.Warn(
                                $"[Sidecar] Config applyDelta failed topic={topic}: {ex.Message}");
                            return stateJson;
                        }
                    });
            }
        }

        /// <summary>
        ///     Sends a client-side config change request using a direct net service reference.
        /// </summary>
        public static bool TryRequestClientChange<TDelta>(INetGameService? netService, string topic, TDelta delta,
            string reason = "")
        {
            EnsureHandlers();
            return RitsuLibSidecarTypedMessageRegistry.SendToHost(
                netService,
                RequestDescriptor,
                new(topic, Guid.NewGuid().ToString("N"), JsonSerializer.Serialize(delta), reason));
        }

        /// <summary>
        ///     Sends a client-side config change request using <see cref="RunManager" />.
        /// </summary>
        public static bool TryRequestClientChange<TDelta>(RunManager? runManager, string topic, TDelta delta,
            string reason = "")
        {
            EnsureHandlers();
            return RitsuLibSidecarTypedMessageRegistry.SendToHost(
                runManager,
                RequestDescriptor,
                new(topic, Guid.NewGuid().ToString("N"), JsonSerializer.Serialize(delta), reason));
        }

        /// <summary>
        ///     Reads cached topic state and revision.
        /// </summary>
        public static bool TryGetTopicState<TState>(string topic, out TState? state, out long revision)
        {
            lock (Gate)
            {
                if (!Topics.TryGetValue(topic, out var t))
                {
                    state = default;
                    revision = 0;
                    return false;
                }

                state = JsonSerializer.Deserialize<TState>(t.StateJson);
                revision = t.Revision;
                return true;
            }
        }

        /// <summary>
        ///     Host publishes current topic snapshot and raises the local topic-change event.
        /// </summary>
        public static void PublishHostState(INetGameService? netService, string topic, ulong changedBy, string reason)
        {
            TopicState state;
            lock (Gate)
            {
                if (!Topics.TryGetValue(topic, out state))
                    return;
            }

            TopicChanged?.Invoke(new(topic, state.Revision, changedBy, reason, state.StateJson));

            RitsuLibSidecarTypedMessageRegistry.Broadcast(
                netService,
                SnapshotDescriptor,
                new(topic, state.Revision, state.StateJson));
        }

        private static void EnsureHandlers()
        {
            if (Interlocked.CompareExchange(ref _bootstrapped, 1, 0) != 0)
                return;

            RitsuLibSidecarTypedMessageRegistry.Subscribe(RequestDescriptor, OnRequestMessage);
            RitsuLibSidecarTypedMessageRegistry.Subscribe(SnapshotDescriptor, OnSnapshotMessage);
            RitsuLibSidecarTypedMessageRegistry.Subscribe(DecisionDescriptor, OnDecisionMessage);
        }

        private static void OnRequestMessage(RitsuLibSidecarTypedDispatchContext<ConfigChangeRequestMessage> ctx)
        {
            var rm = RunManager.Instance;
            var netService = rm?.NetService;
            if (netService is not NetHostGameService)
                return;

            bool approved;
            string reason;
            TopicState? next;
            lock (Gate)
            {
                if (!Topics.TryGetValue(ctx.Message.Topic, out var topic))
                {
                    approved = false;
                    reason = "topic_not_found";
                    next = null;
                }
                else if (!topic.CanClientRequest(ctx.SenderNetId, ctx.Message.DeltaJson))
                {
                    approved = false;
                    reason = "client_request_rejected";
                    next = topic;
                }
                else
                {
                    approved = true;
                    reason = string.IsNullOrWhiteSpace(ctx.Message.Reason) ? "applied" : ctx.Message.Reason;
                    var nextState = topic.ApplyDelta(topic.StateJson, ctx.Message.DeltaJson);
                    next = topic with { Revision = topic.Revision + 1, StateJson = nextState };
                    Topics[ctx.Message.Topic] = next.Value;
                }
            }

            if (next == null)
                return;

            RitsuLibSidecarTypedMessageRegistry.SendToPeer(
                netService,
                ctx.SenderNetId,
                DecisionDescriptor,
                new(
                    ctx.Message.Topic,
                    ctx.Message.RequestId,
                    approved,
                    reason,
                    next.Value.Revision,
                    next.Value.StateJson));
            if (!approved)
                return;

            PublishHostState(netService, ctx.Message.Topic, ctx.SenderNetId, reason);
        }

        private static void OnSnapshotMessage(RitsuLibSidecarTypedDispatchContext<ConfigStateSnapshotMessage> ctx)
        {
            lock (Gate)
            {
                if (Topics.TryGetValue(ctx.Message.Topic, out var current) && current.Revision > ctx.Message.Revision)
                    return;

                Topics[ctx.Message.Topic] = new(
                    ctx.Message.Revision,
                    ctx.Message.StateJson,
                    (_, _) => false,
                    (state, _) => state);
            }

            TopicChanged?.Invoke(
                new(
                    ctx.Message.Topic,
                    ctx.Message.Revision,
                    ctx.SenderNetId,
                    "snapshot",
                    ctx.Message.StateJson));
        }

        private static void OnDecisionMessage(RitsuLibSidecarTypedDispatchContext<ConfigChangeDecisionMessage> ctx)
        {
            if (!ctx.Message.Approved)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Sidecar] Config change rejected topic={ctx.Message.Topic}, request={ctx.Message.RequestId}, reason={ctx.Message.Reason}");
                return;
            }

            lock (Gate)
            {
                Topics[ctx.Message.Topic] = new(
                    ctx.Message.Revision,
                    ctx.Message.StateJson,
                    (_, _) => false,
                    (state, _) => state);
            }

            TopicChanged?.Invoke(
                new(
                    ctx.Message.Topic,
                    ctx.Message.Revision,
                    ctx.SenderNetId,
                    ctx.Message.Reason,
                    ctx.Message.StateJson));
        }

        private static bool TryDeserialize<T>(string json, out T value)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<T>(json);
                if (parsed is null)
                {
                    value = default!;
                    return false;
                }

                value = parsed;
                return true;
            }
            catch
            {
                value = default!;
                return false;
            }
        }

        private readonly record struct TopicState(
            long Revision,
            string StateJson,
            Func<ulong, string, bool> CanClientRequest,
            Func<string, string, string> ApplyDelta);
    }
}
