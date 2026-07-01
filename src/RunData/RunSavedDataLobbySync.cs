using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using STS2RitsuLib.RunData.Patches;

namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     Lobby contribution sync via vanilla message trailers (no custom INetMessage or sidecar envelopes).
    ///     通过原版消息尾部扩展同步大厅贡献（无自定义 INetMessage 或 sidecar 包）。
    /// </summary>
    internal static class RunSavedDataLobbySync
    {
        private static readonly AsyncLocal<Stack<string?>?> OutboundPayloads = new();

        /// <summary>
        ///     Pushes the local lobby staging contribution to the authoritative host session.
        ///     将本地大厅暂存贡献推送到权威主机会话。
        /// </summary>
        public static bool TryPushContribution(StartRunLobby lobby)
        {
            ArgumentNullException.ThrowIfNull(lobby);
            if (!RunSavedDataRegistry.HasSlots)
                return false;

            var netId = lobby.NetService.NetId;
            var payload = RunSavedDataRegistry.BuildLobbyContributionPayload(lobby, netId);
            return PushContributionCore(lobby, netId, payload);
        }

        internal static void AppendVanillaTrailer(StartRunLobby? lobby, PacketWriter writer)
        {
            if (TryPeekOutboundPayload(out var outboundPayload))
            {
                RunSavedDataPatchHelpers.WritePayload(writer, outboundPayload);
                return;
            }

            if (lobby == null || !RunSavedDataRegistry.HasSlots)
                return;

            var payload = RunSavedDataRegistry.BuildLobbyContributionPayload(lobby, lobby.NetService.NetId);
            RunSavedDataPatchHelpers.WritePayload(writer, payload);
        }

        internal static void TryMergeVanillaTrailer(StartRunLobby lobby, ulong senderId)
        {
            if (lobby.NetService.Type != NetGameType.Host)
                return;

            if (!RunSavedDataLobbyContributionState.TryConsume(out var payload))
                return;

            RunSavedDataRegistry.MergeLobbyContribution(lobby, senderId, payload);
        }

        internal static IDisposable? PushOutboundContribution(StartRunLobby lobby)
        {
            ArgumentNullException.ThrowIfNull(lobby);
            if (!RunSavedDataRegistry.HasSlots)
                return null;

            var payload = RunSavedDataRegistry.BuildLobbyContributionPayload(lobby, lobby.NetService.NetId);
            return PushOutboundPayload(payload);
        }

        private static bool PushContributionCore(StartRunLobby lobby, ulong netId, string? payload)
        {
            switch (lobby.NetService.Type)
            {
                case NetGameType.Host:
                case NetGameType.Singleplayer:
                    RunSavedDataRegistry.MergeLobbyContribution(lobby, netId, payload);
                    return true;
                case NetGameType.Client:
                    return TrySendVanillaContributionMessage(lobby, payload);
                default:
                    return false;
            }
        }

        private static bool TrySendVanillaContributionMessage(StartRunLobby lobby, string? payload)
        {
            try
            {
                var character = lobby.LocalPlayer.character;
                if (character == null)
                    return false;

                using (PushOutboundPayload(payload))
                {
                    lobby.NetService.SendMessage(new LobbyPlayerChangedCharacterMessage { character = character });
                }

                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[RunSavedData] Failed to push lobby contribution: {ex.Message}");
                return false;
            }
        }

        private static IDisposable PushOutboundPayload(string? payload)
        {
            var stack = OutboundPayloads.Value;
            if (stack == null)
            {
                stack = new();
                OutboundPayloads.Value = stack;
            }

            stack.Push(payload);
            return new OutboundPayloadScope(stack);
        }

        private static bool TryPeekOutboundPayload(out string? payload)
        {
            var stack = OutboundPayloads.Value;
            if (stack is { Count: > 0 })
            {
                payload = stack.Peek();
                return true;
            }

            payload = null;
            return false;
        }

        private sealed class OutboundPayloadScope(Stack<string?> stack) : IDisposable
        {
            public void Dispose()
            {
                if (stack.Count > 0)
                    stack.Pop();
            }
        }
    }
}
