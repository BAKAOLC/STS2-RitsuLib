using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Data;
using STS2RitsuLib.Networking.ManagedActions;
using STS2RitsuLib.Networking.Sidecar;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Toast;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal readonly record struct RitsuDebugActionEnvelope(
        int ProtocolVersion,
        string ActionId,
        ulong RequestedByNetId,
        ulong TargetPlayerNetId,
        string PayloadJson);

    internal readonly record struct RitsuDebugActionRequestMessage(
        string RequestId,
        RitsuDebugActionEnvelope Action);

    internal readonly record struct RitsuDebugActionDecisionMessage(
        string RequestId,
        bool Accepted,
        RitsuDebugActionFeedback Feedback);

    internal readonly record struct RitsuDebugActionContext(
        Player Requester,
        Player Target);

    internal readonly record struct RitsuDebugActionCheck(bool Success, RitsuDebugActionFeedback Feedback)
    {
        internal static RitsuDebugActionCheck Ok => new(true, default);

        internal static RitsuDebugActionCheck Fail(
            string code,
            string fallback,
            params object?[] arguments)
        {
            return new(false, RitsuDebugActionFeedback.Create(code, fallback, arguments));
        }

        internal static RitsuDebugActionCheck Fail(RitsuDebugActionFeedback feedback)
        {
            return new(false, feedback);
        }
    }

    internal readonly record struct RitsuDebugActionSubmission(
        bool Accepted,
        RitsuDebugActionFeedback Feedback)
    {
        internal static RitsuDebugActionSubmission Success => new(true, default);

        internal static RitsuDebugActionSubmission PendingApproval(
            string code,
            string fallback,
            params object?[] arguments)
        {
            return new(true, RitsuDebugActionFeedback.Create(code, fallback, arguments));
        }

        internal static RitsuDebugActionSubmission Reject(
            string code,
            string fallback,
            params object?[] arguments)
        {
            return new(false, RitsuDebugActionFeedback.Create(code, fallback, arguments));
        }

        internal static RitsuDebugActionSubmission Reject(RitsuDebugActionFeedback feedback)
        {
            return new(false, feedback);
        }

        internal string Message => Accepted
            ? Feedback.IsValid()
                ? Feedback.GetLocalizedText()
                : ModSettingsLocalization.Get(
                    "ritsulib.debugTools.requestAccepted",
                    "The requested change was accepted.")
            : Feedback.GetLocalizedText();
    }

    internal readonly record struct RitsuDebugActionExecutionResult(
        string ActionId,
        ulong RequestedByNetId,
        ulong TargetPlayerNetId,
        bool Success,
        RitsuDebugActionFeedback Feedback)
    {
        internal string Message => Feedback.IsValid() ? Feedback.GetLocalizedText() : string.Empty;
    }

    internal sealed class RitsuDebugActionExecutionException : InvalidOperationException
    {
        internal RitsuDebugActionExecutionException(RitsuDebugActionFeedback feedback)
            : base(feedback.GetEnglishText())
        {
            Feedback = feedback;
        }

        internal RitsuDebugActionFeedback Feedback { get; }
    }

    internal static class RitsuDebugActionProtocol
    {
        internal const int CurrentProtocolVersion = 1;

        private const int MaxActionIdLength = 64;
        internal const int MaxActionPayloadCharacters = 8 * 1024;
        private const int MaxPendingClientRequests = 32;
        private const int MaxRecentHostClientRequests = 256;
        private const int MaxClientRequestsPerWindow = 16;
        private const string ProtocolModuleId = "STS2RitsuLib.DebugTools";
        private const string ManagedActionKey = "developer-action-v1";
        private const string RequestMessageKey = "developer-action-request-v1";
        private const string DecisionMessageKey = "developer-action-decision-v1";

        private static readonly Lock Gate = new();

        private static readonly Dictionary<string, RegistrationBase> Registrations =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, PendingClientRequest> PendingClientRequests =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<HostClientRequestKey, DateTimeOffset> RecentHostClientRequests = [];

        private static readonly RitsuLibManagedNetActionDescriptor<RitsuDebugActionEnvelope> ActionDescriptor = new(
            ProtocolModuleId,
            ManagedActionKey,
            static message => JsonSerializer.SerializeToUtf8Bytes(message),
            Deserialize<RitsuDebugActionEnvelope>,
            ExecuteManagedActionAsync,
            GameActionType.Any);

        private static readonly RitsuLibSidecarMessageDescriptor<RitsuDebugActionRequestMessage> RequestDescriptor =
            new(
                ProtocolModuleId,
                RequestMessageKey,
                static message => JsonSerializer.SerializeToUtf8Bytes(message),
                Deserialize<RitsuDebugActionRequestMessage>);

        private static readonly RitsuLibSidecarMessageDescriptor<RitsuDebugActionDecisionMessage> DecisionDescriptor =
            new(
                ProtocolModuleId,
                DecisionMessageKey,
                static message => JsonSerializer.SerializeToUtf8Bytes(message),
                Deserialize<RitsuDebugActionDecisionMessage>);

        private static IDisposable? _requestSubscription;
        private static IDisposable? _decisionSubscription;

        internal static event Action<RitsuDebugActionExecutionResult>? ActionExecuted;

        internal static void EnsureHandlersRegistered()
        {
            if (_requestSubscription != null && _decisionSubscription != null)
                return;

            RitsuDebugCardActions.RegisterBuiltInActions();
            RitsuDebugPlayerActions.RegisterBuiltInActions();
            RitsuDebugInventoryActions.RegisterBuiltInActions();
            RitsuDebugCombatActions.RegisterBuiltInActions();
            RitsuDebugRunActions.RegisterBuiltInActions();
            RitsuDebugStatePresetActions.RegisterBuiltInActions();
            lock (Gate)
            {
                if (_requestSubscription != null && _decisionSubscription != null)
                    return;

                RitsuLibManagedNetActions.Register(ActionDescriptor);

                IDisposable? request = null;
                IDisposable? decision = null;
                try
                {
                    request = RitsuLibSidecarTypedMessageRegistry.Subscribe(RequestDescriptor, OnClientRequest);
                    decision = RitsuLibSidecarTypedMessageRegistry.Subscribe(DecisionDescriptor, OnHostDecision);
                    _requestSubscription = request;
                    _decisionSubscription = decision;
                }
                catch
                {
                    decision?.Dispose();
                    request?.Dispose();
                    throw;
                }
            }
        }

        internal static void Register<TPayload>(
            string actionId,
            Func<RitsuDebugActionContext, TPayload, RitsuDebugActionCheck> validate,
            Func<RitsuDebugActionContext, TPayload, Task<string>> execute)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
            ArgumentNullException.ThrowIfNull(validate);
            ArgumentNullException.ThrowIfNull(execute);
            if (actionId.Length > MaxActionIdLength || actionId.Any(char.IsWhiteSpace))
                throw new ArgumentException("Debug action IDs must be compact non-whitespace identifiers.",
                    nameof(actionId));

            lock (Gate)
            {
                if (Registrations.TryGetValue(actionId, out var existing))
                {
                    if (existing.PayloadType == typeof(TPayload))
                        return;

                    throw new InvalidOperationException(
                        $"Debug action '{actionId}' is already registered with payload type {existing.PayloadType}.");
                }

                Registrations[actionId] = new Registration<TPayload>(actionId, validate, execute);
            }
        }

        internal static RitsuDebugActionEnvelope CreateEnvelope<TPayload>(
            string actionId,
            Player requester,
            Player target,
            TPayload payload)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
            ArgumentNullException.ThrowIfNull(requester);
            ArgumentNullException.ThrowIfNull(target);
            return new(
                CurrentProtocolVersion,
                actionId,
                requester.NetId,
                target.NetId,
                JsonSerializer.Serialize(payload));
        }

        internal static RitsuDebugActionSubmission Submit(Player requester, RitsuDebugActionEnvelope envelope)
        {
            ArgumentNullException.ThrowIfNull(requester);
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            EnsureHandlersRegistered();

            if (!RitsuLibSettingsStore.AreDeveloperToolsEnabled())
                return RitsuDebugActionSubmission.Reject(
                    "protocol.toolsDisabled",
                    "RitsuLib developer tools are disabled in settings.");

            envelope = envelope with { RequestedByNetId = requester.NetId };
            if (!TryValidateEnvelope(envelope, out _, out var validationError))
                return RitsuDebugActionSubmission.Reject(validationError);

            var runManager = RunManager.Instance;
            var netService = runManager?.NetService;
            if (runManager == null || netService == null)
                return RitsuDebugActionSubmission.Reject(
                    "protocol.startRun",
                    "Start a run before changing its state.");

            switch (netService)
            {
                case { Type: NetGameType.Singleplayer }:
                    return RequestManagedAction(runManager, envelope);
                case NetHostGameService host:
                    return !CanHostSynchronize(host, out var hostError)
                        ? RitsuDebugActionSubmission.Reject(hostError)
                        : RequestManagedAction(runManager, envelope);
                case NetClientGameService client:
                    return SubmitClientRequest(runManager, client, requester, envelope);
                default:
                    return RitsuDebugActionSubmission.Reject(
                        "protocol.unsupportedGameMode",
                        "Developer tools cannot change state in the current game mode.");
            }
        }

        internal static bool CanHostSynchronize(
            NetHostGameService host,
            out RitsuDebugActionFeedback feedback)
        {
            foreach (var peer in host.ConnectedPeers)
            {
                if (!peer.readyForBroadcasting)
                {
                    feedback = RitsuDebugActionFeedback.Create(
                        "protocol.peerNotReady",
                        "Another player is not ready yet; no state was changed.");
                    return false;
                }

                if (!PeerSupportsDeveloperActions(peer.peerId))
                {
                    feedback = RitsuDebugActionFeedback.Create(
                        "protocol.connectedPlayerUnsupported",
                        "A connected player cannot apply developer-tool changes; no state was changed.");
                    return false;
                }
            }

            feedback = default;
            return true;
        }

        private static RitsuDebugActionSubmission SubmitClientRequest(
            RunManager runManager,
            NetClientGameService client,
            Player requester,
            RitsuDebugActionEnvelope envelope)
        {
            if (!PeerSupportsDeveloperActions(client.HostNetId))
                return RitsuDebugActionSubmission.Reject(
                    "protocol.hostUnsupported",
                    "The host does not support these RitsuLib developer tools; no state was changed.");

            string requestId;
            lock (Gate)
            {
                RemoveExpiredPendingClientRequests();
                if (PendingClientRequests.Count >= MaxPendingClientRequests)
                    return RitsuDebugActionSubmission.Reject(
                        "protocol.tooManyPending",
                        "Too many changes are waiting for approval. Try again later.");

                requestId = Guid.NewGuid().ToString("N");
                PendingClientRequests.Add(requestId, new(envelope.ActionId, DateTimeOffset.UtcNow));
            }

            if (RitsuLibSidecarTypedMessageRegistry.SendToHost(
                    runManager,
                    RequestDescriptor,
                    new(requestId, envelope)))
                return RitsuDebugActionSubmission.PendingApproval(
                    "protocol.awaitingApproval",
                    "The host will review this change. The result will appear as a notification.");

            lock (Gate)
            {
                PendingClientRequests.Remove(requestId);
            }

            return RitsuDebugActionSubmission.Reject(
                "protocol.sendFailed",
                "The request could not be sent to the host; no state was changed.");
        }

        private static void OnClientRequest(
            RitsuLibSidecarTypedDispatchContext<RitsuDebugActionRequestMessage> context)
        {
            var runManager = RunManager.Instance;
            if (runManager?.NetService is not NetHostGameService host || !context.IsHostIngest)
                return;

            var request = context.Message;
            if (!IsValidRequestId(request.RequestId))
                return;

            var action = request.Action with { RequestedByNetId = context.SenderNetId };
            RitsuDebugActionSubmission decision;
            if (!TryReserveHostClientRequest(context.SenderNetId, request.RequestId, out var requestError))
                decision = RitsuDebugActionSubmission.Reject(requestError);
            else if (!PeerSupportsDeveloperActions(context.SenderNetId))
                decision = RitsuDebugActionSubmission.Reject(
                    "protocol.requesterUnsupported",
                    "The requesting player does not support these RitsuLib developer tools.");
            else if (!RitsuLibSettingsStore.AreDeveloperToolsEnabled())
                decision = RitsuDebugActionSubmission.Reject(
                    "protocol.hostToolsDisabled",
                    "The host has not enabled RitsuLib developer tools.");
            else if (!RitsuLibSettingsStore.AreDeveloperToolClientRequestsAllowed())
                decision = RitsuDebugActionSubmission.Reject(
                    "protocol.clientRequestsDisabled",
                    "The host does not allow other players to request state changes.");
            else if (!CanHostSynchronize(host, out var capabilityError))
                decision = RitsuDebugActionSubmission.Reject(capabilityError);
            else if (!TryValidateEnvelope(action, out _, out var validationError))
                decision = RitsuDebugActionSubmission.Reject(validationError);
            else
                decision = RequestManagedAction(runManager, action);

            if (!RitsuLibSidecarTypedMessageRegistry.SendToPeer(
                    host,
                    context.SenderNetId,
                    DecisionDescriptor,
                    new(request.RequestId, decision.Accepted, decision.Feedback)))
                RitsuLibFramework.Logger.Warn(
                    $"[DebugTools] Could not return request decision to client {context.SenderNetId}.");
        }

        private static void OnHostDecision(
            RitsuLibSidecarTypedDispatchContext<RitsuDebugActionDecisionMessage> context)
        {
            if (RunManager.Instance?.NetService is not NetClientGameService client ||
                context.SenderNetId != client.HostNetId)
                return;
            if (!IsValidRequestId(context.Message.RequestId) ||
                (!context.Message.Accepted && !context.Message.Feedback.IsValid()))
            {
                RitsuLibFramework.Logger.Warn("[DebugTools] Ignored an invalid host decision message.");
                return;
            }

            string actionId;
            lock (Gate)
            {
                if (!PendingClientRequests.Remove(context.Message.RequestId, out var pending))
                    return;

                actionId = pending.ActionId;
            }

            var title = ModSettingsLocalization.Get("ritsulib.debugTools.toastTitle", "Developer tools");
            if (context.Message.Accepted)
            {
                RitsuLibFramework.Logger.Info(
                    $"[DebugTools] Host accepted client request '{actionId}'.");
                RitsuToastService.ShowInfo(
                    ModSettingsLocalization.Get(
                        "ritsulib.debugTools.requestApproved",
                        "The host approved the requested change."),
                    title);
                return;
            }

            var feedback = context.Message.Feedback;
            RitsuLibFramework.Logger.Warn(
                $"[DebugTools] Host rejected client request '{actionId}': {feedback.GetEnglishText()}");
            RitsuToastService.ShowWarning(feedback.GetLocalizedText(), title);
        }

        private static RitsuDebugActionSubmission RequestManagedAction(
            RunManager runManager,
            RitsuDebugActionEnvelope envelope)
        {
            return RitsuLibManagedNetActions.Request(runManager, ActionDescriptor, envelope)
                ? RitsuDebugActionSubmission.Success
                : RitsuDebugActionSubmission.Reject(
                    "protocol.requestRejected",
                    "The requested change could not be accepted; no state was changed.");
        }

        private static async Task ExecuteManagedActionAsync(
            RitsuLibManagedNetActionContext<RitsuDebugActionEnvelope> managedContext)
        {
            if (!IsAuthorizedManagedActionOwner(managedContext.Player))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugTools] Rejected non-host action owner {managedContext.Player.NetId}.");
                NotifyActionExecuted(
                    managedContext.Message,
                    false,
                    RitsuDebugActionFeedback.Create(
                        "protocol.hostOnlyApproval",
                        "Only the host can approve this change; no state was changed."));
                return;
            }

            if (!TryValidateEnvelope(managedContext.Message, out var prepared, out var feedback))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugTools] Rejected synchronized action: {feedback.GetEnglishText()}");
                NotifyActionExecuted(managedContext.Message, false, feedback);
                return;
            }

            string result;
            try
            {
                result = await prepared.Registration.Execute(prepared.Context, managedContext.Message.PayloadJson);
            }
            catch (RitsuDebugActionExecutionException ex)
            {
                NotifyActionExecuted(managedContext.Message, false, ex.Feedback);
                throw;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugTools] Unexpected failure while applying '{managedContext.Message.ActionId}': {ex}");
                NotifyActionExecuted(
                    managedContext.Message,
                    false,
                    RitsuDebugActionFeedback.Create(
                        "protocol.executionFailed",
                        "The requested change could not be completed. See the game log for details."));
                throw;
            }

            RitsuLibFramework.Logger.Info(
                $"[DebugTools] Executed '{managedContext.Message.ActionId}' requestedBy=" +
                $"{prepared.Context.Requester.NetId} target={prepared.Context.Target.NetId}: {result}");
            NotifyActionExecuted(managedContext.Message, true, default);
        }

        private static void NotifyActionExecuted(
            RitsuDebugActionEnvelope envelope,
            bool success,
            RitsuDebugActionFeedback feedback)
        {
            if (!success && RunManager.Instance?.NetService?.NetId == envelope.RequestedByNetId)
                RitsuToastService.ShowError(
                    feedback.GetLocalizedText(),
                    ModSettingsLocalization.Get("ritsulib.debugTools.toastTitle", "Developer tools"));

            if (ActionExecuted is not { } handlers)
                return;

            var result = new RitsuDebugActionExecutionResult(
                envelope.ActionId,
                envelope.RequestedByNetId,
                envelope.TargetPlayerNetId,
                success,
                feedback);
            foreach (var handler in handlers.GetInvocationList().OfType<Action<RitsuDebugActionExecutionResult>>())
                try
                {
                    handler(result);
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn($"[DebugTools] Execution-result subscriber failed: {ex}");
                }
        }

        private static bool TryValidateEnvelope(
            RitsuDebugActionEnvelope envelope,
            out PreparedAction prepared,
            out RitsuDebugActionFeedback feedback)
        {
            prepared = default;
            if (envelope.ProtocolVersion != CurrentProtocolVersion)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "protocol.incompatibleVersion",
                    "This change requires a compatible RitsuLib version on every player.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(envelope.ActionId) ||
                envelope.ActionId.Length > MaxActionIdLength ||
                envelope.ActionId.Any(char.IsWhiteSpace))
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "protocol.invalidAction",
                    "The requested change is invalid.");
                return false;
            }

            if (envelope.PayloadJson == null || envelope.PayloadJson.Length > MaxActionPayloadCharacters)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "protocol.excessiveData",
                    "The requested change contains invalid or excessive data.");
                return false;
            }

            RegistrationBase registration;
            lock (Gate)
            {
                if (!Registrations.TryGetValue(envelope.ActionId, out registration!))
                {
                    feedback = RitsuDebugActionFeedback.Create(
                        "protocol.unsupportedAction",
                        "This RitsuLib version does not support the requested change.");
                    return false;
                }
            }

            var runManager = RunManager.Instance;
            var state = runManager?.DebugOnlyGetState();
            if (runManager == null || state == null || !runManager.IsInProgress)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "run.notInProgress",
                    "A run is not currently in progress.");
                return false;
            }

            var requester = state.Players.FirstOrDefault(player => player.NetId == envelope.RequestedByNetId);
            if (requester == null)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "protocol.requesterUnavailable",
                    "The player requesting this change is no longer in the run.");
                return false;
            }

            var target = state.Players.FirstOrDefault(player => player.NetId == envelope.TargetPlayerNetId);
            if (target == null)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "protocol.targetUnavailable",
                    "The selected player is no longer in the run.");
                return false;
            }

            var actionContext = new RitsuDebugActionContext(requester, target);
            var check = registration.Validate(actionContext, envelope.PayloadJson);
            if (!check.Success)
            {
                feedback = check.Feedback;
                return false;
            }

            prepared = new(registration, actionContext);
            feedback = default;
            return true;
        }

        private static bool IsAuthorizedManagedActionOwner(Player owner)
        {
            return RunManager.Instance?.NetService switch
            {
                { Type: NetGameType.Singleplayer } singleplayer => owner.NetId == singleplayer.NetId,
                { Type: NetGameType.Replay } => true,
                NetHostGameService host => owner.NetId == host.NetId,
                NetClientGameService client => owner.NetId == client.HostNetId,
                _ => false,
            };
        }

        private static bool PeerSupportsDeveloperActions(ulong peerNetId)
        {
            return RitsuLibSidecarSessionManager.TryGetPeerFeatures(peerNetId, out var features) &&
                   (features & RitsuLibSidecarPeerFeatures.DeveloperActionsV1) != 0;
        }

        private static bool IsValidRequestId(string? requestId)
        {
            return requestId is { Length: 32 } && Guid.TryParseExact(requestId, "N", out _);
        }

        private static void RemoveExpiredPendingClientRequests()
        {
            var cutoff = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(30);
            foreach (var requestId in PendingClientRequests
                         .Where(pair => pair.Value.CreatedAtUtc < cutoff)
                         .Select(static pair => pair.Key)
                         .ToArray())
                PendingClientRequests.Remove(requestId);
        }

        private static bool TryReserveHostClientRequest(
            ulong senderNetId,
            string requestId,
            out RitsuDebugActionFeedback feedback)
        {
            var now = DateTimeOffset.UtcNow;
            var rateCutoff = now - TimeSpan.FromSeconds(5);
            var retentionCutoff = now - TimeSpan.FromSeconds(30);
            var key = new HostClientRequestKey(senderNetId, requestId);
            lock (Gate)
            {
                foreach (var expiredKey in RecentHostClientRequests
                             .Where(pair => pair.Value < retentionCutoff)
                             .Select(static pair => pair.Key)
                             .ToArray())
                    RecentHostClientRequests.Remove(expiredKey);

                if (RecentHostClientRequests.ContainsKey(key))
                {
                    feedback = RitsuDebugActionFeedback.Create(
                        "protocol.requestAlreadyHandled",
                        "This request was already handled; no state was changed.");
                    return false;
                }

                if (RecentHostClientRequests.Count >= MaxRecentHostClientRequests)
                {
                    feedback = RitsuDebugActionFeedback.Create(
                        "protocol.rateLimited",
                        "Too many changes were requested recently. Try again in a few seconds.");
                    return false;
                }

                if (RecentHostClientRequests.Count(pair =>
                        pair.Key.SenderNetId == senderNetId && pair.Value >= rateCutoff) >=
                    MaxClientRequestsPerWindow)
                {
                    feedback = RitsuDebugActionFeedback.Create(
                        "protocol.rateLimited",
                        "Too many changes were requested recently. Try again in a few seconds.");
                    return false;
                }

                RecentHostClientRequests.Add(key, now);
            }

            feedback = default;
            return true;
        }

        private static T Deserialize<T>(ReadOnlySpan<byte> payload)
        {
            return JsonSerializer.Deserialize<T>(payload)
                   ?? throw new InvalidOperationException($"Failed to deserialize debug-tools payload {typeof(T)}.");
        }

        private readonly record struct PreparedAction(
            RegistrationBase Registration,
            RitsuDebugActionContext Context);

        private readonly record struct PendingClientRequest(
            string ActionId,
            DateTimeOffset CreatedAtUtc);

        private readonly record struct HostClientRequestKey(
            ulong SenderNetId,
            string RequestId);

        private abstract class RegistrationBase(string actionId, Type payloadType)
        {
            internal string ActionId { get; } = actionId;
            internal Type PayloadType { get; } = payloadType;

            internal abstract RitsuDebugActionCheck Validate(
                RitsuDebugActionContext context,
                string payloadJson);

            internal abstract Task<string> Execute(
                RitsuDebugActionContext context,
                string payloadJson);
        }

        private sealed class Registration<TPayload>(
            string actionId,
            Func<RitsuDebugActionContext, TPayload, RitsuDebugActionCheck> validate,
            Func<RitsuDebugActionContext, TPayload, Task<string>> execute)
            : RegistrationBase(actionId, typeof(TPayload))
        {
            internal override RitsuDebugActionCheck Validate(
                RitsuDebugActionContext context,
                string payloadJson)
            {
                if (!TryDeserializePayload(payloadJson, out var payload, out var error))
                    return RitsuDebugActionCheck.Fail(error);

                return validate(context, payload);
            }

            internal override Task<string> Execute(
                RitsuDebugActionContext context,
                string payloadJson)
            {
                if (!TryDeserializePayload(payloadJson, out var payload, out var error))
                    throw new RitsuDebugActionExecutionException(error);

                return execute(context, payload);
            }

            private bool TryDeserializePayload(
                string payloadJson,
                out TPayload payload,
                out RitsuDebugActionFeedback feedback)
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<TPayload>(payloadJson);
                    if (parsed is null)
                    {
                        payload = default!;
                        feedback = RitsuDebugActionFeedback.Create(
                            "protocol.missingData",
                            "The requested change is missing required data.");
                        return false;
                    }

                    payload = parsed;
                    feedback = default;
                    return true;
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[DebugTools] Invalid data for action '{ActionId}' ({typeof(TPayload).Name}): {ex}");
                    payload = default!;
                    feedback = RitsuDebugActionFeedback.Create(
                        "protocol.invalidData",
                        "The requested change contains invalid data.");
                    return false;
                }
            }
        }
    }
}
