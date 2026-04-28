namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarDiscoveryPolicy
    {
        public const string RouteReasonPrefix = "route:";

        public const string ReasonPeerConnected = "peer_connected";
        public const string ReasonHandshake = "handshake";

        public const string RouteNameManualHint = "manual_hint";
        public const int RouteOrderManualHint = 0;

        public const string RouteNameNativeTrailer = "native_trailer";
        public const int RouteOrderNativeTrailer = 50;

        public const string RouteNameSteamLobbyMemberData = "steam_lobby_member_data";
        public const int RouteOrderSteamLobbyMemberData = 100;
    }
}
