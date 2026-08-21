namespace STS2RitsuLib.Networking.Sidecar
{
    [Flags]
    internal enum RitsuLibSidecarTransportProfileMask : byte
    {
        None = 0,
        Control = 1 << 0,
        RealtimeDatagram = 1 << 1,
        BulkStream = 1 << 2,
    }

    internal enum RitsuLibSidecarEndpointDestination : byte
    {
        Host = 1,
        Broadcast = 2,
        Peer = 3,
    }

    internal readonly record struct RitsuLibSidecarEndpointKey(string OwnerId, string Name);

    internal readonly record struct RitsuLibSidecarEndpointAdvertisement(
        RitsuLibSidecarEndpointKey Key,
        ushort ProtocolVersion,
        ushort MinimumCompatibleProtocolVersion,
        RitsuLibSidecarDeliveryProfile DeliveryProfile,
        RitsuLibSidecarEndpointTopology Topology,
        int MaxPayloadBytes);

    internal readonly record struct RitsuLibSidecarEndpointCatalog(
        RitsuLibSidecarTransportProfileMask SupportedProfiles,
        IReadOnlyList<RitsuLibSidecarEndpointAdvertisement> Endpoints);

    internal readonly record struct RitsuLibSidecarEndpointRouteDefinition(
        uint RouteId,
        ulong Nonce,
        RitsuLibSidecarEndpointKey Key,
        ushort ProtocolVersion,
        RitsuLibSidecarDeliveryProfile DeliveryProfile,
        RitsuLibSidecarEndpointTopology Topology,
        int MaxPayloadBytes,
        IReadOnlyList<ulong> ParticipantNetIds);

    internal readonly record struct RitsuLibSidecarEndpointRouteSnapshot(
        uint Revision,
        IReadOnlyList<RitsuLibSidecarEndpointRouteDefinition> Routes);

    internal readonly record struct RitsuLibSidecarEndpointIngressFrame(
        uint RouteId,
        ulong Nonce,
        uint Sequence,
        RitsuLibSidecarEndpointDestination Destination,
        ulong TargetNetId,
        ReadOnlyMemory<byte> Payload);

    internal readonly record struct RitsuLibSidecarEndpointDeliveryFrame(
        uint RouteId,
        ulong Nonce,
        uint Sequence,
        ulong OriginalSenderNetId,
        ReadOnlyMemory<byte> Payload);
}
