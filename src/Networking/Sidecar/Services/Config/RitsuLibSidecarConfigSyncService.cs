using System.Text.Json;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">Carries the state and metadata for one synchronized configuration-topic change.</para>
    ///     <para xml:lang="zh-CN">携带一次同步配置主题变更的状态和元数据。</para>
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
    ///     <para xml:lang="en">
    ///         Provides host-authoritative Sidecar configuration synchronization. Topic-state updates are committed
    ///         before <see cref="TopicChanged" /> runs; its subscribers execute synchronously.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供主机权威的 Sidecar 配置同步。主题状态更新会在运行 <see cref="TopicChanged" /> 前提交；其订阅者同步执行。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarConfigSyncService
    {
        private static readonly Lock Gate = new();
        private static readonly Lock HandlerGate = new();
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

        private static IDisposable? _handlerSubscriptions;

        /// <summary>
        ///     <para xml:lang="en">Raised after a topic state is updated locally or from a remote snapshot or decision.</para>
        ///     <para xml:lang="zh-CN">在主题状态从本地或远程快照、决策更新后引发。</para>
        /// </summary>
        public static event Action<SidecarConfigTopicChangedEvent>? TopicChanged;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers or replaces a synchronized configuration topic with its initial state, client-request
        ///         policy, and delta-application logic. Policy and delta callbacks run under the topic lock when a
        ///         host processes a request.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用初始状态、客户端请求策略和增量应用逻辑注册或替换同步配置主题。主机处理请求时，策略和增量回调在主题锁内运行。
        ///     </para>
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
                            RitsuLibSidecarRepeatedWarningLog.Warn(
                                $"config-can-client-request:topic={topic}:sender={sender}:{ex.GetType().FullName}:{ex.Message}",
                                $"[Sidecar] Config canClientRequest failed topic={topic}, sender={sender}: {ex}");
                            return false;
                        }
                    },
                    (stateJson, deltaJson) =>
                    {
                        if (!TryDeserialize(stateJson, out TState state) ||
                            !TryDeserialize(deltaJson, out TDelta delta))
                            return new(false, stateJson);
                        try
                        {
                            return new(true, JsonSerializer.Serialize(applyDelta(state, delta)));
                        }
                        catch (Exception ex)
                        {
                            RitsuLibSidecarRepeatedWarningLog.Warn(
                                $"config-apply-delta:topic={topic}:{ex.GetType().FullName}:{ex.Message}",
                                $"[Sidecar] Config applyDelta failed topic={topic}: {ex}");
                            return new(false, stateJson);
                        }
                    });
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sends a client-side configuration-change request through a direct network-service reference. A
        ///         successful result reports only local send acceptance, not host approval or state persistence.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过直接网络服务引用发送客户端配置变更请求。成功结果仅表示本地接受发送，不表示主机已批准或状态已持久化。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Sends a client-side configuration-change request through <see cref="RunManager" />. A successful
        ///         result reports only local send acceptance, not host approval or state persistence.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过 <see cref="RunManager" /> 发送客户端配置变更请求。成功结果仅表示本地接受发送，不表示主机已批准或状态已持久化。
        ///     </para>
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
        ///     <para xml:lang="en">Tries to deserialize and read the cached state and revision for a topic.</para>
        ///     <para xml:lang="zh-CN">尝试反序列化并读取主题的缓存状态和修订号。</para>
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
        ///     <para xml:lang="en">
        ///         Raises the local topic-change event and broadcasts the current host snapshot when the topic exists.
        ///         It does not return the broadcast outcome.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         主题存在时引发本地主题变更事件并广播当前主机快照；该方法不返回广播结果。
        ///     </para>
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
            if (_handlerSubscriptions != null)
                return;

            lock (HandlerGate)
            {
                if (_handlerSubscriptions != null)
                    return;

                IDisposable? request = null;
                IDisposable? snapshot = null;
                IDisposable? decision = null;
                try
                {
                    request = RitsuLibSidecarTypedMessageRegistry.Subscribe(RequestDescriptor, OnRequestMessage);
                    snapshot = RitsuLibSidecarTypedMessageRegistry.Subscribe(SnapshotDescriptor, OnSnapshotMessage);
                    decision = RitsuLibSidecarTypedMessageRegistry.Subscribe(DecisionDescriptor, OnDecisionMessage);
                    _handlerSubscriptions = new HandlerSubscriptionGroup(request, snapshot, decision);
                }
                catch
                {
                    decision?.Dispose();
                    snapshot?.Dispose();
                    request?.Dispose();
                    throw;
                }
            }
        }

        private static void OnRequestMessage(RitsuLibSidecarTypedDispatchContext<ConfigChangeRequestMessage> ctx)
        {
            var rm = RunManager.Instance;
            var netService = rm?.NetService;
            if (netService is not NetHostGameService)
                return;

            bool approved;
            string reason;
            long revision;
            string stateJson;
            lock (Gate)
            {
                if (!Topics.TryGetValue(ctx.Message.Topic, out var topic))
                {
                    approved = false;
                    reason = "topic_not_found";
                    revision = 0;
                    stateJson = string.Empty;
                }
                else if (!topic.CanClientRequest(ctx.SenderNetId, ctx.Message.DeltaJson))
                {
                    approved = false;
                    reason = "client_request_rejected";
                    revision = topic.Revision;
                    stateJson = topic.StateJson;
                }
                else
                {
                    var applied = topic.ApplyDelta(topic.StateJson, ctx.Message.DeltaJson);
                    approved = applied.Succeeded;
                    reason = approved
                        ? string.IsNullOrWhiteSpace(ctx.Message.Reason) ? "applied" : ctx.Message.Reason
                        : "apply_delta_failed";
                    revision = approved ? topic.Revision + 1 : topic.Revision;
                    stateJson = approved ? applied.StateJson : topic.StateJson;
                    if (approved)
                        Topics[ctx.Message.Topic] = topic with { Revision = revision, StateJson = stateJson };
                }
            }

            RitsuLibSidecarTypedMessageRegistry.SendToPeer(
                netService,
                ctx.SenderNetId,
                DecisionDescriptor,
                new(
                    ctx.Message.Topic,
                    ctx.Message.RequestId,
                    approved,
                    reason,
                    revision,
                    stateJson));
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
                    (state, _) => new(true, state));
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
                RitsuLibSidecarRepeatedWarningLog.Warn(
                    $"config-rejected:topic={ctx.Message.Topic}:reason={ctx.Message.Reason}",
                    $"[Sidecar] Config change rejected topic={ctx.Message.Topic}, request={ctx.Message.RequestId}, reason={ctx.Message.Reason}");
                return;
            }

            lock (Gate)
            {
                Topics[ctx.Message.Topic] = new(
                    ctx.Message.Revision,
                    ctx.Message.StateJson,
                    (_, _) => false,
                    (state, _) => new(true, state));
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
            Func<string, string, DeltaApplyResult> ApplyDelta);

        private readonly record struct DeltaApplyResult(bool Succeeded, string StateJson);

        private sealed class HandlerSubscriptionGroup(
            IDisposable request,
            IDisposable snapshot,
            IDisposable decision) : IDisposable
        {
            public void Dispose()
            {
                decision.Dispose();
                snapshot.Dispose();
                request.Dispose();
            }
        }
    }
}
