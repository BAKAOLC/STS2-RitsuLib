using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Platform;
using Steamworks;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal sealed class RitsuLibSidecarSteamLobbyValidationRoute : IRitsuLibSidecarCapabilityValidationRoute
    {
        public string Name => RitsuLibSidecarDiscoveryPolicy.RouteNameSteamLobbyMemberData;
        public int Order => RitsuLibSidecarDiscoveryPolicy.RouteOrderSteamLobbyMemberData;

        public bool IsAvailable(INetGameService netService)
        {
            if (netService.Platform != PlatformType.Steam)
                return false;
            if (!ulong.TryParse(netService.GetRawLobbyIdentifier(), out var lobbyIdRaw))
                return false;

            var lobbyId = new CSteamID(lobbyIdRaw);
            var memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            if (memberCount <= 0)
                return false;

            var localPeer = new CSteamID(netService.NetId);
            for (var i = 0; i < memberCount; i++)
                if (SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i) == localPeer)
                    return true;

            return false;
        }

        public void PublishLocalEvidence(INetGameService netService)
        {
            if (!TryGetLobbyId(netService, out var lobbyId))
                return;
            SteamMatchmaking.SetLobbyMemberData(
                lobbyId,
                RitsuLibSidecarCapabilityMarkers.SteamLobbyMemberKey,
                RitsuLibSidecarCapabilityMarkers.SteamLobbyMemberValueSupported);
        }

        public RitsuLibSidecarPeerReachability? TryResolve(INetGameService netService, ulong peerNetId)
        {
            if (!TryGetLobbyId(netService, out var lobbyId))
                return null;

            var peerId = new CSteamID(peerNetId);
            var value = SteamMatchmaking.GetLobbyMemberData(
                lobbyId,
                peerId,
                RitsuLibSidecarCapabilityMarkers.SteamLobbyMemberKey);
            if (string.IsNullOrEmpty(value))
                return null;

            return value == RitsuLibSidecarCapabilityMarkers.SteamLobbyMemberValueSupported
                ? RitsuLibSidecarPeerReachability.Supported
                : RitsuLibSidecarPeerReachability.Unsupported;
        }

        private static bool TryGetLobbyId(INetGameService netService, out CSteamID lobbyId)
        {
            lobbyId = CSteamID.Nil;
            if (!TryReadSteamLobbyId(netService, out var raw))
                return false;

            lobbyId = new(raw);
            return true;
        }

        private static bool TryReadSteamLobbyId(INetGameService netService, out ulong lobbyIdRaw)
        {
            lobbyIdRaw = 0;
            if (netService.Platform != PlatformType.Steam)
                return false;
            if (!ulong.TryParse(netService.GetRawLobbyIdentifier(), out lobbyIdRaw))
                return false;
            var lobbyId = new CSteamID(lobbyIdRaw);
            return SteamMatchmaking.GetNumLobbyMembers(lobbyId) > 0;
        }
    }
}
