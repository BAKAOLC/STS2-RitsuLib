using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarEndpointTransport
    {
        internal static RitsuLibSidecarTransportProfileMask GetSupportedProfiles(INetGameService? netService)
        {
            if (!TryGetTransport(netService, out _))
                return RitsuLibSidecarTransportProfileMask.None;

            return RitsuLibSidecarTransportProfileMask.Control |
                   RitsuLibSidecarTransportProfileMask.RealtimeDatagram |
                   RitsuLibSidecarTransportProfileMask.BulkStream;
        }

        internal static bool SupportsProfile(
            INetGameService? netService,
            RitsuLibSidecarDeliveryProfile deliveryProfile)
        {
            var supported = GetSupportedProfiles(netService);
            return deliveryProfile switch
            {
                RitsuLibSidecarDeliveryProfile.Control =>
                    (supported & RitsuLibSidecarTransportProfileMask.Control) != 0,
                RitsuLibSidecarDeliveryProfile.RealtimeDatagram =>
                    (supported & RitsuLibSidecarTransportProfileMask.RealtimeDatagram) != 0,
                RitsuLibSidecarDeliveryProfile.BulkStream =>
                    (supported & RitsuLibSidecarTransportProfileMask.BulkStream) != 0,
                _ => false,
            };
        }

        internal static bool TrySend(
            INetGameService? netService,
            ulong peerNetId,
            byte[] envelope,
            RitsuLibSidecarDeliveryProfile deliveryProfile)
        {
            if (!TryGetNetworkParameters(deliveryProfile, out var mode, out var channel))
                return false;

            return netService switch
            {
                NetClientGameService client when peerNetId == client.HostNetId =>
                    RitsuLibSidecarSend.TrySendToHost(client, envelope, mode, channel),
                NetHostGameService host =>
                    RitsuLibSidecarSend.TrySendToPeer(host, peerNetId, envelope, mode, channel),
                _ => false,
            };
        }

        internal static bool MatchesReceivedProfile(
            RitsuLibSidecarDeliveryProfile deliveryProfile,
            NetTransferMode transferMode)
        {
            return deliveryProfile switch
            {
                RitsuLibSidecarDeliveryProfile.Control => transferMode == NetTransferMode.Reliable,
                RitsuLibSidecarDeliveryProfile.RealtimeDatagram => transferMode == NetTransferMode.Unreliable,
                RitsuLibSidecarDeliveryProfile.BulkStream => transferMode == NetTransferMode.Reliable,
                _ => false,
            };
        }

        internal static bool TryGetNetworkParameters(
            RitsuLibSidecarDeliveryProfile deliveryProfile,
            out NetTransferMode mode,
            out int channel)
        {
            switch (deliveryProfile)
            {
                case RitsuLibSidecarDeliveryProfile.Control:
                    mode = NetTransferMode.Reliable;
                    channel = RitsuLibSidecarWire.RecommendedReliableChannel;
                    return true;
                case RitsuLibSidecarDeliveryProfile.RealtimeDatagram:
                    mode = NetTransferMode.Unreliable;
                    channel = RitsuLibSidecarWire.RecommendedUnreliableChannel;
                    return true;
                case RitsuLibSidecarDeliveryProfile.BulkStream:
                    mode = NetTransferMode.Reliable;
                    channel = RitsuLibSidecarWire.RecommendedBulkChannel;
                    return true;
                default:
                    mode = NetTransferMode.None;
                    channel = 0;
                    return false;
            }
        }

        private static bool TryGetTransport(INetGameService? netService, out object? transport)
        {
            transport = netService switch
            {
                NetClientGameService client => client.NetClient,
                NetHostGameService host => host.NetHost,
                _ => null,
            };
            return transport != null && netService is { IsConnected: true };
        }
    }
}
