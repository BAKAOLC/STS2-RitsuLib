using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Platform;
using STS2RitsuLib.Platform.Steam;

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

            if (!RitsuLibSteamworks.TryGetNumLobbyMembers(lobbyIdRaw, out var memberCount))
                return false;
            if (memberCount <= 0)
                return false;

            return RitsuLibSteamworks.TryLobbyContainsMember(lobbyIdRaw, netService.NetId, out var contains) &&
                   contains;
        }

        public void PublishLocalEvidence(INetGameService netService)
        {
            if (!TryGetLobbyId(netService, out var lobbyId))
                return;
            RitsuLibSteamworks.TrySetLobbyMemberData(
                lobbyId,
                RitsuLibSidecarCapabilityMarkers.SteamLobbyMemberKey,
                RitsuLibSidecarCapabilityMarkers.SteamLobbyMemberValueSupported);
        }

        public RitsuLibSidecarPeerReachability? TryResolve(INetGameService netService, ulong peerNetId)
        {
            if (!TryGetLobbyId(netService, out var lobbyId))
                return null;

            if (!RitsuLibSteamworks.TryGetLobbyMemberData(
                    lobbyId,
                    peerNetId,
                    RitsuLibSidecarCapabilityMarkers.SteamLobbyMemberKey,
                    out var value))
                return null;
            if (string.IsNullOrEmpty(value))
                return null;

            return value == RitsuLibSidecarCapabilityMarkers.SteamLobbyMemberValueSupported
                ? RitsuLibSidecarPeerReachability.Supported
                : RitsuLibSidecarPeerReachability.Unsupported;
        }

        private static bool TryGetLobbyId(INetGameService netService, out ulong lobbyId)
        {
            lobbyId = 0;
            if (!TryReadSteamLobbyId(netService, out var raw))
                return false;

            lobbyId = raw;
            return true;
        }

        private static bool TryReadSteamLobbyId(INetGameService netService, out ulong lobbyIdRaw)
        {
            lobbyIdRaw = 0;
            if (netService.Platform != PlatformType.Steam)
                return false;
            if (!ulong.TryParse(netService.GetRawLobbyIdentifier(), out lobbyIdRaw))
                return false;
            return RitsuLibSteamworks.TryGetNumLobbyMembers(lobbyIdRaw, out var memberCount) && memberCount > 0;
        }
    }
}
