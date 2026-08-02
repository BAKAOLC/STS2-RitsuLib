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
        string Reason);

    internal readonly record struct RitsuDebugActionContext(
        Player Requester,
        Player Target);

    internal readonly record struct RitsuDebugActionCheck(bool Success, string Error)
    {
        internal static RitsuDebugActionCheck Ok => new(true, string.Empty);

        internal static RitsuDebugActionCheck Fail(string error)
        {
            return new(false, error);
        }
    }

    internal readonly record struct RitsuDebugActionSubmission(bool Accepted, string Message);

    internal readonly record struct RitsuDebugActionExecutionResult(
        string ActionId,
        ulong RequestedByNetId,
        ulong TargetPlayerNetId,
        bool Success,
        string Message);

    internal sealed class RitsuDebugActionExecutionException(string message)
        : InvalidOperationException(message);

    internal static class RitsuDebugActionProtocol
    {
        internal const int CurrentProtocolVersion = 1;

        private const int MaxActionIdLength = 64;
        private const int MaxActionPayloadCharacters = 8 * 1024;
        private const int MaxDecisionMessageCharacters = 2048;
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
                return new(false, "RitsuLib developer tools are disabled in settings.");

            envelope = envelope with { RequestedByNetId = requester.NetId };
            if (!TryValidateEnvelope(envelope, out _, out var validationError))
                return new(false, validationError);

            var runManager = RunManager.Instance;
            var netService = runManager?.NetService;
            if (runManager == null || netService == null)
                return new(false, "Start a run before changing its state.");

            switch (netService)
            {
                case { Type: NetGameType.Singleplayer }:
                    return RequestManagedAction(runManager, envelope);
                case NetHostGameService host:
                    return !CanHostSynchronize(host, out var hostError)
                        ? new(false, hostError)
                        : RequestManagedAction(runManager, envelope);
                case NetClientGameService client:
                    return SubmitClientRequest(runManager, client, requester, envelope);
                default:
                    return new(false, "Developer tools cannot change state in the current game mode.");
            }
        }

        internal static bool CanHostSynchronize(NetHostGameService host, out string error)
        {
            foreach (var peer in host.ConnectedPeers)
            {
                if (!peer.readyForBroadcasting)
                {
                    error = "Another player is not ready yet; no state was changed.";
                    return false;
                }

                if (!PeerSupportsDeveloperActions(peer.peerId))
                {
                    error = "A connected player cannot apply developer-tool changes; no state was changed.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static RitsuDebugActionSubmission SubmitClientRequest(
            RunManager runManager,
            NetClientGameService client,
            Player requester,
            RitsuDebugActionEnvelope envelope)
        {
            if (!PeerSupportsDeveloperActions(client.HostNetId))
                return new(false,
                    "The host does not support these RitsuLib developer tools; no state was changed.");

            string requestId;
            lock (Gate)
            {
                RemoveExpiredPendingClientRequests();
                if (PendingClientRequests.Count >= MaxPendingClientRequests)
                    return new(false, "Too many changes are waiting for approval. Try again later.");

                requestId = Guid.NewGuid().ToString("N");
                PendingClientRequests.Add(requestId, new(envelope.ActionId, DateTimeOffset.UtcNow));
            }

            if (RitsuLibSidecarTypedMessageRegistry.SendToHost(
                    runManager,
                    RequestDescriptor,
                    new(requestId, envelope)))
                return new(true, "The host will review this change. The result will appear as a notification.");

            lock (Gate)
            {
                PendingClientRequests.Remove(requestId);
            }

            return new(false, "The request could not be sent to the host; no state was changed.");
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
                decision = new(false, requestError);
            else if (!PeerSupportsDeveloperActions(context.SenderNetId))
                decision = new(false, "The requesting player does not support these RitsuLib developer tools.");
            else if (!RitsuLibSettingsStore.AreDeveloperToolsEnabled())
                decision = new(false, "The host has not enabled RitsuLib developer tools.");
            else if (!RitsuLibSettingsStore.AreDeveloperToolClientRequestsAllowed())
                decision = new(false, "The host does not allow other players to request state changes.");
            else if (!CanHostSynchronize(host, out var capabilityError))
                decision = new(false, capabilityError);
            else if (!TryValidateEnvelope(action, out _, out var validationError))
                decision = new(false, validationError);
            else
                decision = RequestManagedAction(runManager, action);

            if (!RitsuLibSidecarTypedMessageRegistry.SendToPeer(
                    host,
                    context.SenderNetId,
                    DecisionDescriptor,
                    new(request.RequestId, decision.Accepted, decision.Message)))
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
                string.IsNullOrWhiteSpace(context.Message.Reason) ||
                context.Message.Reason.Length > MaxDecisionMessageCharacters)
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
                    $"[DebugTools] Host accepted client request '{actionId}': {context.Message.Reason}");
                RitsuToastService.ShowInfo(
                    ModSettingsLocalization.Get(
                        "ritsulib.debugTools.requestApproved",
                        "The host approved the requested change."),
                    title);
                return;
            }

            RitsuLibFramework.Logger.Warn(
                $"[DebugTools] Host rejected client request '{actionId}': {context.Message.Reason}");
            RitsuToastService.ShowWarning(context.Message.Reason, title);
        }

        private static RitsuDebugActionSubmission RequestManagedAction(
            RunManager runManager,
            RitsuDebugActionEnvelope envelope)
        {
            return RitsuLibManagedNetActions.Request(runManager, ActionDescriptor, envelope)
                ? new(true, "The requested change was accepted and will be applied shortly.")
                : new(false, "The requested change could not be accepted; no state was changed.");
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
                    "Only the host can approve this change; no state was changed.");
                return;
            }

            if (!TryValidateEnvelope(managedContext.Message, out var prepared, out var error))
            {
                RitsuLibFramework.Logger.Warn($"[DebugTools] Rejected synchronized action: {error}");
                NotifyActionExecuted(managedContext.Message, false, error);
                return;
            }

            string result;
            try
            {
                result = await prepared.Registration.Execute(prepared.Context, managedContext.Message.PayloadJson);
            }
            catch (RitsuDebugActionExecutionException ex)
            {
                NotifyActionExecuted(managedContext.Message, false, ex.Message);
                throw;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugTools] Unexpected failure while applying '{managedContext.Message.ActionId}': {ex}");
                NotifyActionExecuted(
                    managedContext.Message,
                    false,
                    "The requested change could not be completed. See the game log for details.");
                throw;
            }

            RitsuLibFramework.Logger.Info(
                $"[DebugTools] Executed '{managedContext.Message.ActionId}' requestedBy=" +
                $"{prepared.Context.Requester.NetId} target={prepared.Context.Target.NetId}: {result}");
            NotifyActionExecuted(managedContext.Message, true, result);
        }

        private static void NotifyActionExecuted(
            RitsuDebugActionEnvelope envelope,
            bool success,
            string message)
        {
            if (!success && RunManager.Instance?.NetService?.NetId == envelope.RequestedByNetId)
                RitsuToastService.ShowError(
                    message,
                    ModSettingsLocalization.Get("ritsulib.debugTools.toastTitle", "Developer tools"));

            if (ActionExecuted is not { } handlers)
                return;

            var result = new RitsuDebugActionExecutionResult(
                envelope.ActionId,
                envelope.RequestedByNetId,
                envelope.TargetPlayerNetId,
                success,
                message);
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
            out string error)
        {
            prepared = default;
            if (envelope.ProtocolVersion != CurrentProtocolVersion)
            {
                error = "This change requires a compatible RitsuLib version on every player.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(envelope.ActionId) ||
                envelope.ActionId.Length > MaxActionIdLength ||
                envelope.ActionId.Any(char.IsWhiteSpace))
            {
                error = "The requested change is invalid.";
                return false;
            }

            if (envelope.PayloadJson == null || envelope.PayloadJson.Length > MaxActionPayloadCharacters)
            {
                error = "The requested change contains invalid or excessive data.";
                return false;
            }

            RegistrationBase registration;
            lock (Gate)
            {
                if (!Registrations.TryGetValue(envelope.ActionId, out registration!))
                {
                    error = "This RitsuLib version does not support the requested change.";
                    return false;
                }
            }

            var runManager = RunManager.Instance;
            var state = runManager?.DebugOnlyGetState();
            if (runManager == null || state == null || !runManager.IsInProgress)
            {
                error = "A run is not currently in progress.";
                return false;
            }

            var requester = state.Players.FirstOrDefault(player => player.NetId == envelope.RequestedByNetId);
            if (requester == null)
            {
                error = "The player requesting this change is no longer in the run.";
                return false;
            }

            var target = state.Players.FirstOrDefault(player => player.NetId == envelope.TargetPlayerNetId);
            if (target == null)
            {
                error = "The selected player is no longer in the run.";
                return false;
            }

            var actionContext = new RitsuDebugActionContext(requester, target);
            var check = registration.Validate(actionContext, envelope.PayloadJson);
            if (!check.Success)
            {
                error = check.Error;
                return false;
            }

            prepared = new(registration, actionContext);
            error = string.Empty;
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
            out string error)
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
                    error = "This request was already handled; no state was changed.";
                    return false;
                }

                if (RecentHostClientRequests.Count >= MaxRecentHostClientRequests)
                {
                    error = "Too many changes were requested recently. Try again in a few seconds.";
                    return false;
                }

                if (RecentHostClientRequests.Count(pair =>
                        pair.Key.SenderNetId == senderNetId && pair.Value >= rateCutoff) >=
                    MaxClientRequestsPerWindow)
                {
                    error = "Too many changes were requested recently. Try again in a few seconds.";
                    return false;
                }

                RecentHostClientRequests.Add(key, now);
            }

            error = string.Empty;
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
                    throw new InvalidOperationException(error);

                return execute(context, payload);
            }

            private bool TryDeserializePayload(
                string payloadJson,
                out TPayload payload,
                out string error)
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<TPayload>(payloadJson);
                    if (parsed is null)
                    {
                        payload = default!;
                        error = "The requested change is missing required data.";
                        return false;
                    }

                    payload = parsed;
                    error = string.Empty;
                    return true;
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[DebugTools] Invalid data for action '{ActionId}' ({typeof(TPayload).Name}): {ex}");
                    payload = default!;
                    error = "The requested change contains invalid data.";
                    return false;
                }
            }
        }
    }
}
