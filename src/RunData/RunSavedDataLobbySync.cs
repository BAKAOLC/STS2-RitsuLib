using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using STS2RitsuLib.RunData.Patches;

namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Synchronizes lobby contributions through trailers appended to base-game messages, without custom network
    ///         messages or sidecar envelopes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过附加到原版游戏消息的尾部数据同步大厅贡献，不使用自定义网络消息或 Sidecar 信封。
    ///     </para>
    /// </summary>
    internal static class RunSavedDataLobbySync
    {
        private static readonly AsyncLocal<Stack<string?>?> OutboundPayloads = new();

        /// <summary>
        ///     <para xml:lang="en">Pushes the local staged contribution to the authoritative host lobby session.</para>
        ///     <para xml:lang="zh-CN">将本地暂存贡献推送到作为权威端的主机大厅会话。</para>
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
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
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
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                if (stack.Count > 0)
                    stack.Pop();
            }
        }
    }
}
