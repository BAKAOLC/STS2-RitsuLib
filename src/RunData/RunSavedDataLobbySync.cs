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

        private static bool PushContributionCore(StartRunLobby lobby, ulong netId, string? payload)
        {
            switch (lobby.NetService.Type)
            {
                case NetGameType.Host:
                case NetGameType.Singleplayer:
                    RunSavedDataRegistry.MergeLobbyContribution(lobby, netId, payload);
                    return true;
                case NetGameType.Client:
                    return TrySendVanillaContributionMessage(lobby);
                default:
                    return false;
            }
        }

        private static bool TrySendVanillaContributionMessage(StartRunLobby lobby)
        {
            try
            {
                var character = lobby.LocalPlayer.character;
                if (character == null)
                    return false;

                lobby.NetService.SendMessage(new LobbyPlayerChangedCharacterMessage { character = character });
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[RunSavedData] Failed to push lobby contribution: {ex.Message}");
                return false;
            }
        }
    }
}
